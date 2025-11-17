# Database Connection Setup

## Prerequisites
1. SQL Server installed and running
2. Database created with `tbl_Users` table
3. Connection established in Visual Studio Server Explorer

## Connection String Configuration

### Step 1: Get Your Connection String
1. Open Visual Studio
2. Go to **View** > **Server Explorer**
3. Expand **Data Connections**
4. Right-click on your database connection
5. Select **Properties**
6. Copy the **Connection String** value

### Step 2: Update Connection String in Code
1. Open `MauiProgram.cs`
2. Find the line with the connection string (around line 28):
   ```csharp
   string connectionString = "Server=localhost;Database=YourDatabaseName;Integrated Security=true;TrustServerCertificate=true;";
   ```
3. Replace it with your actual connection string from Server Explorer

### Example Connection Strings

**Windows Authentication:**
```
Server=localhost;Database=BrightEnrollDB;Integrated Security=true;TrustServerCertificate=true;
```

**SQL Server Authentication:**
```
Server=localhost;Database=BrightEnrollDB;User Id=your_username;Password=your_password;TrustServerCertificate=true;
```

**Named Instance:**
```
Server=localhost\SQLEXPRESS;Database=BrightEnrollDB;Integrated Security=true;TrustServerCertificate=true;
```

## Database Table Structure

The `tbl_Users` table should have the following structure:

| Column Name | Data Type | Allow Nulls | Description |
|------------|-----------|-------------|-------------|
| user_ID | int | No | Primary Key, Identity |
| system_ID | varchar(50) | No | System identifier (e.g., BDES-0001) |
| first_name | varchar(50) | No | User's first name |
| mid_name | varchar(50) | Yes | User's middle name |
| last_name | varchar(50) | No | User's last name |
| suffix | varchar(10) | Yes | Name suffix (Jr., Sr., etc.) |
| birthdate | date | No | User's birthdate |
| age | tinyint | No | User's age |
| gender | varchar(20) | No | User's gender |
| contact_num | varchar(20) | No | Contact number |
| user_role | varchar(50) | No | User role (Admin, Teacher, etc.) |
| email | varchar(150) | No | Email address (used for login) |
| password | varchar(255) | No | Hashed password (BCrypt) |
| date_hired | datetime | No | Date when user was hired |

## Initial Admin Account

The system will automatically seed the first admin account on startup:

- **System ID:** BDES-0001
- **Name:** Josh Vanderson
- **Email:** joshvanderson01@gmail.com
- **Password:** Admin123456
- **Role:** Admin
- **Contact:** 09366669571
- **Gender:** male
- **Birthdate:** January 1, 2000

**Note:** The password is hashed using BCrypt before being stored in the database.

## Testing the Connection

After updating the connection string, run the application. The system will:
1. Attempt to connect to the database
2. Seed the initial admin account (if it doesn't exist)
3. Allow login with the seeded credentials

## Troubleshooting

### Connection Failed
- Verify SQL Server is running
- Check the connection string is correct
- Ensure the database exists
- Verify Windows Authentication or SQL Server Authentication credentials

### Seeder Not Working
- Check that the `tbl_Users` table exists
- Verify the table structure matches the expected schema
- Check database permissions for INSERT operations

### Login Not Working
- Verify the admin account was seeded successfully
- Check the email and password are correct
- Ensure password hashing is working correctly

