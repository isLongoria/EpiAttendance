import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../models/attendance_record.dart';
import '../models/attendance_type.dart';
import '../models/month_summary.dart';
import '../models/user_profile.dart';

// Change this to your API's address when deploying.
const _baseUrl = 'http://localhost:5000';
const _tokenKey = 'jwt_token';

class ApiService {
  final Dio _dio;
  final FlutterSecureStorage _storage;

  ApiService()
      : _dio = Dio(BaseOptions(baseUrl: _baseUrl)),
        _storage = const FlutterSecureStorage() {
    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await _storage.read(key: _tokenKey);
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        handler.next(options);
      },
    ));
  }

  // ── Auth ─────────────────────────────────────────────────────────────────

  Future<String> login(String email, String password) async {
    final res = await _dio.post('/api/auth/login',
        data: {'email': email, 'password': password});
    return res.data['token'] as String;
  }

  Future<void> register(String email, String password, String confirmPassword,
      String firstName, String lastName) async {
    await _dio.post('/api/auth/register', data: {
      'email': email,
      'password': password,
      'confirmPassword': confirmPassword,
      'firstName': firstName,
      'lastName': lastName,
    });
  }

  Future<UserProfile> getProfile() async {
    final res = await _dio.get('/api/auth/profile');
    return UserProfile.fromJson(res.data as Map<String, dynamic>);
  }

  // ── Token storage ────────────────────────────────────────────────────────

  Future<String?> getStoredToken() => _storage.read(key: _tokenKey);
  Future<void> storeToken(String token) =>
      _storage.write(key: _tokenKey, value: token);
  Future<void> clearToken() => _storage.delete(key: _tokenKey);

  // ── Attendance ───────────────────────────────────────────────────────────

  Future<MonthSummary> getMonthSummary(int year, int month) async {
    final res = await _dio.get('/api/attendance/summary/$year/$month');
    return MonthSummary.fromJson(res.data as Map<String, dynamic>);
  }

  Future<AttendanceRecord> createOrUpdate(DateTime date, AttendanceType type,
      {String? notes}) async {
    final dateStr =
        '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
    final res = await _dio.post('/api/attendance', data: {
      'date': dateStr,
      'type': type.value,
      if (notes != null) 'notes': notes,
    });
    return AttendanceRecord.fromJson(res.data as Map<String, dynamic>);
  }

  Future<void> deleteAttendance(int id) async {
    await _dio.delete('/api/attendance/$id');
  }
}
