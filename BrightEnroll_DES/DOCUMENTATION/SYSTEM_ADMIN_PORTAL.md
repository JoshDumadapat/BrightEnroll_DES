# ?? System Admin Portal - ERP Vendor Business Management

## Overview

The **System Admin Portal** is the business-side administration interface for your ERP company. It allows you to manage:
- ?? School customers (CRM)
- ?? Sales pipeline and conversions
- ?? Subscription billing and renewals
- ?? Customer support tickets
- ?? Contracts and licensing
- ?? Version updates and feature rollouts

---

## ?? File Structure

```
Components/Pages/SystemAdmin/
??? Dashboard.razor         # Main dashboard with key metrics
??? Customers.razor         # Customer Management (CRM)
??? Sales.razor            # Sales pipeline and leads
??? Subscriptions.razor    # Billing and subscription management
??? Support.razor          # Support ticket system
??? Contracts.razor        # Contracts and licensing
??? Updates.razor          # Version and feature management
```

---

## ?? Module Features

### 1. **Dashboard** (`/system-admin/dashboard`)
**Overview of all business metrics**

**Key Metrics:**
- ?? Total Customers (48 schools)
- ? Active Subscriptions (45)
- ?? Monthly Revenue (?2,450,000)
- ?? Open Support Tickets (12)

**Quick Actions:**
- Navigate to all modules
- View recent sales
- Check expiring subscriptions

---

### 2. **Customer Management** (`/system-admin/customers`)
**CRM-lite for school customers**

**Features:**
- ? School customer list with profiles
- ?? Filter by plan, status
- ?? Summary cards (Total, Active, Expiring, Revenue)
- ?? Customer details:
  - School name & address
  - Contact person & email
  - Subscription plan (Basic/Standard/Premium)
  - Contract duration
  - Monthly fee
  - Status (Active/Inactive/Suspended/Expired)

**Mock Data:**
- 5 sample school customers
- Different subscription plans
- Various contract statuses

---

### 3. **Sales Management** (`/system-admin/sales`)
**Track leads and conversions**

**Features:**
- ?? Sales pipeline visualization (4 stages):
  - New Leads (15)
  - Qualified (8)
  - Proposal Sent (5)
  - Closing (3)
- ?? Revenue tracking
- ?? Sales agent management
- ?? Conversion metrics (75% rate)
- ?? Follow-up reminders (8 due)

**Recent Conversions Table:**
- School name
- Sales agent
- Plan selected
- Amount
- Conversion date

---

### 4. **Subscription Management** (`/system-admin/subscriptions`)
**SaaS billing and renewals**

**Pricing Plans:**

| Plan | Price/Month | Features | Customers |
|------|-------------|----------|-----------|
| **Basic** | ?35,000 | 100 students, 5 users, Basic modules | 15 schools |
| **Standard** | ?55,000 | 500 students, 15 users, All modules | 22 schools |
| **Premium** | ?85,000 | Unlimited, Custom modules, 24/7 support | 8 schools |

**Features:**
- ?? MRR (Monthly Recurring Revenue): ?2,450,000
- ? Expiring subscriptions alerts
- ?? Payment history tracking
- ?? Automated renewal reminders

---

### 5. **Support Ticket System** (`/system-admin/support`)
**Customer support management**

**Metrics:**
- ?? Open Tickets: 12
- ?? In Progress: 8
- ?? Resolved Today: 15
- ?? Avg Response Time: 2.3 hours

**Ticket Management:**
- Priority levels (High/Medium/Low)
- Assignment to support staff
- Status tracking (Open/In Progress/Resolved)
- Response time monitoring

**Sample Tickets:**
- 10 mock support tickets
- Various priorities and statuses
- School-specific issues

---

### 6. **Contracts & Licensing** (`/system-admin/contracts`)
**Legal agreements and module licensing**

**Features:**
- ?? Upload contract PDFs
- ?? User license monitoring
- ?? Module enablement:
  - Admission
  - Finance
  - HR
  - Grades
  - Enrollment
- ?? Contract expiration tracking

**Stats:**
- Active Contracts: 42
- Expiring Soon: 7
- Pending Renewal: 5

---

### 7. **Version Management** (`/system-admin/updates`)
**ERP version and feature control**

**Current Version:** 2.5.0

**Features:**
- ?? Version history with changelog
- ?? Update deployment status
- ??? Feature toggles:
  - Advanced Analytics
  - AI-Powered Insights
  - Mobile App Access
  - API Integration
  - Custom Reports
- ?? Maintenance scheduling

**Stats:**
- Schools on Latest: 38 / 45
- Pending Updates: 7
- Beta Features: 5
- Scheduled Maintenance: 1

---

## ?? Getting Started

### Access the System Admin Portal

1. **Login Credentials:**
   - Role: `System Admin`
   - URL: `/system-admin/dashboard`

2. **Navigation:**
   ```
   Dashboard ? Quick Actions ? Select Module
   ```

3. **Demo Data:**
   - All pages include mock data for demonstration
   - 48 school customers
   - 31 active sales leads
   - 45 active subscriptions
   - 12 open support tickets

---

## ?? UI Design

### Color Scheme:
- **Primary:** Blue (#2563eb) - Main actions
- **Success:** Green (#16a34a) - Active status, revenue
- **Warning:** Orange (#ea580c) - Expiring, pending
- **Danger:** Red (#dc2626) - Expired, high priority
- **Info:** Purple (#9333ea) - Premium features

### Layout:
- Responsive grid system
- Card-based design
- Clean tables with hover effects
- Color-coded status badges
- Interactive buttons with icons

---

## ?? Sample Data Structure

### Customer Info
```csharp
public class CustomerInfo
{
    public string CustomerId { get; set; }
    public string SchoolName { get; set; }
    public string ContactPerson { get; set; }
    public string ContactEmail { get; set; }
    public string Address { get; set; }
    public string Plan { get; set; } // Basic/Standard/Premium
    public decimal MonthlyFee { get; set; }
    public DateTime ContractEndDate { get; set; }
    public string Status { get; set; } // Active/Inactive/Suspended/Expired
}
```

---

## ?? Security & Access Control

### Role-Based Access:
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

## ?? TODO: Backend Integration

### Future Implementation:
1. **Database Tables:**
   - `tbl_Customers` - School customers
   - `tbl_SalesLeads` - Sales pipeline
   - `tbl_Subscriptions` - Billing records
   - `tbl_SupportTickets` - Help desk
   - `tbl_Contracts` - Legal agreements
   - `tbl_VersionHistory` - Update tracking

2. **Services:**
   - `CustomerService` - CRM operations
   - `SalesService` - Lead management
   - `SubscriptionService` - Billing logic
   - `SupportService` - Ticket system
   - `ContractService` - Document management
   - `VersionService` - Update deployment

3. **API Integration:**
   - Payment gateway (e.g., PayMongo, PayPal)
   - Email notifications (SendGrid)
   - File storage (Azure Blob Storage)
   - Analytics (Google Analytics)

---

## ?? Business Metrics

### Revenue Tracking:
- **MRR:** ?2,450,000/month
- **ARR:** ?29,400,000/year
- **ARPU:** ?54,444 (Average Revenue Per User)
- **Churn Rate:** 2% (98% retention)

### Customer Health:
- **Active:** 45 schools (93.75%)
- **Expiring Soon:** 7 schools (14.58%)
- **Conversion Rate:** 75%
- **Support SLA:** 2.3 hours avg response

---

## ?? Update Cycle

### Version Release Process:
1. **Development** ? Beta testing with select schools
2. **QA Testing** ? Internal quality assurance
3. **Staging** ? Deploy to staging environment
4. **Production** ? Gradual rollout to all schools
5. **Monitoring** ? Track adoption and issues

### Feature Rollout:
- Premium features enabled first
- Standard features after 30 days
- Basic features after 60 days

---

## ?? Communication Templates

### Renewal Reminder Email:
```
Subject: Your ERP subscription expires in 30 days

Dear [School Name],

Your subscription to BrightEnroll ERP expires on [Date].

Renewal options:
- Basic Plan: ?35,000/month
- Standard Plan: ?55,000/month
- Premium Plan: ?85,000/month

Contact us: support@brightenroll.com
```

---

## ?? Support

### System Admin Contact:
- **Email:** sysadmin@brightenroll.com
- **Phone:** +63 (2) 8123-4567
- **Hours:** 24/7 (Premium customers)

### Emergency Hotline:
- **Critical Issues:** +63 917-123-4567

---

## ? Checklist for Production

- [ ] Connect to real database
- [ ] Implement authentication
- [ ] Add file upload for contracts
- [ ] Integrate payment gateway
- [ ] Set up email notifications
- [ ] Enable analytics tracking
- [ ] Configure backup system
- [ ] Deploy monitoring tools
- [ ] Create admin user accounts
- [ ] Test all features end-to-end

---

## ?? Summary

The System Admin Portal provides a comprehensive business management interface for your ERP vendor operations:

? **7 Complete Modules**
? **Responsive UI Design**
? **Mock Data for Demo**
? **Role-Based Access Control**
? **Ready for Backend Integration**

**Next Steps:**
1. Test all pages in development
2. Customize for your brand
3. Implement backend services
4. Deploy to production

---

## ?? Related Documentation

- [Cashier Portal Setup](../CASHIER_PORTAL_SETUP.md)
- [Teacher Portal Guide](../TEACHER_PORTAL_QUICKSTART.md)
- [Admin Portal Features](../ADMIN_PORTAL_GUIDE.md)

---

**Created:** 2024
**Version:** 1.0.0
**Status:** ? UI Complete - Ready for Backend Integration
