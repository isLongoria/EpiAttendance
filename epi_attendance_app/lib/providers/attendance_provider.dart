import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/attendance_type.dart';
import '../models/month_summary.dart';
import 'auth_provider.dart';

/// The month currently displayed in the calendar.
final selectedMonthProvider = StateProvider<DateTime>((ref) {
  final now = DateTime.now();
  return DateTime(now.year, now.month);
});

/// Fetches the summary for the given month. Auto-invalidated when auth changes.
final monthSummaryProvider =
    FutureProvider.autoDispose.family<MonthSummary, DateTime>((ref, month) async {
  // Re-fetch whenever the auth token changes (e.g. on login / logout).
  ref.watch(authStateProvider);
  final api = ref.read(apiServiceProvider);
  return api.getMonthSummary(month.year, month.month);
});

/// The attendance type currently selected for paint mode, or null when inactive.
final paintTypeProvider = StateProvider<AttendanceType?>((ref) => null);
