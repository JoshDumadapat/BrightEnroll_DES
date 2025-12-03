using System.Text;

namespace BrightEnroll_DES.Services.Business.Reports;

public class ExportService
{
    public string GenerateCsv<T>(List<T> data, Dictionary<string, Func<T, object?>> columnMappings, string fileName = "export")
    {
        var csv = new StringBuilder();
        
        // Add header row
        var headers = columnMappings.Keys.ToList();
        csv.AppendLine(string.Join(",", headers.Select(h => EscapeCsvValue(h))));
        
        // Add data rows
        foreach (var item in data)
        {
            var values = headers.Select(header => 
            {
                var value = columnMappings[header](item);
                return EscapeCsvValue(value?.ToString() ?? "");
            });
            csv.AppendLine(string.Join(",", values));
        }
        
        return csv.ToString();
    }

    private string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        
        // If value contains comma, quote, or newline, wrap in quotes and escape quotes
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        
        return value;
    }
}

