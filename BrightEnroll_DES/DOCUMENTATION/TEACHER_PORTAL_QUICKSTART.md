# Quick Start: Teacher Portal Testing

## ?? Quick Login

### Teacher Account
```
Email: maria.garcia@brightenroll.com
System ID: BDES-1001
Password: Teacher123456
```

### Admin Account (For Comparison)
```
Email: joshvanderson01@gmail.com
System ID: BDES-0001
Password: Admin123456
```

---

## ?? Teacher Menu Items

When logged in as a teacher, you'll see:

1. **Dashboard** ? `/teacher/dashboard`
2. **My Classes** ? `/teacher/my-classes`
3. **My Students** ? `/teacher/my-students`
4. **Grade Entry** ? `/teacher/grade-entry`
5. **My Schedule** ? `/teacher/my-schedule`
6. **Reports** ? `/teacher/reports`
7. **Settings** ? `/settings`
8. **Logout**

---

## ? What Changed

### 1. NavMenu.razor
- Now shows different menus based on `user_role`
- Teachers see teacher-specific navigation
- Admins see admin-specific navigation

### 2. NavigationGuard.razor
- Redirects teachers from admin pages
- Redirects admins from teacher pages
- Redirects to correct dashboard on login

### 3. DatabaseSeeder.cs
- Creates test teacher account automatically
- Includes address, emergency contact, and salary info

---

## ?? Test Checklist

- [ ] Login as teacher using credentials above
- [ ] Verify redirect to `/teacher/dashboard`
- [ ] Check navigation menu shows teacher items only
- [ ] Click through all menu items
- [ ] Try accessing `/dashboard` (should redirect)
- [ ] Logout and login as admin
- [ ] Verify different menu appears
- [ ] Try accessing `/teacher/dashboard` (should redirect)

---

## ?? Build Status

? **All files compiled successfully**
? **No errors found**
? **Ready to test**

---

## ?? Notes

- Mock data is used in teacher pages
- All UI styling matches admin pages
- Database changes are automatic on startup
- No manual database updates needed

---

**Everything is ready! Just run the app and login!** ??
