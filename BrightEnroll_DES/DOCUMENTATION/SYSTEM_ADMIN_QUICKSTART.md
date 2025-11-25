# ?? System Admin Portal - Quick Start Guide

## ?? What You Get

**7 Complete UI Pages** for ERP vendor business management:

1. **Dashboard** - Business overview
2. **Customers** - School CRM
3. **Sales** - Pipeline management
4. **Subscriptions** - Billing & renewals
5. **Support** - Ticket system
6. **Contracts** - Legal agreements
7. **Updates** - Version control

---

## ? Quick Access

### URLs:
```
/system-admin/dashboard       # Main dashboard
/system-admin/customers       # Customer management
/system-admin/sales           # Sales pipeline
/system-admin/subscriptions   # Billing
/system-admin/support         # Support tickets
/system-admin/contracts       # Contracts & licenses
/system-admin/updates         # Version management
```

---

## ?? Key Features

### Dashboard Highlights:
- **48** School Customers
- **?2.45M** Monthly Revenue
- **45** Active Subscriptions
- **12** Open Support Tickets

### Customer Management:
- Filter by plan (Basic/Standard/Premium)
- Track contract expiration
- Monitor subscription status
- View customer profiles

### Sales Pipeline:
- **15** New Leads
- **8** Qualified Prospects
- **5** Proposals Sent
- **3** Closing Deals
- **75%** Conversion Rate

### Subscription Plans:
| Plan | Price | Schools |
|------|-------|---------|
| Basic | ?35,000/mo | 15 |
| Standard | ?55,000/mo | 22 |
| Premium | ?85,000/mo | 8 |

---

## ?? Screenshots & Features

### 1. Dashboard
- **4 Metric Cards**: Customers, Subscriptions, Revenue, Tickets
- **6 Quick Action Buttons**: Navigate to all modules
- **Recent Sales**: Latest conversions
- **Expiring Soon**: Renewal alerts

### 2. Customer Management
- **Filter & Search**: By plan, status, name
- **Summary Cards**: Total, Active, Expiring, Revenue
- **Customer Table**: School info, plans, contract dates
- **Actions**: View details, Edit customer

### 3. Sales Management
- **4-Stage Pipeline**: Visual kanban board
- **Sales Stats**: Leads, conversions, revenue, follow-ups
- **Recent Conversions**: Table of closed deals
- **CTA**: Add new lead button

### 4. Subscriptions
- **Plan Comparison**: 3 pricing tiers with features
- **MRR Tracking**: Monthly recurring revenue
- **Expiring List**: Schools due for renewal
- **Renewal Actions**: Send reminder buttons

### 5. Support Tickets
- **Ticket Stats**: Open, In Progress, Resolved, Response time
- **Priority Levels**: High, Medium, Low
- **Assignment**: Track support staff
- **Status**: Open, In Progress, Resolved

### 6. Contracts & Licensing
- **Active Contracts**: 42 schools
- **Module Management**: Enable/disable features per school
- **Upload Contracts**: PDF document management
- **License Limits**: User caps per plan

### 7. Version Management
- **Current Version**: 2.5.0
- **Update Status**: 38/45 schools updated
- **Feature Toggles**: Enable/disable features
- **Version History**: Changelog & release notes

---

## ?? Access Control

### Role Required:
```csharp
AuthService.CurrentUser?.user_role == "System Admin"
```

### Redirect Logic:
```csharp
protected override void OnInitialized()
{
    if (AuthService.CurrentUser?.user_role != "System Admin")
    {
        Navigation.NavigateTo("/dashboard");
        return;
    }
}
```

---

## ?? Mock Data Included

### Customers (5 schools):
1. St. Mary's Academy - Premium - ?85,000/mo
2. Hope Christian School - Standard - ?55,000/mo
3. Bright Future High School - Basic - ?35,000/mo
4. Golden Gate Academy - Premium (Expired)
5. Sacred Heart School - Standard - ?55,000/mo

### Sales (3 recent conversions):
- St. Mary's Academy - ?85,000
- Hope Christian School - ?55,000
- Bright Future High School - ?35,000

### Support (10 sample tickets):
- Various priorities (High/Medium/Low)
- Different statuses (Open/In Progress/Resolved)
- Assigned to support agents

---

## ?? Next Steps

### For Development:
1. ? UI is complete
2. ?? Backend integration needed
3. ?? Email notifications
4. ?? Payment gateway
5. ?? File upload system

### For Production:
1. Create database tables
2. Implement services
3. Add authentication
4. Deploy to server
5. Train system admins

---

## ?? Tips

### Navigation:
- Use the **Dashboard** as your home base
- **Quick Actions** provide fast access to all modules
- **Back Button** returns to dashboard

### Filtering:
- All tables support **search and filter**
- **Sort** by name, date, or revenue
- **Status badges** are color-coded

### Best Practices:
- Check **Expiring Soon** daily
- Follow up on **Open Tickets** promptly
- Review **Sales Pipeline** weekly
- Monitor **Feature Toggles** for rollouts

---

## ?? Support

For help with System Admin Portal:
- **Email:** dev@brightenroll.com
- **Documentation:** `/DOCUMENTATION/SYSTEM_ADMIN_PORTAL.md`

---

## ? Checklist

- [ ] Access dashboard at `/system-admin/dashboard`
- [ ] Review customer list
- [ ] Check sales pipeline
- [ ] Monitor subscriptions
- [ ] Handle support tickets
- [ ] Manage contracts
- [ ] Review version updates

---

## ?? You're All Set!

The System Admin Portal is ready for demo and testing. All UI components are functional with mock data.

**Happy Managing! ??**
