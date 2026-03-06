import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:table_calendar/table_calendar.dart';

import '../models/attendance_record.dart';
import '../models/attendance_type.dart';
import '../models/month_summary.dart';
import '../providers/attendance_provider.dart';
import '../providers/auth_provider.dart';
import '../services/api_service.dart';

// ── Colors per type ──────────────────────────────────────────────────────────

Color _typeColor(AttendanceType type) => switch (type) {
      AttendanceType.attended => Colors.green.shade400,
      AttendanceType.pto => Colors.orange.shade400,
      AttendanceType.holiday => Colors.blue.shade400,
      AttendanceType.permission => Colors.purple.shade400,
      AttendanceType.na => Colors.grey.shade300,
    };

// ── Calendar screen ──────────────────────────────────────────────────────────

class CalendarScreen extends ConsumerWidget {
  const CalendarScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selectedMonth = ref.watch(selectedMonthProvider);
    final summaryAsync = ref.watch(monthSummaryProvider(selectedMonth));

    return Scaffold(
      appBar: AppBar(
        title: const Text('Attendance'),
        actions: [
          IconButton(
            icon: const Icon(Icons.person_outline),
            onPressed: () => context.go('/profile'),
          ),
        ],
      ),
      body: summaryAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Error: $e')),
        data: (summary) => _CalendarBody(summary: summary),
      ),
    );
  }
}

// ── Calendar body ────────────────────────────────────────────────────────────

class _CalendarBody extends ConsumerWidget {
  final MonthSummary summary;
  const _CalendarBody({required this.summary});

  /// Build a lookup map from date (midnight) → record for O(1) access.
  Map<DateTime, AttendanceRecord> _buildMap() => {
        for (final r in summary.attendanceRecords)
          DateTime(r.date.year, r.date.month, r.date.day): r,
      };

  void _showDaySheet(
      BuildContext context, DateTime day, AttendanceRecord? existing) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (_) => _DaySheet(day: day, existing: existing),
    );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selectedMonth = ref.watch(selectedMonthProvider);
    final recordMap = _buildMap();

    final met = summary.requirementMet;
    final bannerBg = met ? Colors.green.shade100 : Colors.orange.shade100;
    final bannerFg = met ? Colors.green.shade800 : Colors.orange.shade800;

    return Column(
      children: [
        // ── Progress banner ─────────────────────────────────────────────────
        Container(
          width: double.infinity,
          color: bannerBg,
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          child: Row(
            children: [
              Icon(met ? Icons.check_circle : Icons.info_outline,
                  color: bannerFg, size: 20),
              const SizedBox(width: 8),
              Text(
                '${summary.totalAttendedDays} / ${summary.requiredDays} days attended'
                '${met ? ' — Requirement met!' : ' — ${summary.remainingDays} more needed'}',
                style: TextStyle(
                    color: bannerFg, fontWeight: FontWeight.w600, fontSize: 13),
              ),
            ],
          ),
        ),

        // ── Calendar ────────────────────────────────────────────────────────
        TableCalendar(
          firstDay: DateTime(2020),
          lastDay: DateTime(2030, 12, 31),
          focusedDay: selectedMonth,
          calendarFormat: CalendarFormat.month,
          availableCalendarFormats: const {CalendarFormat.month: 'Month'},
          headerStyle: const HeaderStyle(formatButtonVisible: false),
          onPageChanged: (day) {
            ref.read(selectedMonthProvider.notifier).state =
                DateTime(day.year, day.month);
          },
          onDaySelected: (selectedDay, _) {
            final key = DateTime(
                selectedDay.year, selectedDay.month, selectedDay.day);
            final existing = recordMap[key];
            final paintType = ref.read(paintTypeProvider);
            if (paintType != null) {
              _applyType(context, ref, selectedDay, paintType, existing);
            } else {
              _showDaySheet(context, selectedDay, existing);
            }
          },
          calendarBuilders: CalendarBuilders(
            defaultBuilder: (context, day, _) {
              final key = DateTime(day.year, day.month, day.day);
              final record = recordMap[key];
              return _DayCell(day: day, record: record, isToday: false);
            },
            todayBuilder: (context, day, _) {
              final key = DateTime(day.year, day.month, day.day);
              final record = recordMap[key];
              return _DayCell(day: day, record: record, isToday: true);
            },
            outsideBuilder: (context, day, _) => const SizedBox.shrink(),
          ),
        ),

        // ── Paint bar ───────────────────────────────────────────────────────
        const _PaintBar(),

        // ── Legend ──────────────────────────────────────────────────────────
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
          child: Wrap(
            spacing: 12,
            runSpacing: 4,
            children: AttendanceType.values
                .where((t) => t != AttendanceType.na)
                .map((type) => Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Container(
                          width: 11,
                          height: 11,
                          decoration: BoxDecoration(
                              color: _typeColor(type), shape: BoxShape.circle),
                        ),
                        const SizedBox(width: 4),
                        Text(type.label,
                            style: const TextStyle(fontSize: 12)),
                      ],
                    ))
                .toList(),
          ),
        ),
      ],
    );
  }
}

// ── Paint mode helpers ───────────────────────────────────────────────────────

Future<void> _applyType(
  BuildContext context,
  WidgetRef ref,
  DateTime day,
  AttendanceType type,
  AttendanceRecord? existing,
) async {
  final api = ref.read(apiServiceProvider);
  final month = ref.read(selectedMonthProvider);
  // Tapping the same type again toggles the day off.
  final effectiveType =
      (existing != null && existing.type == type) ? AttendanceType.na : type;
  try {
    if (effectiveType == AttendanceType.na && existing != null) {
      await api.deleteAttendance(existing.id);
    } else if (effectiveType != AttendanceType.na) {
      await api.createOrUpdate(day, effectiveType);
    }
    ref.invalidate(monthSummaryProvider(month));
  } catch (e) {
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Failed to update: $e')),
      );
    }
  }
}

class _PaintBar extends ConsumerWidget {
  const _PaintBar();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final paintType = ref.watch(paintTypeProvider);
    const types = [
      AttendanceType.attended,
      AttendanceType.pto,
      AttendanceType.holiday,
      AttendanceType.permission,
    ];
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      child: Wrap(
        spacing: 6,
        children: [
          ...types.map((t) => ChoiceChip(
                label: Text(t.label),
                selected: paintType == t,
                selectedColor: _typeColor(t),
                onSelected: (on) =>
                    ref.read(paintTypeProvider.notifier).state = on ? t : null,
              )),
          if (paintType != null)
            ActionChip(
              label: const Text('✕ Clear'),
              onPressed: () =>
                  ref.read(paintTypeProvider.notifier).state = null,
            ),
        ],
      ),
    );
  }
}

// ── Day cell widget ──────────────────────────────────────────────────────────

class _DayCell extends StatelessWidget {
  final DateTime day;
  final AttendanceRecord? record;
  final bool isToday;

  const _DayCell(
      {required this.day, required this.record, required this.isToday});

  @override
  Widget build(BuildContext context) {
    final bg = record != null ? _typeColor(record!.type) : null;
    return Container(
      margin: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: bg,
        shape: BoxShape.circle,
        border: isToday ? Border.all(color: Colors.indigo, width: 2) : null,
      ),
      child: Center(
        child: Text(
          '${day.day}',
          style: TextStyle(
              fontWeight: isToday ? FontWeight.bold : FontWeight.normal),
        ),
      ),
    );
  }
}

// ── Day bottom sheet ─────────────────────────────────────────────────────────

class _DaySheet extends ConsumerStatefulWidget {
  final DateTime day;
  final AttendanceRecord? existing;

  const _DaySheet({required this.day, required this.existing});

  @override
  ConsumerState<_DaySheet> createState() => _DaySheetState();
}

class _DaySheetState extends ConsumerState<_DaySheet> {
  late AttendanceType _selected;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _selected = widget.existing?.type ?? AttendanceType.na;
  }

  Future<void> _save() async {
    setState(() => _saving = true);
    try {
      final api = ref.read(apiServiceProvider);
      if (_selected == AttendanceType.na && widget.existing != null) {
        await api.deleteAttendance(widget.existing!.id);
      } else if (_selected != AttendanceType.na) {
        await api.createOrUpdate(widget.day, _selected);
      }
      final month = DateTime(widget.day.year, widget.day.month);
      ref.invalidate(monthSummaryProvider(month));
      if (mounted) Navigator.pop(context);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Error: $e')));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final label = DateFormat('EEEE, MMMM d, y').format(widget.day);
    return Padding(
      padding: EdgeInsets.fromLTRB(
          24, 16, 24, 24 + MediaQuery.of(context).viewInsets.bottom),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label,
              style:
                  const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          ...AttendanceType.values.map((type) => RadioListTile<AttendanceType>(
                value: type,
                groupValue: _selected,
                title: Text(type.label),
                onChanged: (v) => setState(() => _selected = v!),
                contentPadding: EdgeInsets.zero,
                dense: true,
              )),
          const SizedBox(height: 12),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton(
              onPressed: _saving ? null : _save,
              child: _saving
                  ? const SizedBox(
                      height: 20,
                      width: 20,
                      child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Save'),
            ),
          ),
        ],
      ),
    );
  }
}
