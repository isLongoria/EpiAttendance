import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../services/api_service.dart';

final apiServiceProvider = Provider<ApiService>((ref) => ApiService());

// ── Auth state ───────────────────────────────────────────────────────────────

class AuthState {
  final String? token;
  final bool isLoading;
  final String? error;

  const AuthState({this.token, this.isLoading = false, this.error});

  bool get isAuthenticated => token != null;

  AuthState copyWith({
    String? token,
    bool? isLoading,
    String? error,
    bool clearToken = false,
    bool clearError = false,
  }) {
    return AuthState(
      token: clearToken ? null : (token ?? this.token),
      isLoading: isLoading ?? this.isLoading,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

class AuthNotifier extends StateNotifier<AuthState> {
  final ApiService _api;

  AuthNotifier(this._api) : super(const AuthState(isLoading: true)) {
    _loadToken();
  }

  Future<void> _loadToken() async {
    final token = await _api.getStoredToken();
    state = AuthState(token: token);
  }

  Future<void> login(String email, String password) async {
    state = state.copyWith(isLoading: true, clearError: true);
    try {
      final token = await _api.login(email, password);
      await _api.storeToken(token);
      state = AuthState(token: token);
    } catch (e) {
      state = state.copyWith(
          isLoading: false, error: _message(e), clearToken: false);
    }
  }

  Future<void> register(String email, String password, String confirmPassword,
      String firstName, String lastName) async {
    state = state.copyWith(isLoading: true, clearError: true);
    try {
      await _api.register(email, password, confirmPassword, firstName, lastName);
      await login(email, password);
    } catch (e) {
      state = state.copyWith(isLoading: false, error: _message(e));
    }
  }

  Future<void> logout() async {
    await _api.clearToken();
    state = const AuthState();
  }

  String _message(Object e) {
    if (e is DioException) {
      final data = e.response?.data;
      if (data is Map) return (data['message'] as String?) ?? 'Request failed';
      return e.message ?? 'Request failed';
    }
    return e.toString();
  }
}

final authStateProvider =
    StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier(ref.read(apiServiceProvider));
});
