using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BrightEnroll_DES.Services.Business.HR;

public class TimeRecordExcelService
{
    private readonly Random _random = new();
    private static bool _licenseInitialized = false;
    private static readonly object _licenseLock = new object();

    private static void EnsureLicenseInitialized()
    {
        if (!_licenseInitialized)
        {
            lock (_licenseLock)
            {
                if (!_licenseInitialized)
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    _licenseInitialized = true;
                }
            }
        }
    }

    // Generate Excel template for time record uploads
    public byte[] GenerateTemplate(List<EmployeeDisplayDto> employees)
    {
        try
        {
            EnsureLicenseInitialized();
            using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Time Records");

        // Set column headers
        worksheet.Cells[1, 1].Value = "Employee ID";
        worksheet.Cells[1, 2].Value = "Name";
        worksheet.Cells[1, 3].Value = "Role";
        worksheet.Cells[1, 4].Value = "Regular Hours";
        worksheet.Cells[1, 5].Value = "Overtime (hrs)";
        worksheet.Cells[1, 6].Value = "Leave (days)";
        worksheet.Cells[1, 7].Value = "Late (mins)";
        worksheet.Cells[1, 8].Value = "Total Days Absent";
        worksheet.Cells[1, 9].Value = "Total Days Duty";

        // Style the header row
        using (var headerRange = worksheet.Cells[1, 1, 1, 9])
        {
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(59, 130, 246)); // Blue-500
            headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        // Set column widths
        worksheet.Column(1).Width = 15; // Employee ID
        worksheet.Column(2).Width = 25; // Name
        worksheet.Column(3).Width = 20; // Role
        worksheet.Column(4).Width = 15; // Regular Hours
        worksheet.Column(5).Width = 15; // Overtime (hrs)
        worksheet.Column(6).Width = 15; // Leave (days)
        worksheet.Column(7).Width = 15; // Late (mins)
        worksheet.Column(8).Width = 18; // Total Days Absent
        worksheet.Column(9).Width = 18; // Total Days Duty

        // Populate with employee data
        int row = 2;
        if (employees != null && employees.Any())
        {
            // Get current month's working days
            int monthlyDays = GetMonthlyWorkingDays(DateTime.Now);
            
            foreach (var employee in employees)
            {
                // Generate sample attendance data
                var attendanceData = GenerateRandomAttendanceData(monthlyDays);

                worksheet.Cells[row, 1].Value = employee.Id;
                worksheet.Cells[row, 2].Value = employee.Name;
                worksheet.Cells[row, 3].Value = employee.Role;
                worksheet.Cells[row, 4].Value = attendanceData.RegularHours;
                worksheet.Cells[row, 5].Value = attendanceData.OvertimeHours;
                worksheet.Cells[row, 6].Value = attendanceData.LeaveDays;
                worksheet.Cells[row, 7].Value = attendanceData.LateMinutes;
                worksheet.Cells[row, 8].Value = attendanceData.TotalDaysAbsent;
                
                // Calculate total days duty
                worksheet.Cells[row, 9].Formula = $"={monthlyDays}-H{row}";

                row++;
            }
        }

        // Add instructions note after employee data
        int instructionRow = row + 1;
        worksheet.Cells[instructionRow, 1].Value = "Instructions:";
        worksheet.Cells[instructionRow, 1].Style.Font.Bold = true;
        
        var instructions = new List<string>
        {
            "1. Employee ID, Name, and Role are pre-filled from the database",
            "2. Regular Hours: Total regular hours worked (decimal, e.g., 160.00, 176.00)",
            "3. Overtime (hrs): Total overtime hours worked (decimal, e.g., 2.00, 8.50)",
            "4. Leave (days): Number of leave days taken (decimal, e.g., 1.0, 0.5)",
            "5. Late (mins): Total late minutes (whole number, e.g., 15, 30)",
            "6. Total Days Absent: Number of days the employee was absent (whole number, e.g., 0, 1, 3)",
            "7. Total Days Duty: Calculated automatically as Monthly Working Days - Total Days Absent",
            "8. Example: If monthly days = 22 and absent = 2, then Total Days Duty = 20",
            "9. Monthly working days are calculated automatically (typically 22-30 days per month)",
            "10. Base salary is prorated based on Regular Hours worked",
            "11. Overtime pay is calculated at 1.25x the hourly rate",
            "12. You can modify the attendance data as needed before uploading",
            "13. All sample data is randomly generated for testing purposes"
        };

        // Set instructions in merged cell
        worksheet.Cells[instructionRow + 1, 1].Value = string.Join("\n", instructions);
        
        // Merge cells for instructions (span across all columns)
        worksheet.Cells[instructionRow + 1, 1, instructionRow + 13, 9].Merge = true;
        worksheet.Cells[instructionRow + 1, 1, instructionRow + 13, 9].Style.WrapText = true;
        worksheet.Cells[instructionRow + 1, 1, instructionRow + 13, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Top;

        // Freeze header row
        worksheet.View.FreezePanes(2, 1);

        // Auto-fit row height for header
        worksheet.Row(1).Height = 25;

        return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error generating Excel template: {ex.Message}", ex);
        }
    }

    // Get working days in month (excluding weekends)
    private int GetMonthlyWorkingDays(DateTime date)
    {
        int year = date.Year;
        int month = date.Month;
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int workingDays = 0;

        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime currentDate = new DateTime(year, month, day);
            // Count weekdays only
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
        }

        return workingDays;
    }

    // Generate sample attendance data
    private AttendanceData GenerateRandomAttendanceData(int monthlyDays)
    {
        var data = new AttendanceData();
        
        // Generate random absent days
        int absentDays = _random.Next(0, 4);
        data.TotalDaysAbsent = absentDays;
        
        // Random attendance scenario
        var scenario = _random.Next(100);
        
        if (scenario < 5) // On leave
        {
            data.TimeIn = "";
            data.TimeOut = "";
            data.RegularHours = 0;
            data.OvertimeHours = 0;
            data.LeaveDays = _random.Next(1, 3) == 1 ? 1.0m : 0.5m;
            data.LateMinutes = 0;
        }
        else if (scenario < 20) // Late arrival
        {
            // 5-30 minutes late
            var lateMinutes = _random.Next(5, 31);
            var timeInHour = 8;
            var timeInMinute = lateMinutes;
            
            data.TimeIn = $"{timeInHour:D2}:{timeInMinute:D2}";
            data.TimeOut = $"{_random.Next(17, 19):D2}:{_random.Next(0, 60):D2}";
            data.RegularHours = Math.Round(8.0m - (lateMinutes / 60.0m), 2);
            data.OvertimeHours = _random.Next(0, 3) == 0 ? Math.Round((decimal)_random.Next(0, 30) / 60.0m, 2) : 0;
            data.LeaveDays = 0;
            data.LateMinutes = lateMinutes;
        }
        else // Normal attendance
        {
            var timeInHour = 7;
            var timeInMinute = _random.Next(30, 60);
            if (timeInMinute >= 60)
            {
                timeInHour = 8;
                timeInMinute = _random.Next(0, 16);
            }
            
            data.TimeIn = $"{timeInHour:D2}:{timeInMinute:D2}";
            data.TimeOut = $"{_random.Next(17, 18):D2}:{_random.Next(0, 60):D2}";
            data.RegularHours = 8.00m;
            data.OvertimeHours = _random.Next(0, 4) == 0 ? Math.Round((decimal)_random.Next(0, 120) / 60.0m, 2) : 0;
            data.LeaveDays = 0;
            data.LateMinutes = 0;
        }
        
        return data;
    }

    // Parse uploaded Excel file and extract time records
    public List<TimeRecordUploadDto> ParseUploadedFile(byte[] fileBytes)
    {
        try
        {
            EnsureLicenseInitialized();
            var records = new List<TimeRecordUploadDto>();
            
            if (fileBytes == null || fileBytes.Length == 0)
            {
                throw new Exception("File bytes are empty or null.");
            }
            
            using var stream = new MemoryStream(fileBytes);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null)
            {
                throw new Exception("Excel file does not contain any worksheets.");
            }

            // Find data rows
            int startRow = 2;
            int endRow = worksheet.Dimension?.End.Row ?? 0;

            // Stop at instruction row
            for (int row = startRow; row <= endRow; row++)
            {
                var firstCellValue = worksheet.Cells[row, 1].Text?.Trim();
                if (string.IsNullOrEmpty(firstCellValue) || firstCellValue.Equals("Instructions:", StringComparison.OrdinalIgnoreCase))
                {
                    endRow = row - 1;
                    break;
                }
            }

            for (int row = startRow; row <= endRow; row++)
            {
                var employeeId = worksheet.Cells[row, 1].Text?.Trim();
                
                // Skip empty rows
                if (string.IsNullOrWhiteSpace(employeeId))
                    continue;

                var record = new TimeRecordUploadDto
                {
                    EmployeeId = employeeId,
                    Name = worksheet.Cells[row, 2].Text?.Trim() ?? "",
                    Role = worksheet.Cells[row, 3].Text?.Trim() ?? "",
                    TimeIn = "",
                    TimeOut = "",
                    RegularHours = ParseDecimal(worksheet.Cells[row, 4].Text),
                    OvertimeHours = ParseDecimal(worksheet.Cells[row, 5].Text),
                    LeaveDays = ParseDecimal(worksheet.Cells[row, 6].Text),
                    LateMinutes = ParseInt(worksheet.Cells[row, 7].Text),
                    TotalDaysAbsent = ParseInt(worksheet.Cells[row, 8].Text),
                };

                records.Add(record);
            }

            return records;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error parsing Excel file: {ex.Message}", ex);
        }
    }

    private decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;
        
        if (decimal.TryParse(value, out var result))
            return result;
        
        return 0m;
    }

    private int ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;
        
        if (int.TryParse(value, out var result))
            return result;
        
        return 0;
    }

    private class AttendanceData
    {
        public string TimeIn { get; set; } = "";
        public string TimeOut { get; set; } = "";
        public decimal RegularHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal LeaveDays { get; set; }
        public int LateMinutes { get; set; }
        public int TotalDaysAbsent { get; set; }
    }
}

// DTO for time record upload data
public class TimeRecordUploadDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TimeIn { get; set; } = string.Empty;
    public string TimeOut { get; set; } = string.Empty;
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal LeaveDays { get; set; }
    public int LateMinutes { get; set; }
    public int TotalDaysAbsent { get; set; }
}

