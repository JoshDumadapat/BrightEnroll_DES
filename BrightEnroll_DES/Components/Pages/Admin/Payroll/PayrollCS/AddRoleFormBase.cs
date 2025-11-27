using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Components.Pages.Admin.Payroll.PayrollCS;
using BrightEnroll_DES.Components;
using BrightEnroll_DES.Services.Business.Payroll;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Linq;

namespace BrightEnroll_DES.Components.Pages.Admin.Payroll.PayrollCS;

public class AddRoleFormBase : ComponentBase
{
    [Parameter] public EventCallback OnRoleAdded { get; set; }
    [Parameter] public List<PayrollRoleData>? Roles { get; set; }
    [Parameter] public EventCallback<(string message, ToastType type)> OnShowToast { get; set; }

    [Inject] protected IRoleService RoleService { get; set; } = null!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;

    protected Role newRole = new Role
    {
        RoleName = string.Empty,
        BaseSalary = 0,
        Allowance = 0,
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
    protected bool isEditingBaseSalary = false;
    protected bool isEditingAllowance = false;
    
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
            var roles = await RoleService.GetAllRolesAsync();

            dbRoles = roles.Select(role =>
            {
                var data = new PayrollRoleData
                {
                    Role = role.RoleName,
                    BaseSalary = role.BaseSalary,
                    Allowance = role.Allowance,
                    IsActive = role.IsActive
                };
                data.Recalculate();
                return data;
            }).ToList();
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading roles from database: {ex.Message}");
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
        var dbRole = await RoleService.GetRoleByNameAsync(role.Role);
        if (dbRole != null)
        {
            editingRoleId = dbRole.RoleId;
            newRole = new Role
            {
                RoleId = dbRole.RoleId,
                RoleName = dbRole.RoleName,
                BaseSalary = dbRole.BaseSalary,
                Allowance = dbRole.Allowance,
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
            validationErrors["BaseSalary"] = "Base salary must be greater than 0";
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
                var existingRole = await RoleService.GetRoleByIdAsync(editingRoleId.Value);
                if (existingRole != null)
                {
                    // Check if role name changed and if new name already exists
                    if (existingRole.RoleName.ToLower() != newRole.RoleName.ToLower())
                    {
                        var exists = await RoleService.RoleExistsAsync(newRole.RoleName, editingRoleId.Value);
                        if (exists)
                        {
                            await ShowToast($"Role '{newRole.RoleName}' already exists!", ToastType.Error);
                            return;
                        }
                    }

                    // Update properties
                    existingRole.RoleName = newRole.RoleName;
                    existingRole.BaseSalary = newRole.BaseSalary;
                    existingRole.Allowance = newRole.Allowance;
                    existingRole.IsActive = newRole.IsActive;
                    
                    await RoleService.UpdateRoleAsync(existingRole);
                    await ShowToast($"Role '{newRole.RoleName}' updated successfully!", ToastType.Success);
                }
            }
            else
            {
                // Add new role - explicitly set IsSynced = false
                newRole.IsSynced = false;
                newRole.CreatedDate = DateTime.Now;
                
                var exists = await RoleService.RoleExistsAsync(newRole.RoleName);
                if (exists)
                {
                    await ShowToast($"Role '{newRole.RoleName}' already exists!", ToastType.Error);
                    return;
                }

                await RoleService.CreateRoleAsync(newRole);
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
            validationErrors["BaseSalary"] = "Base salary must be greater than 0";
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
            IsActive = role.IsActive
        };
        StateHasChanged();
    }

    protected void UpdateRoleInTable(PayrollRoleData role)
    {
        if (editingRoles.ContainsKey(role.Role))
        {
            editingRoles[role.Role].BaseSalary = role.BaseSalary;
            editingRoles[role.Role].Allowance = role.Allowance;
            editingRoles[role.Role].Recalculate();
        }
    }

    protected async Task SaveRoleInTable(PayrollRoleData role)
    {
        if (role.BaseSalary <= 0)
        {
            await ShowToast("Base salary must be greater than 0", ToastType.Error);
            return;
        }

        try
        {
            var dbRole = await RoleService.GetRoleByNameAsync(role.Role);
            if (dbRole != null)
            {
                dbRole.BaseSalary = role.BaseSalary;
                dbRole.Allowance = role.Allowance;
                await RoleService.UpdateRoleAsync(dbRole);

                editingRoles.Remove(role.Role);
                
                // Update the role in dbRoles list for immediate UI update
                var roleInList = dbRoles?.FirstOrDefault(r => r.Role == role.Role);
                if (roleInList != null)
                {
                    roleInList.BaseSalary = role.BaseSalary;
                    roleInList.Allowance = role.Allowance;
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
        StateHasChanged();
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

