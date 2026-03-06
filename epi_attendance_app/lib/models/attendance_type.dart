enum AttendanceType {
  na(0),
  attended(1),
  pto(2),
  holiday(3),
  permission(4);

  const AttendanceType(this.value);
  final int value;

  static AttendanceType fromValue(int v) =>
      AttendanceType.values.firstWhere((e) => e.value == v,
          orElse: () => AttendanceType.na);

  String get label => switch (this) {
        AttendanceType.na => 'N/A',
        AttendanceType.attended => 'Attended',
        AttendanceType.pto => 'PTO',
        AttendanceType.holiday => 'Holiday',
        AttendanceType.permission => 'Permission',
      };
}
