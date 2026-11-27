import 'package:dio/dio.dart';
import 'package:dm_core/dm_core.dart';
import 'package:flutter_test/flutter_test.dart';

Response<Object?> _response(int status, Object? data) =>
    Response(requestOptions: RequestOptions(path: 'x'), statusCode: status, data: data);

void main() {
  test('parses the API error envelope with field errors and trace id', () {
    final error = ApiException.fromResponse(
      _response(400, {
        'status': 400,
        'code': 'VALIDATION_FAILED',
        'title': 'Check the highlighted fields',
        'detail': 'Some fields are not valid.',
        'errors': {
          'newPassword': ['Use 8 to 128 characters.'],
        },
        'traceId': '00-abc-01',
      }),
    );

    expect(error.status, 400);
    expect(error.code, 'VALIDATION_FAILED');
    expect(error.detail, 'Some fields are not valid.');
    expect(error.fieldError('newPassword'), 'Use 8 to 128 characters.');
    expect(error.fieldError('other'), isNull);
    expect(error.traceId, '00-abc-01');
    expect(error.endsSession, isFalse);
  });

  test('only UNAUTHENTICATED and ACCOUNT_DEACTIVATED end the session', () {
    ApiException of(String code) => ApiException.fromResponse(
      _response(401, {'status': 401, 'code': code, 'title': 't', 'detail': 'd'}),
    );

    expect(of('UNAUTHENTICATED').endsSession, isTrue);
    expect(of('ACCOUNT_DEACTIVATED').endsSession, isTrue);
    expect(of('INVALID_CREDENTIALS').endsSession, isFalse);
    expect(of('ACCOUNT_LOCKED').endsSession, isFalse);
  });

  test('a non-envelope body becomes a generic error with the status', () {
    final error = ApiException.fromResponse(_response(502, '<html>Bad gateway</html>'));

    expect(error.code, 'UNEXPECTED_RESPONSE');
    expect(error.status, 502);
  });

  test('connection problems and server start-up are transient', () {
    final offline = ApiException.fromDio(
      DioException(requestOptions: RequestOptions(path: 'x'), type: DioExceptionType.connectionError),
    );
    final starting = ApiException.fromResponse(
      _response(503, {'status': 503, 'code': 'DATABASE_NOT_READY', 'title': 'Starting up', 'detail': 'd'}),
    );

    expect(offline.code, ApiException.networkUnavailable);
    expect(offline.isTransient, isTrue);
    expect(starting.isTransient, isTrue);
  });
}
