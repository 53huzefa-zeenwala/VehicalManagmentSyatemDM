import 'dart:io';
import 'dart:ui';

import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:path_provider/path_provider.dart';

import 'app.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  _handleUnexpectedErrors();

  if (!AppConfig.isConfigured) {
    runApp(const _NotConfiguredApp());
    return;
  }

  // The server uses a self-signed certificate (D-036): trust it for the API host only, also for images.
  HttpOverrides.global = ApiHostCertificateOverrides(AppConfig.origin.host);
  final api = ApiClient.create(
    origin: AppConfig.origin,
    appVersion: AppConfig.appVersion,
    storageDirectory: await getApplicationSupportDirectory(),
  );

  runApp(
    ProviderScope(
      overrides: [
        apiClientProvider.overrideWithValue(api),
        appRoleProvider.overrideWithValue(UserRole.admin),
      ],
      child: const AdminApp(),
    ),
  );
}

/// Release builds show a grey box and log nothing when a widget fails to build. Instead: write the error to the
/// device log (`adb logcat -s flutter`) and show a readable panel in place of the broken part.
void _handleUnexpectedErrors() {
  FlutterError.onError = (details) {
    FlutterError.presentError(details);
    // ignore: avoid_print
    print('UNHANDLED UI ERROR: ${details.exceptionAsString()}\n${details.stack}');
  };
  PlatformDispatcher.instance.onError = (error, stack) {
    // ignore: avoid_print
    print('UNHANDLED ERROR: $error\n$stack');
    return true;
  };
  ErrorWidget.builder = (details) => const Material(
    child: Center(
      child: Padding(
        padding: EdgeInsets.all(24),
        child: Text('Something went wrong on this screen. Go back and try again.', textAlign: TextAlign.center),
      ),
    ),
  );
}

/// Shown when the app was built without `--dart-define-from-file=dart_defines/admin.json`.
class _NotConfiguredApp extends StatelessWidget {
  const _NotConfiguredApp();

  @override
  Widget build(BuildContext context) => const MaterialApp(
    home: Scaffold(
      body: Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Text(
            'This build has no API_BASE_URL.\nBuild with --dart-define-from-file=dart_defines/admin.json',
            textAlign: TextAlign.center,
          ),
        ),
      ),
    ),
  );
}
