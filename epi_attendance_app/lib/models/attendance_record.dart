import 'attendance_type.dart';

class AttendanceRecord {
  final int id;
  final DateTime date;
  final AttendanceType type;
  final String? notes;
  final DateTime createdAt;
  final DateTime? updatedAt;

  AttendanceRecord({
    required this.id,
    required this.date,
    required this.type,
    this.notes,
    required this.createdAt,
    this.updatedAt,
  });

  factory AttendanceRecord.fromJson(Map<String, dynamic> json) {
    return AttendanceRecord(
      id: json['id'] as int,
      date: DateTime.parse(json['date'] as String),
      type: AttendanceType.fromValue(json['type'] as int),
      notes: json['notes'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: json['updatedAt'] != null
          ? DateTime.parse(json['updatedAt'] as String)
          : null,
    );
  }
}
