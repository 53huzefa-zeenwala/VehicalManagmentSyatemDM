import 'package:dio/dio.dart';

/// Every failed API call, in the API's single error format (status, code, title, detail, errors, traceId),
/// plus the cases where no response arrived at all (no network, timeout).
class ApiException implements Exception {
  const ApiException({
    required this.code,
    required this.title,
    required this.detail,
    this.status,
    this.fieldErrors = const {},
    this.traceId,
  });

  /// Parses the API error envelope; falls back to a generic message for anything else.
  factory ApiException.fromResponse(Response<Object?>? response) {
    final data = response?.data;
    if (data is Map<String, Object?> && data['code'] is String) {
      final errors = <String, List<String>>{};
      final rawErrors = data['errors'];
      if (rawErrors is Map<String, Object?>) {
        for (final entry in rawErrors.entries) {
          final messages = entry.value;
          if (messages is List) {
            errors[entry.key] = messages.whereType<String>().toList();
          }
        }
      }
      return ApiException(
        status: (data['status'] as num?)?.toInt() ?? response?.statusCode,
        code: data['code']! as String,
        title: data['title'] as String? ?? 'Error',
        detail: data['detail'] as String? ?? 'Something went wrong.',
        fieldErrors: errors,
        traceId: data['traceId'] as String?,
      );
    }

    return ApiException(
      status: response?.statusCode,
      code: 'UNEXPECTED_RESPONSE',
      title: 'Unexpected response',
      detail: 'The server sent an unexpected response (${response?.statusCode ?? 'no status'}). Try again.',
    );
  }

  factory ApiException.fromDio(DioException error) {
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.transformTimeout:
        return const ApiException(
          code: networkTimeout,
          title: 'No answer',
          detail: 'The server took too long to answer. Check your connection and try again.',
        );
      case DioExceptionType.connectionError:
      case DioExceptionType.badCertificate:
        return const ApiException(
          code: networkUnavailable,
          title: 'No connection',
          detail: "Can't reach the server. Check your connection and try again.",
        );
      case DioExceptionType.badResponse:
        return ApiException.fromResponse(error.response);
      case DioExceptionType.cancel:
        return const ApiException(code: cancelled, title: 'Cancelled', detail: 'The request was cancelled.');
      case DioExceptionType.unknown:
        return ApiException(
          code: networkUnavailable,
          title: 'No connection',
          detail: "Can't reach the server. Check your connection and try again. (${error.message ?? error.error})",
        );
    }
  }

  static const String networkUnavailable = 'NETWORK_UNAVAILABLE';
  static const String networkTimeout = 'NETWORK_TIMEOUT';
  static const String cancelled = 'CANCELLED';

  final int? status;
  final String code;
  final String title;
  final String detail;
  final Map<String, List<String>> fieldErrors;
  final String? traceId;

  /// The session is gone (expired, password changed elsewhere, or account deactivated): go to sign-in.
  bool get endsSession => status == 401 && (code == 'UNAUTHENTICATED' || code == 'ACCOUNT_DEACTIVATED');

  /// Worth an automatic or one-tap retry: no network, timeouts, server starting or busy.
  bool get isTransient =>
      code == networkUnavailable ||
      code == networkTimeout ||
      code == 'DATABASE_NOT_READY' ||
      code == 'DATABASE_UNAVAILABLE' ||
      code == 'DATABASE_TIMEOUT';

  /// Someone else saved the same record first (stale `rowVersion`): reload before editing again.
  bool get isConflict => code == 'CONCURRENCY_CONFLICT';

  /// First message for a request field (JSON name), for showing under the input.
  String? fieldError(String field) {
    final messages = fieldErrors[field];
    return messages == null || messages.isEmpty ? null : messages.first;
  }

  @override
  String toString() => 'ApiException($status $code: $detail)';
}
