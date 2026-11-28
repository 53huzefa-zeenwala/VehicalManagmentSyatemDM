import 'dart:io';

import 'package:cookie_jar/cookie_jar.dart';
import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:dio_cookie_manager/dio_cookie_manager.dart';

import 'api_exception.dart';

/// Accepts the server's self-signed certificate (D-036) for the configured API host only. Applied to the
/// API client and, through [HttpOverrides.global], to image loading.
class ApiHostCertificateOverrides extends HttpOverrides {
  ApiHostCertificateOverrides(this.apiHost);

  final String apiHost;

  @override
  HttpClient createHttpClient(SecurityContext? context) =>
      super.createHttpClient(context)..badCertificateCallback = (_, host, _) => host == apiHost;
}

/// The one HTTP client for the API: `/api/v1` base path, the session cookie kept across app restarts,
/// `X-App-Version` on every call, and every failure turned into an [ApiException].
class ApiClient {
  ApiClient._(this._dio, this._cookies, this.origin);

  /// [storageDirectory] holds the cookie jar (the session survives app restarts).
  static ApiClient create({required Uri origin, required String appVersion, required Directory storageDirectory}) {
    final cookies = PersistCookieJar(storage: FileStorage('${storageDirectory.path}/cookies/'));
    final dio = Dio(
      BaseOptions(
        baseUrl: '${origin.toString()}/api/v1/',
        connectTimeout: const Duration(seconds: 15),
        // The API may wait up to 15 s for its database on a cold start, and up to 10 s for a lock.
        receiveTimeout: const Duration(seconds: 40),
        headers: {'X-App-Version': appVersion, HttpHeaders.acceptHeader: 'application/json'},
        contentType: Headers.jsonContentType,
        // `ids=1&ids=2` (ASP.NET Core binds repeated keys to a list).
        listFormat: ListFormat.multi,
      ),
    );
    dio.httpClientAdapter = IOHttpClientAdapter(
      createHttpClient: () => HttpClient()..badCertificateCallback = (_, host, _) => host == origin.host,
    );
    dio.interceptors.add(CookieManager(cookies));
    final client = ApiClient._(dio, cookies, origin);
    dio.interceptors.add(
      InterceptorsWrapper(
        onError: (error, handler) {
          final exception = ApiException.fromDio(error);
          final isSignIn = error.requestOptions.path.startsWith('auth/login');
          if (exception.endsSession && !isSignIn) {
            client.onSessionEnded?.call(exception);
          }
          handler.next(error);
        },
      ),
    );
    return client;
  }

  final Dio _dio;
  final PersistCookieJar _cookies;

  /// Server origin, e.g. `https://server:8445`.
  final Uri origin;

  /// Called when any request reports that the session has ended (401), so the app can show sign-in.
  void Function(ApiException error)? onSessionEnded;

  Future<T> get<T>(String path, {Map<String, Object?>? query, required T Function(Object? json) parse}) =>
      _send(() => _dio.get<Object?>(path, queryParameters: _clean(query)), parse);

  Future<T> post<T>(String path, {Object? body, required T Function(Object? json) parse}) =>
      _send(() => _dio.post<Object?>(path, data: body), parse);

  /// For calls that answer 204 No Content.
  Future<void> postNoContent(String path, {Object? body}) =>
      _send(() => _dio.post<Object?>(path, data: body), (_) {});

  Future<T> put<T>(String path, {Object? body, required T Function(Object? json) parse}) =>
      _send(() => _dio.put<Object?>(path, data: body), parse);

  /// Multipart upload of one file (field `file`), e.g. a vehicle or staff photo.
  Future<T> upload<T>(
    String path, {
    required List<int> bytes,
    required String fileName,
    required String mimeType,
    required T Function(Object? json) parse,
  }) {
    final [type, subtype] = mimeType.split('/');
    final form = FormData.fromMap({
      'file': MultipartFile.fromBytes(bytes, filename: fileName, contentType: DioMediaType(type, subtype)),
    });
    return _send(
      () => _dio.put<Object?>(path, data: form, options: Options(contentType: Headers.multipartFormDataContentType)),
      parse,
    );
  }

  /// Forgets the session cookie (sign-out, or the server ended the session).
  Future<void> clearSession() => _cookies.deleteAll();

  /// Absolute URL for a file path the API returns (e.g. `/api/v1/files/staff/x.jpg`).
  Uri fileUri(String path) => origin.resolve(path);

  /// `Cookie` header value for [uri], so images can load with the session (files need sign-in).
  Future<String> cookieHeader(Uri uri) async {
    final cookies = await _cookies.loadForRequest(uri);
    return cookies.map((c) => '${c.name}=${c.value}').join('; ');
  }

  Future<T> _send<T>(Future<Response<Object?>> Function() request, T Function(Object? json) parse) async {
    try {
      final response = await request();
      return parse(response.statusCode == 204 ? null : response.data);
    } on DioException catch (error) {
      throw ApiException.fromDio(error);
    }
  }

  static Map<String, Object?>? _clean(Map<String, Object?>? query) =>
      query == null ? null : (Map.of(query)..removeWhere((_, value) => value == null));
}
