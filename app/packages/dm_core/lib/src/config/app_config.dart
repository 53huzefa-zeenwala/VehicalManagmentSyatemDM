/// Build-time settings, passed with `--dart-define-from-file=dart_defines/<app>.json` (D-055).
/// The real files are git-ignored because the repository is public.
abstract final class AppConfig {
  /// Server origin, e.g. `https://<server>:8445` (no `/api/v1`).
  static const String apiBaseUrl = String.fromEnvironment('API_BASE_URL');

  /// Sent as `X-App-Version` so server error logs show which build failed.
  static const String appVersion = String.fromEnvironment('APP_VERSION', defaultValue: '1.0.0');

  static bool get isConfigured => apiBaseUrl.trim().isNotEmpty;

  /// The server origin without a trailing slash.
  static Uri get origin {
    final uri = Uri.parse(apiBaseUrl.trim());
    return uri.replace(path: uri.path.endsWith('/') ? uri.path.substring(0, uri.path.length - 1) : uri.path);
  }
}
