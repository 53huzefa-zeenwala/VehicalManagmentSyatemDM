import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/models.dart';
import '../net/api_exception.dart';
import '../repos/repos.dart';

/// Where the user is: still checking the stored session, signed out (optionally with why), or signed in.
sealed class SessionState {
  const SessionState();
}

final class SessionChecking extends SessionState {
  const SessionChecking();
}

final class SignedOut extends SessionState {
  const SignedOut({this.message});

  /// Why the user was signed out (session ended, account deactivated), shown on the sign-in screen.
  final String? message;
}

final class SignedIn extends SessionState {
  const SignedIn(this.me);

  final Me me;
}

/// The role this app is for (D-053); each app overrides it.
final appRoleProvider = Provider<UserRole>((ref) => throw UnimplementedError('appRoleProvider must be overridden'));

/// Sign-in state for the whole app. The session itself is the API's HttpOnly cookie, kept by the API client.
class SessionController extends Notifier<SessionState> {
  @override
  SessionState build() {
    ref.watch(apiClientProvider).onSessionEnded = (error) => _endSession(error.detail);
    Future.microtask(restore);
    return const SessionChecking();
  }

  Repos get _repos => ref.read(reposProvider);

  /// On start-up: a stored cookie that is still valid signs the user straight in.
  Future<void> restore() async {
    try {
      state = await _accept(await _repos.me());
    } on ApiException catch (error) {
      // No session, or the server can't be reached: show sign-in (it explains connection problems when used).
      state = SignedOut(message: error.endsSession ? null : (error.isTransient ? null : error.detail));
    }
  }

  /// Throws [ApiException] for wrong ID/password, lockout, deactivated, or connection problems.
  Future<void> login(String loginId, String password) async {
    final me = await _repos.login(loginId, password);
    state = await _accept(me);
    if (state is SignedOut) {
      throw ApiException(
        status: 403,
        code: 'WRONG_APP',
        title: 'Wrong app',
        detail: (state as SignedOut).message!,
      );
    }
  }

  /// Reloads the profile (name, this month's totals, active-trip flag).
  Future<void> refreshMe() async {
    if (state is! SignedIn) {
      return;
    }
    state = await _accept(await _repos.me());
  }

  Future<void> logout() async {
    try {
      await _repos.logout();
    } on ApiException {
      // Sign out locally even if the server can't be reached.
    }
    await ref.read(apiClientProvider).clearSession();
    state = const SignedOut();
  }

  Future<SessionState> _accept(Me me) async {
    final role = ref.read(appRoleProvider);
    if (me.role == role) {
      return SignedIn(me);
    }
    await _repos.logout().catchError((Object _) {});
    await ref.read(apiClientProvider).clearSession();
    return SignedOut(
      message: role == UserRole.staff
          ? 'This app is for staff. Admins use the DM Admin app.'
          : 'This app is for admins. Staff use the DM Staff app.',
    );
  }

  /// The server ended a live session (expired, password changed elsewhere, deactivated). Ignored while the app is
  /// still checking a stored session at start-up: having no session then is normal, not news for the user.
  Future<void> _endSession(String message) async {
    if (state is! SignedIn) {
      return;
    }
    await ref.read(apiClientProvider).clearSession();
    state = SignedOut(message: message);
  }
}

final sessionProvider = NotifierProvider<SessionController, SessionState>(SessionController.new);

/// The signed-in user; only read this inside signed-in screens.
final meProvider = Provider<Me>((ref) {
  final session = ref.watch(sessionProvider);
  if (session is SignedIn) {
    return session.me;
  }
  throw StateError('Not signed in');
});
