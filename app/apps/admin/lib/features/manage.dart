import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../shell.dart';
import '../widgets/admin_widgets.dart';

/// A-08: master data and system settings, grouped Fleet · People · Places · System. The order follows the
/// first-run setup (J7): types → lots → destinations → vehicles → QR labels → staff.
class ManageScreen extends StatelessWidget {
  const ManageScreen({super.key});

  @override
  Widget build(BuildContext context) {
    Widget item(IconData icon, String title, String subtitle, String path, {Tone tone = Tone.ok}) => Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: ListRow(leading: IconTile(icon, tone: tone, size: 40), title: title, subtitle: subtitle, onTap: () => context.push(path)),
    );
    return Scaffold(
      appBar: const TabAppBar(title: 'Manage'),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(DmSize.gutter, 0, DmSize.gutter, 24),
        children: [
          const SectionLabel('Fleet'),
          item(Icons.directions_car_rounded, 'Vehicles', 'Add, edit, status, photo, history', '/vehicles'),
          item(Icons.qr_code_2_rounded, 'QR labels', 'Print, share or save labels for many vehicles', '/qr-labels'),
          item(Icons.category_rounded, 'Categories & types', 'Truck, car… and their types', '/categories'),
          const SectionLabel('People'),
          item(Icons.badge_rounded, 'Staff', 'Drivers: accounts, licences, hours', '/staff', tone: Tone.info),
          item(Icons.admin_panel_settings_rounded, 'Admins', 'Admin accounts', '/admins', tone: Tone.info),
          const SectionLabel('Places'),
          item(Icons.local_parking_rounded, 'Parking lots', 'Capacity per type, hours, contact', '/lots'),
          item(Icons.place_rounded, 'Destinations', 'Areas and locations', '/destinations'),
          const SectionLabel('System'),
          item(Icons.settings_rounded, 'Settings', 'Staff ID prefix, reminder time', '/settings', tone: Tone.neutral),
          item(Icons.history_rounded, 'Audit log', 'Who changed what, and when', '/audit', tone: Tone.neutral),
        ],
      ),
    );
  }
}
