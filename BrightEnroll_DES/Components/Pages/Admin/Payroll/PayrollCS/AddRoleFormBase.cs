using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Components.Pages.Admin.Payroll.PayrollCS;
using BrightEnroll_DES.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Linq;

namespace BrightEnroll_DES.Components.Pages.Admin.Payroll.PayrollCS;

public class AddRoleFormBase : ComponentBase
{
    [Parameter] public EventCallback OnRoleAdded { get; set; }
    [Parameter] public List<PayrollRoleData>? Roles { get; set; }
    [Parameter] public EventCallback<(string message, ToastType type)> OnShowToast { get; set; }

    [Inject] protected AppDbContext DbContext { get; set; } = null!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;

    protected Role newRole = new Role
    {
        RoleName = string.Empty,
        BaseSalary = 0,
        Allowance = 0,
        ThresholdPercentage = 0.00m, // Default 0%
        IsActive = true,
        CreatedDate = DateTime.Now
    };

    protected PayrollRoleData? previewData;
    protected bool isSubmitting = false;
    protected bool isEditing = false;
    protected int? editingRoleId = null;
    protected string? originalRoleName = null;
    protected Dictionary<string, string> validationErrors = new();
    
    // For inline table editing
    protected Dictionary<string, PayrollRoleData> editingRoles = new();
    
    // Raw input storage for table editing (per role)
    protected Dictionary<string, string?> tableBaseSalaryRawInput = new();
    protected Dictionary<string, string?> tableAllowanceRawInput = new();
    protected Dictionary<string, string?> tableThresholdPercentageRawInput = new();
    protected Dictionary<string, bool> tableIsEditingBaseSalary = new();
    protected Dictionary<string, bool> tableIsEditingAllowance = new();
    protected Dictionary<string, bool> tableIsEditingThresholdPercentage = new();
    
    // Form visibility
    protected bool showAddForm = false;
    
    // Confirmation modals
    protected bool showAddConfirmModal = false;
    protected bool showSuccessConfirmModal = false;
    
    // Filter variables
    protected string SearchText = string.Empty;
    protected string StatusFilter = string.Empty;
    
    // Pagination state
    protected int CurrentPage { get; set; } = 1;
    protected int RowsPerPage { get; set; } = 5;
    
    // Database-loaded roles
    protected List<PayrollRoleData> dbRoles = new();
    
    // Raw input storage for money fields
    protected string? baseSalaryRawInput = null;
    protected string? allowanceRawInput = null;
    protected string? thresholdPercentageRawInput = null;
    protected bool isEditingBaseSalary = false;
    protected bool isEditingAllowance = false;
    protected bool isEditingThresholdPercentage = false;
    
    // Computed property for filtered roles - uses database roles
    protected List<PayrollRoleData>? FilteredRoles
    {
        get
        {
            if (dbRoles == null || !dbRoles.Any()) return null;
            
            var filtered = dbRoles.AsEnumerable();
            
            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(r => r.Role.ToLower().Contains(searchLower));
            }
            
            // Status filter
            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                bool isActive = StatusFilter == "true";
                filtered = filtered.Where(r => r.IsActive == isActive);
            }
            
            return filtered.ToList();
        }
    }
    
    // Computed property for paginated roles
    protected List<PayrollRoleData>? PaginatedRoles
    {
        get
        {
            if (FilteredRoles == null || !FilteredRoles.Any())
                return null;
            
            int startIndex = (CurrentPage - 1) * RowsPerPage;
            return FilteredRoles.Skip(startIndex).Take(RowsPerPage).ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadRolesFromDatabase();
        RecalculatePreview();
    }
    
    // Load roles directly from database
    protected async Task LoadRolesFromDatabase()
    {
        try
        {
            var roles = await DbContext.Roles
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            dbRoles = roles.Select(role =>
            {
                var data = new PayrollRoleData
                {
                    Role = role.RoleName,
                    BaseSalary = role.BaseSalary,
                    Allowance = role.Allowance,
                    ThresholdPercentage = role.ThresholdPercentage,
                    IsActive = role.IsActive
                };
                data.Recalculate();
                return data;
            }).ToList();
            
            StateHasChanged();
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 207) // Invalid column name
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: threshold_percentage column missing: {sqlEx.Message}");
            dbRoles = new List<PayrollRoleData>();
            // Error will be visible in console - user needs to run migration
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR loading roles: {ex.Message}");
            dbRoles = new List<PayrollRoleData>();
        }
    }

    protected void RecalculatePreview()
    {
        if (!string.IsNullOrWhiteSpace(newRole.RoleName) && newRole.BaseSalary > 0)
        {
            previewData = new PayrollRoleData
            {
                Role = newRole.RoleName,
                BaseSalary = newRole.BaseSalary,
                Allowance = newRole.Allowance,
                ThresholdPercentage = newRole.ThresholdPercentage,
                IsActive = newRole.IsActive
            };
            previewData.Recalculate();
        }
        else
        {
            previewData = null;
        }
        StateHasChanged();
    }

    protected void ToggleAddForm()
    {
        showAddForm = !showAddForm;
        if (!showAddForm)
        {
            HandleReset();
        }
        StateHasChanged();
    }

    protected async Task EditRole(PayrollRoleData role)
    {
        isEditing = true;
        showAddForm = true;
        originalRoleName = role.Role;
        
        // Find the role in database to get the ID
        var dbRole = await DbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == role.Role);
        if (dbRole != null)
        {
            editingRoleId = dbRole.RoleId;
            newRole = new Role
            {
                RoleId = dbRole.RoleId,
                RoleName = dbRole.RoleName,
                BaseSalary = dbRole.BaseSalary,
                Allowance = dbRole.Allowance,
                ThresholdPercentage = dbRole.ThresholdPercentage,
                IsActive = dbRole.IsActive,
                CreatedDate = dbRole.CreatedDate
            };
        }
        else
        {
            newRole = new Role
            {
                RoleName = role.Role,
                BaseSalary = role.BaseSalary,
                Allowance = role.Allowance,
                ThresholdPercentage = role.ThresholdPercentage,
                IsActive = role.IsActive,
                CreatedDate = DateTime.Now
            };
        }
        
        RecalculatePreview();
        StateHasChanged();
    }

    protected async Task HandleSubmit()
    {
        validationErrors.Clear();
        
        // Validation
        if (string.IsNullOrWhiteSpace(newRole.RoleName))
        {
            validationErrors["RoleName"] = "Role name is required";
        }
        else if (newRole.RoleName.Length > 50)
        {
            validationErrors["RoleName"] = "Role name must be 50 characters or less";
        }

        if (newRole.BaseSalary <= 0)
        {
            validationErrors["BaseSalary"] = "Monthly salary must be greater than 0";
        }

        if (validationErrors.Any())
        {
            StateHasChanged();
            return;
        }

        try
        {
            isSubmitting = true;

            if (isEditing && editingRoleId.HasValue)
            {
                // Update existing role
                var existingRole = await DbContext.Roles.FindAsync(editingRoleId.Value);
                if (existingRole != null)
                {
                    // Check if role name changed and if new name already exists
                    if (existingRole.RoleName.ToLower() != newRole.RoleName.ToLower())
                    {
                        var duplicateRole = await DbContext.Roles
                            .FirstOrDefaultAsync(r => r.RoleName.ToLower() == newRole.RoleName.ToLower() && r.RoleId != editingRoleId.Value);
                        if (duplicateRole != null)
                        {
                            await ShowToast($"Role '{newRole.RoleName}' already exists!", ToastType.Error);
                            return;
                        }
                    }

                    existingRole.RoleName = newRole.RoleName;
                    existingRole.BaseSalary = newRole.BaseSalary;
                    existingRole.Allowance = newRole.Allowance;
                    existingRole.ThresholdPercentage = newRole.ThresholdPercentage;
                    existingRole.IsActive = newRole.IsActive;
                    existingRole.UpdatedDate = DateTime.Now;
                    
                    await DbContext.SaveChangesAsync();
                    await ShowToast($"Role '{newRole.RoleName}' updated successfully!", ToastType.Success);
                }
            }
            else
            {
                // Add new role
                var existingRole = await DbContext.Roles
                    .FirstOrDefaultAsync(r => r.RoleName.ToLower() == newRole.RoleName.ToLower());

                if (existingRole != null)
                {
                    await ShowToast($"Role '{newRole.RoleName}' already exists!", ToastType.Error);
                    return;
                }

                newRole.CreatedDate = DateTime.Now;
                DbContext.Roles.Add(newRole);
                await DbContext.SaveChangesAsync();
                await ShowToast($"Role '{newRole.RoleName}' added successfully!", ToastType.Success);
            }

            // Reload roles from database after add/edit
            await LoadRolesFromDatabase();
            await OnRoleAdded.InvokeAsync();
            
            // Show success confirmation modal
            showSuccessConfirmModal = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            string action = isEditing ? "updating" : "adding";
            await ShowToast($"Error {action} role: {ex.Message}", ToastType.Error);
        }
        finally
        {
            isSubmitting = false;
        }
    }

    protected void HandleCancel()
    {
        HandleReset();
        showAddForm = false;
    }

    protected void HandleReset()
    {
        isEditing = false;
        editingRoleId = null;
        originalRoleName = null;
        newRole = new Role
        {
            RoleName = string.Empty,
            BaseSalary = 0,
            Allowance = 0,
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        validationErrors.Clear();
        baseSalaryRawInput = null;
        allowanceRawInput = null;
        isEditingBaseSalary = false;
        isEditingAllowance = false;
        RecalculatePreview();
        StateHasChanged();
    }
    
    // Confirmation modal methods
    protected void ShowAddConfirmModal()
    {
        // Validate first
        validationErrors.Clear();
        
        if (string.IsNullOrWhiteSpace(newRole.RoleName))
        {
            validationErrors["RoleName"] = "Role name is required";
        }
        else if (newRole.RoleName.Length > 50)
        {
            validationErrors["RoleName"] = "Role name must be 50 characters or less";
        }

        if (newRole.BaseSalary <= 0)
        {
            validationErrors["BaseSalary"] = "Monthly salary must be greater than 0";
        }

        if (validationErrors.Any())
        {
            StateHasChanged();
            return;
        }
        
        showAddConfirmModal = true;
        StateHasChanged();
    }
    
    protected void CloseAddConfirmModal()
    {
        showAddConfirmModal = false;
        StateHasChanged();
    }
    
    protected async Task ConfirmAddRole()
    {
        showAddConfirmModal = false;
        await HandleSubmit();
    }
    
    protected void CloseSuccessConfirmModal()
    {
        showSuccessConfirmModal = false;
        HandleReset();
        showAddForm = false;
        StateHasChanged();
    }
    
    protected void AddAnotherRole()
    {
        showSuccessConfirmModal = false;
        HandleReset();
        // Keep form open for adding another role
        StateHasChanged();
    }
    
    // Role name formatting - capitalize first letter, lowercase rest
    protected void HandleRoleNameInput(ChangeEventArgs e)
    {
        var inputValue = e.Value?.ToString() ?? "";
        if (!string.IsNullOrWhiteSpace(inputValue))
        {
            // Capitalize first letter, lowercase the rest
            if (inputValue.Length > 0)
            {
                newRole.RoleName = char.ToUpper(inputValue[0]) + (inputValue.Length > 1 ? inputValue.Substring(1).ToLower() : "");
            }
            else
            {
                newRole.RoleName = "";
            }
        }
        else
        {
            newRole.RoleName = "";
        }
        StateHasChanged();
    }
    
    // Get display value for base salary
    protected string GetBaseSalaryDisplayValue()
    {
        if (isEditingBaseSalary && !string.IsNullOrWhiteSpace(baseSalaryRawInput))
        {
            return FormatNumberWithCommas(baseSalaryRawInput);
        }
        
        if (newRole.BaseSalary > 0)
        {
            return newRole.BaseSalary.ToString("N2");
        }
        return "";
    }
    
    // Get display value for allowance
    protected string GetAllowanceDisplayValue()
    {
        if (isEditingAllowance && !string.IsNullOrWhiteSpace(allowanceRawInput))
        {
            return FormatNumberWithCommas(allowanceRawInput);
        }
        
        if (newRole.Allowance > 0)
        {
            return newRole.Allowance.ToString("N2");
        }
        return "";
    }
    
    // Format number string with commas (for display while typing)
    protected string FormatNumberWithCommas(string numberString)
    {
        if (string.IsNullOrWhiteSpace(numberString) || numberString == ".")
            return numberString;

        // Remove any existing commas
        string cleaned = numberString.Replace(",", "");
        
        // Check if it has a decimal point
        if (cleaned.Contains('.'))
        {
            var parts = cleaned.Split('.');
            string integerPart = parts[0];
            string decimalPart = parts.Length > 1 ? parts[1] : "";
            
            // Format integer part with commas
            if (!string.IsNullOrWhiteSpace(integerPart) && long.TryParse(integerPart, out long intValue))
            {
                string formattedInteger = intValue.ToString("N0").Replace(".", ",");
                return formattedInteger + (parts.Length > 1 ? "." + decimalPart : "");
            }
        }
        else if (long.TryParse(cleaned, out long value))
        {
            return value.ToString("N0").Replace(".", ",");
        }
        
        return numberString;
    }
    
    // Handle salary input with auto-formatting and strict validation
    protected void HandleSalaryInput(ChangeEventArgs e, string fieldName)
    {
        var inputValue = e.Value?.ToString() ?? "";
        
        // IMMEDIATELY filter: Remove ALL non-numeric characters except period
        var validChars = inputValue.Where(c => char.IsDigit(c) || c == '.').ToArray();
        string cleaned = new string(validChars);
        
        // Remove peso sign, commas, spaces if any got through
        cleaned = cleaned.Replace("₱", "").Replace(",", "").Replace(" ", "").Trim();
        
        if (string.IsNullOrWhiteSpace(cleaned) || cleaned == ".")
        {
            cleaned = "";
        }
        else
        {
            // Ensure only one decimal point
            var dotCount = cleaned.Count(c => c == '.');
            if (dotCount > 1)
            {
                var firstDotIndex = cleaned.IndexOf('.');
                cleaned = cleaned.Substring(0, firstDotIndex + 1) + cleaned.Substring(firstDotIndex + 1).Replace(".", "");
            }
            
            // If starting with "0" and next char is a digit (not "."), remove the leading "0"
            if (cleaned.Length > 1 && cleaned[0] == '0' && char.IsDigit(cleaned[1]))
            {
                cleaned = cleaned.Substring(1);
            }
            
            // Limit decimal places to 2
            if (cleaned.Contains('.'))
            {
                var parts = cleaned.Split('.');
                if (parts.Length == 2 && parts[1].Length > 2)
                {
                    cleaned = parts[0] + "." + parts[1].Substring(0, 2);
                }
            }
        }
        
        // Store raw input
        if (fieldName == "BaseSalary")
        {
            baseSalaryRawInput = cleaned;
        }
        else if (fieldName == "Allowance")
        {
            allowanceRawInput = cleaned;
        }
        
        // Parse and update the role's value
        if (!string.IsNullOrWhiteSpace(cleaned) && cleaned != "." && decimal.TryParse(cleaned, out decimal parsedValue))
        {
            parsedValue = Math.Round(parsedValue, 2);
            if (fieldName == "BaseSalary")
            {
                newRole.BaseSalary = parsedValue;
            }
            else if (fieldName == "Allowance")
            {
                newRole.Allowance = parsedValue;
            }
            RecalculatePreview();
        }
        else if (string.IsNullOrWhiteSpace(cleaned) || cleaned == ".")
        {
            if (fieldName == "BaseSalary")
            {
                newRole.BaseSalary = 0;
            }
            else if (fieldName == "Allowance")
            {
                newRole.Allowance = 0;
            }
            RecalculatePreview();
        }
        
        StateHasChanged();
    }
    
    // Handle focus - start editing
    protected void HandleBaseSalaryFocus()
    {
        isEditingBaseSalary = true;
        if (baseSalaryRawInput == null)
        {
            if (newRole.BaseSalary > 0)
            {
                baseSalaryRawInput = newRole.BaseSalary == Math.Floor(newRole.BaseSalary) 
                    ? ((int)newRole.BaseSalary).ToString() 
                    : newRole.BaseSalary.ToString("0.##");
            }
            else
            {
                baseSalaryRawInput = "";
            }
        }
        StateHasChanged();
    }
    
    protected void HandleAllowanceFocus()
    {
        isEditingAllowance = true;
        if (allowanceRawInput == null)
        {
            if (newRole.Allowance > 0)
            {
                allowanceRawInput = newRole.Allowance == Math.Floor(newRole.Allowance) 
                    ? ((int)newRole.Allowance).ToString() 
                    : newRole.Allowance.ToString("0.##");
            }
            else
            {
                allowanceRawInput = "";
            }
        }
        StateHasChanged();
    }
    
    // Handle blur - finish editing
    protected void HandleBaseSalaryBlur()
    {
        isEditingBaseSalary = false;
        
        if (baseSalaryRawInput != null)
        {
            string rawValue = baseSalaryRawInput;
            if (!string.IsNullOrWhiteSpace(rawValue) && rawValue != "." && decimal.TryParse(rawValue, out decimal parsedValue))
            {
                parsedValue = Math.Round(parsedValue, 2);
                newRole.BaseSalary = parsedValue;
            }
            else
            {
                newRole.BaseSalary = 0;
            }
            baseSalaryRawInput = null;
        }
        RecalculatePreview();
        StateHasChanged();
    }
    
    protected void HandleAllowanceBlur()
    {
        isEditingAllowance = false;
        
        if (allowanceRawInput != null)
        {
            string rawValue = allowanceRawInput;
            if (!string.IsNullOrWhiteSpace(rawValue) && rawValue != "." && decimal.TryParse(rawValue, out decimal parsedValue))
            {
                parsedValue = Math.Round(parsedValue, 2);
                newRole.Allowance = parsedValue;
            }
            else
            {
                newRole.Allowance = 0;
            }
            allowanceRawInput = null;
        }
        RecalculatePreview();
        StateHasChanged();
    }
    
    // Threshold Percentage Display and Input Handling
    protected string GetThresholdPercentageDisplayValue()
    {
        if (isEditingThresholdPercentage)
        {
            // When editing, show raw input (can be empty when first focused)
            return thresholdPercentageRawInput ?? "";
        }
        
        // When not editing, show formatted value
        if (newRole.ThresholdPercentage > 0)
        {
            return newRole.ThresholdPercentage.ToString("F2");
        }
        return "0.00";
    }
    
    protected void HandleThresholdPercentageInput(ChangeEventArgs e)
    {
        var inputValue = e.Value?.ToString() ?? "";
        
        // Filter: Remove ALL non-numeric characters except period
        var validChars = inputValue.Where(c => char.IsDigit(c) || c == '.').ToArray();
        string cleaned = new string(validChars);
        
        // Remove any existing commas, spaces
        cleaned = cleaned.Replace(",", "").Replace(" ", "").Trim();
        
        if (string.IsNullOrWhiteSpace(cleaned) || cleaned == ".")
        {
            cleaned = "";
        }
        else
        {
            // Ensure only one decimal point
            var dotCount = cleaned.Count(c => c == '.');
            if (dotCount > 1)
            {
                var firstDotIndex = cleaned.IndexOf('.');
                cleaned = cleaned.Substring(0, firstDotIndex + 1) + cleaned.Substring(firstDotIndex + 1).Replace(".", "");
            }
            
            // Limit decimal places to 2
            if (cleaned.Contains('.'))
            {
                var parts = cleaned.Split('.');
                if (parts.Length == 2 && parts[1].Length > 2)
                {
                    cleaned = parts[0] + "." + parts[1].Substring(0, 2);
                }
            }
        }
        
        // Store raw input
        thresholdPercentageRawInput = cleaned;
        
        // Parse and update the role's value (limit to 0-100)
        if (!string.IsNullOrWhiteSpace(cleaned) && cleaned != "." && decimal.TryParse(cleaned, out decimal parsedValue))
        {
            parsedValue = Math.Round(parsedValue, 2);
            // Limit to 0-100
            if (parsedValue < 0) parsedValue = 0;
            if (parsedValue > 100) parsedValue = 100;
            newRole.ThresholdPercentage = parsedValue;
            RecalculatePreview();
        }
        else if (string.IsNullOrWhiteSpace(cleaned) || cleaned == ".")
        {
            newRole.ThresholdPercentage = 0.00m; // Default
            RecalculatePreview();
        }
        
        StateHasChanged();
    }
    
    protected void HandleThresholdPercentageFocus()
    {
        isEditingThresholdPercentage = true;
        // Clear the field when focused - user can type fresh value
        thresholdPercentageRawInput = "";
        StateHasChanged();
    }
    
    protected void HandleThresholdPercentageBlur()
    {
        isEditingThresholdPercentage = false;
        
        if (thresholdPercentageRawInput != null)
        {
            string rawValue = thresholdPercentageRawInput;
            if (!string.IsNullOrWhiteSpace(rawValue) && rawValue != "." && decimal.TryParse(rawValue, out decimal parsedValue))
            {
                parsedValue = Math.Round(parsedValue, 2);
                if (parsedValue < 0) parsedValue = 0;
                if (parsedValue > 100) parsedValue = 100;
                newRole.ThresholdPercentage = parsedValue;
            }
            else
            {
                newRole.ThresholdPercentage = 0.00m; // Default
            }
            thresholdPercentageRawInput = null;
        }
        RecalculatePreview();
        StateHasChanged();
    }
    
    // Handle paste to filter invalid characters
    protected async Task HandleSalaryPaste(ClipboardEventArgs e, string fieldName)
    {
        try
        {
            var pastedText = await JSRuntime.InvokeAsync<string>("navigator.clipboard.readText");
            if (!string.IsNullOrWhiteSpace(pastedText))
            {
                // Filter to only allow numbers and decimal point
                var validChars = pastedText.Where(c => char.IsDigit(c) || c == '.').ToArray();
                string cleaned = new string(validChars);
                
                // Create a synthetic ChangeEventArgs
                var changeEvent = new ChangeEventArgs { Value = cleaned };
                HandleSalaryInput(changeEvent, fieldName);
            }
        }
        catch
        {
            // Ignore paste errors
        }
    }

    // Inline table editing methods
    protected void StartEditInTable(PayrollRoleData role)
    {
        editingRoles[role.Role] = new PayrollRoleData
        {
            Role = role.Role,
            BaseSalary = role.BaseSalary,
            Allowance = role.Allowance,
            ThresholdPercentage = role.ThresholdPercentage,
            IsActive = role.IsActive
        };
        
        // Initialize raw input state (will be set on focus to clear field)
        tableBaseSalaryRawInput[role.Role] = null;
        tableAllowanceRawInput[role.Role] = null;
        tableThresholdPercentageRawInput[role.Role] = null;
        tableIsEditingBaseSalary[role.Role] = false;
        tableIsEditingAllowance[role.Role] = false;
        tableIsEditingThresholdPercentage[role.Role] = false;
        
        StateHasChanged();
    }

    protected void UpdateRoleInTable(PayrollRoleData role)
    {
        if (editingRoles.ContainsKey(role.Role))
        {
            editingRoles[role.Role].BaseSalary = role.BaseSalary;
            editingRoles[role.Role].Allowance = role.Allowance;
            editingRoles[role.Role].ThresholdPercentage = role.ThresholdPercentage;
            editingRoles[role.Role].Recalculate();
        }
    }

    protected async Task SaveRoleInTable(PayrollRoleData role)
    {
        if (role.BaseSalary <= 0)
        {
            await ShowToast("Monthly salary must be greater than 0", ToastType.Error);
            return;
        }

        // Validate threshold percentage (0-100)
        if (role.ThresholdPercentage < 0 || role.ThresholdPercentage > 100)
        {
            await ShowToast("Threshold percentage must be between 0 and 100", ToastType.Error);
            return;
        }

        try
        {
            var dbRole = await DbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == role.Role);
            if (dbRole != null)
            {
                dbRole.BaseSalary = role.BaseSalary;
                dbRole.Allowance = role.Allowance;
                // Ensure threshold is within valid range
                dbRole.ThresholdPercentage = Math.Max(0, Math.Min(100, role.ThresholdPercentage));
                dbRole.UpdatedDate = DateTime.Now;
                await DbContext.SaveChangesAsync();

                editingRoles.Remove(role.Role);
                
                // Clear raw input state for this role
                tableBaseSalaryRawInput.Remove(role.Role);
                tableAllowanceRawInput.Remove(role.Role);
                tableThresholdPercentageRawInput.Remove(role.Role);
                tableIsEditingBaseSalary.Remove(role.Role);
                tableIsEditingAllowance.Remove(role.Role);
                tableIsEditingThresholdPercentage.Remove(role.Role);
                
                // Update the role in dbRoles list for immediate UI update
                var roleInList = dbRoles?.FirstOrDefault(r => r.Role == role.Role);
                if (roleInList != null)
                {
                    roleInList.BaseSalary = role.BaseSalary;
                    roleInList.Allowance = role.Allowance;
                    roleInList.ThresholdPercentage = role.ThresholdPercentage;
                    roleInList.Recalculate();
                }
                
                // Reload roles from database after update to ensure consistency
                await LoadRolesFromDatabase();
                await OnRoleAdded.InvokeAsync();
                await ShowToast($"Role '{role.Role}' updated successfully!", ToastType.Success);
            }
        }
        catch (Exception ex)
        {
            await ShowToast($"Error updating role: {ex.Message}", ToastType.Error);
        }
        finally
        {
            StateHasChanged();
        }
    }

    protected void CancelEditInTable(string roleName)
    {
        editingRoles.Remove(roleName);
        // Clear raw input state for this role
        tableBaseSalaryRawInput.Remove(roleName);
        tableAllowanceRawInput.Remove(roleName);
        tableThresholdPercentageRawInput.Remove(roleName);
        tableIsEditingBaseSalary.Remove(roleName);
        tableIsEditingAllowance.Remove(roleName);
        tableIsEditingThresholdPercentage.Remove(roleName);
        StateHasChanged();
    }
    
    // Table editing display value methods
    protected string GetTableBaseSalaryDisplayValue(string roleName)
    {
        if (!editingRoles.ContainsKey(roleName)) return "";
        
        var role = editingRoles[roleName];
        if (tableIsEditingBaseSalary.ContainsKey(roleName) && tableIsEditingBaseSalary[roleName] && 
            !string.IsNullOrWhiteSpace(tableBaseSalaryRawInput.GetValueOrDefault(roleName)))
        {
            return FormatNumberWithCommas(tableBaseSalaryRawInput[roleName] ?? "");
        }
        
        if (role.BaseSalary > 0)
        {
            return role.BaseSalary.ToString("N2");
        }
        return "";
    }
    
    protected string GetTableAllowanceDisplayValue(string roleName)
    {
        if (!editingRoles.ContainsKey(roleName)) return "";
        
        var role = editingRoles[roleName];
        if (tableIsEditingAllowance.ContainsKey(roleName) && tableIsEditingAllowance[roleName] && 
            !string.IsNullOrWhiteSpace(tableAllowanceRawInput.GetValueOrDefault(roleName)))
        {
            return FormatNumberWithCommas(tableAllowanceRawInput[roleName] ?? "");
        }
        
        if (role.Allowance > 0)
        {
            return role.Allowance.ToString("N2");
        }
        return "";
    }
    
    protected string GetTableThresholdPercentageDisplayValue(string roleName)
    {
        if (!editingRoles.ContainsKey(roleName)) return "";
        
        var role = editingRoles[roleName];
        if (tableIsEditingThresholdPercentage.ContainsKey(roleName) && tableIsEditingThresholdPercentage[roleName])
        {
            return tableThresholdPercentageRawInput.GetValueOrDefault(roleName) ?? "";
        }
        
        if (role.ThresholdPercentage > 0)
        {
            return role.ThresholdPercentage.ToString("F2");
        }
        return "0.00";
    }
    
    // Table editing input handlers
    protected void HandleTableSalaryInput(ChangeEventArgs e, string fieldName, string roleName)
    {
        if (!editingRoles.ContainsKey(roleName)) return;
        
        var inputValue = e.Value?.ToString() ?? "";
        
        var validChars = inputValue.Where(c => char.IsDigit(c) || c == '.').ToArray();
        string cleaned = new string(validChars);
        
        cleaned = cleaned.Replace("₱", "").Replace(",", "").Replace(" ", "").Trim();
        
        if (string.IsNullOrWhiteSpace(cleaned) || cleaned == ".")
        {
            cleaned = "";
        }
        else
        {
            var dotCount = cleaned.Count(c => c == '.');
            if (dotCount > 1)
            {
                var firstDotIndex = cleaned.IndexOf('.');
                cleaned = cleaned.Substring(0, firstDotIndex + 1) + cleaned.Substring(firstDotIndex + 1).Replace(".", "");
            }
            
            if (cleaned.Length > 1 && cleaned[0] == '0' && char.IsDigit(cleaned[1]))
            {
                cleaned = cleaned.Substring(1);
            }
            
            if (cleaned.Contains('.'))
            {
                var parts = cleaned.Split('.');
                if (parts.Length == 2 && parts[1].Length > 2)
                {
                    cleaned = parts[0] + "." + parts[1].Substring(0, 2);
                }
            }
        }
        
        if (fieldName == "BaseSalary")
        {
            tableBaseSalaryRawInput[roleName] = cleaned;
        }
        else if (fieldName == "Allowance")
        {
            tableAllowanceRawInput[roleName] = cleaned;
        }
        
        if (!string.IsNullOrWhiteSpace(cleaned) && cleaned != "." && decimal.TryParse(cleaned, out decimal parsedValue))
        {
            parsedValue = Math.Round(parsedValue, 2);
            if (fieldName == "BaseSalary")
            {
                editingRoles[roleName].BaseSalary = parsedValue;
            }
            else if (fieldName == "Allowance")
            {
                editingRoles[roleName].Allowance = parsedValue;
            }
            editingRoles[roleName].Recalculate();
        }
        else if (string.IsNullOrWhiteSpace(cleaned) || cleaned == ".")
        {
            if (fieldName == "BaseSalary")
            {
                editingRoles[roleName].BaseSalary = 0;
            }
            else if (fieldName == "Allowance")
            {
                editingRoles[roleName].Allowance = 0;
            }
            editingRoles[roleName].Recalculate();
        }
        
        StateHasChanged();
    }
    
    protected void HandleTableBaseSalaryFocus(string roleName)
    {
        if (!editingRoles.ContainsKey(roleName)) return;
        
        tableIsEditingBaseSalary[roleName] = true;
        if (!tableBaseSalaryRawInput.ContainsKey(roleName) || tableBaseSalaryRawInput[roleName] == null)
        {
            var role = editingRoles[roleName];
            if (role.BaseSalary > 0)
            {
                tableBaseSalaryRawInput[roleName] = role.BaseSalary == Math.Floor(role.BaseSalary) 
                    ? ((int)role.BaseSalary).ToString() 
                    : role.BaseSalary.ToString("0.##");
            }
            else
            {
                tableBaseSalaryRawInput[roleName] = "";
            }
        }
        StateHasChanged();
    }
    
    protected void HandleTableBaseSalaryBlur(string roleName)
    {
        if (!editingRoles.ContainsKey(roleName)) return;
        
        tableIsEditingBaseSalary[roleName] = false;
        
        if (tableBaseSalaryRawInput.ContainsKey(roleName) && tableBaseSalaryRawInput[roleName] != null)
        {
            string rawValue = tableBaseSalaryRawInput[roleName] ?? "";
            if (!string.IsNullOrWhiteSpace(rawValue) && rawValue != "." && decimal.TryParse(rawValue, out decimal parsedValue))
            {
                parsedValue = Math.Round(parsedValue, 2);
                editingRoles[roleName].BaseSalary = parsedValue;
            }
            else
            {
                editingRoles[roleName].BaseSalary = 0;
            }
            tableBaseSalaryRawInput[roleName] = null;
        }
        editingRoles[roleName].Recalculate();
        StateHasChanged();
    }
    
    protected void HandleTableAllowanceFocus(string roleName)
    {
        if (!editingRoles.ContainsKey(roleName)) return;
        
        tableIsEditingAllowance[roleName] = true;
        if (!tableAllowanceRawInput.ContainsKey(roleName) || tableAllowanceRawInput[roleName] == null)
        {
            var role = editingRoles[roleName];
            if (role.Allowance > 0)
            {
                tableAllowanceRawInput[roleName] = role.Allowance == Math.Floor(role.Allowance) 
                    ? ((int)role.Allowance).ToString() 
                    : role.Allowance.ToString("0.##");
            }
            else
            {
                tableAllowanceRawInput[roleName] = "";
            }
        }
        StateHasChanged();
    }
    
    protected void HandleTableAllowanceBlur(string roleName)
    {
        if (!editingRoles.ContainsKey(roleName)) return;
        
        tableIsEditingAllowance[roleName] = false;
        
        if (tableAllowanceRawInput.ContainsKey(roleName) && tableAllowanceRawInput[roleName] != null)
        {
            string rawValue = tableAllowanceRawInput[roleName] ?? "";
            if (!string.IsNullOrWhiteSpace(rawValue) && rawValue != "." && decimal.TryParse(rawValue, out decimal parsedValue))
            {
                parsedValue = Math.Round(parsedValue, 2);
                editingRoles[roleName].Allowance = parsedValue;
            }
            else
            {
                editingRoles[roleName].Allowance = 0;
            }
            tableAllowanceRawInput[roleName] = null;
        }
        editingRoles[roleName].Recalculate();
        StateHasChanged();
    }
    
    protected void HandleTableThresholdPercentageInput(ChangeEventArgs e, string roleName)
    {
        if (!editingRoles.ContainsKey(roleName)) return;
        
        var inputValue = e.Value?.ToString() ?? "";
        
        var validChars = inputValue.Where(c => char.IsDigit(c) || c == '.').ToArray();
        string cleaned = new string(validChars);
        
        cleaned = cleaned.Replace(",", "").Replace(" ", "").Trim();
        
        if (string.IsNullOrWhiteSpace(cleaned) || cleaned == ".")
        {
            cleaned = "";
        }
        else
        {
            var dotCount = cleaned.Count(c => c == '.');
            if (dotCount > 1)
            {
                var firstDotIndex = cleaned.IndexOf('.');
                cleaned = cleaned.Substring(0, firstDotIndex + 1) + cleaned.Substring(firstDotIndex + 1).Replace(".", "");
            }
            
            if (cleaned.Contains('.'))
            {
                var parts = cleaned.Split('.');
                if (parts.Length == 2 && parts[1].Length > 2)
                {
                    cleaned = parts[0] + "." + parts[1].Substring(0, 2);
                }
            }
        }
        
        tableThresholdPercentageRawInput[roleName] = cleaned;
        
        if (!string.IsNullOrWhiteSpace(cleaned) && cleaned != "." && decimal.TryParse(cleaned, out decimal parsedValue))
        {
            parsedValue = Math.Round(parsedValue, 2);
            if (parsedValue < 0) parsedValue = 0;
            if (parsedValue > 100) parsedValue = 100;
            editingRoles[roleName].ThresholdPercentage = parsedValue;
        }
        else if (string.IsNullOrWhiteSpace(cleaned) || cleaned == ".")
        {
            editingRoles[roleName].ThresholdPercentage = 0.00m;
        }
        
        StateHasChanged();
    }
    
    protected void HandleTableThresholdPercentageFocus(string roleName)
    {
        if (!editingRoles.ContainsKey(roleName)) return;
        
        tableIsEditingThresholdPercentage[roleName] = true;
        tableThresholdPercentageRawInput[roleName] = "";
        StateHasChanged();
    }
    
    protected void HandleTableThresholdPercentageBlur(string roleName)
    {
        if (!editingRoles.ContainsKey(roleName)) return;
        
        tableIsEditingThresholdPercentage[roleName] = false;
        
        if (tableThresholdPercentageRawInput.ContainsKey(roleName) && tableThresholdPercentageRawInput[roleName] != null)
        {
            string rawValue = tableThresholdPercentageRawInput[roleName] ?? "";
            if (!string.IsNullOrWhiteSpace(rawValue) && rawValue != "." && decimal.TryParse(rawValue, out decimal parsedValue))
            {
                parsedValue = Math.Round(parsedValue, 2);
                if (parsedValue < 0) parsedValue = 0;
                if (parsedValue > 100) parsedValue = 100;
                editingRoles[roleName].ThresholdPercentage = parsedValue;
            }
            else
            {
                editingRoles[roleName].ThresholdPercentage = 0.00m;
            }
            tableThresholdPercentageRawInput[roleName] = null;
        }
        StateHasChanged();
    }
    
    // Handle paste for table editing
    protected async Task HandleTableSalaryPaste(ClipboardEventArgs e, string fieldName, string roleName)
    {
        try
        {
            var pastedText = await JSRuntime.InvokeAsync<string>("navigator.clipboard.readText");
            if (!string.IsNullOrWhiteSpace(pastedText))
            {
                var validChars = pastedText.Where(c => char.IsDigit(c) || c == '.').ToArray();
                string cleaned = new string(validChars);
                
                var changeEvent = new ChangeEventArgs { Value = cleaned };
                HandleTableSalaryInput(changeEvent, fieldName, roleName);
            }
        }
        catch
        {
            // Ignore paste errors
        }
    }
    
    // Filter methods
    protected void OnSearchChanged()
    {
        CurrentPage = 1; // Reset to first page when search changes
        StateHasChanged();
    }
    
    protected void OnFilterChanged()
    {
        CurrentPage = 1; // Reset to first page when filter changes
        StateHasChanged();
    }
    
    protected void OnClearFilters()
    {
        SearchText = string.Empty;
        StatusFilter = string.Empty;
        CurrentPage = 1;
        StateHasChanged();
    }
    
    // Pagination methods
    protected void HandlePageChanged(int page)
    {
        CurrentPage = page;
        StateHasChanged();
    }
    
    protected void HandleRowsPerPageChanged(int rowsPerPage)
    {
        RowsPerPage = rowsPerPage;
        CurrentPage = 1; // Reset to first page when rows per page changes
        StateHasChanged();
    }
    
    // Helper method to show toast notifications
    protected async Task ShowToast(string message, ToastType type)
    {
        if (OnShowToast.HasDelegate)
        {
            await OnShowToast.InvokeAsync((message, type));
        }
        else
        {
            // Fallback to alert if toast callback is not set
            await JSRuntime.InvokeVoidAsync("alert", message);
        }
    }
}

