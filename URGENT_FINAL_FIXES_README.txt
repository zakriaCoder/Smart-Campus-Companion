SmartCampus urgent final fixes

Fixed in this package:
1. Register Student now creates BOTH login account and official student record.
2. Add Student from Academic Records also creates a default login account.
3. New Faculty/HOD registration now also adds that person to the live faculty list, so HOD can assign courses to newly registered faculty.
4. After HOD creates a course, the new course is auto-selected in the Assign Faculty dropdown to avoid assigning the wrong old course.
5. Demo/fallback mode works better on free hosting when SQL Server is not connected.

Important test order:
Registrar -> Register Student -> HOD -> Create Course -> Assign Faculty -> Student -> Enroll -> Faculty -> Mark Attendance.
