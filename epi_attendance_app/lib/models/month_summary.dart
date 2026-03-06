import 'attendance_record.dart';

class MonthSummary {
  final int year;
  final int month;
  final int totalAttendedDays;
  final int requiredDays;
  final bool requirementMet;
  final int remainingDays;
  final List<AttendanceRecord> attendanceRecords;

  MonthSummary({
    required this.year,
    required this.month,
    required this.totalAttendedDays,
    required this.requiredDays,
    required this.requirementMet,
    required this.remainingDays,
    required this.attendanceRecords,
  });

  factory MonthSummary.fromJson(Map<String, dynamic> json) {
    return MonthSummary(
      year: json['year'] as int,
      month: json['month'] as int,
      totalAttendedDays: json['totalAttendedDays'] as int,
      requiredDays: json['requiredDays'] as int,
      requirementMet: json['requirementMet'] as bool,
      remainingDays: json['remainingDays'] as int,
      attendanceRecords: (json['attendanceRecords'] as List)
          .map((r) => AttendanceRecord.fromJson(r as Map<String, dynamic>))
          .toList(),
    );
  }
}
