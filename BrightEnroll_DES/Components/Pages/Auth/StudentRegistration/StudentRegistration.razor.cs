using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BrightEnroll_DES.Components;
using BrightEnroll_DES.Services.Infrastructure;
using BrightEnroll_DES.Services.Business.Finance;
using BrightEnroll_DES.Services.Business.Academic;
using BrightEnroll_DES.Services.Business.Students;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Components.Pages.Auth.Handlers;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Components.Pages.Auth;

public partial class StudentRegistration : ComponentBase, IDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AddressService AddressService { get; set; } = null!;
    [Inject] private SchoolYearService SchoolYearService { get; set; } = null!;
    [Inject] private StudentService StudentService { get; set; } = null!;
    [Inject] private FeeService FeeService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private ILogger<StudentRegistration>? Logger { get; set; }
    
    private DotNetObjectReference<StudentRegistration>? dotNetRef;

    private EditContext? editContext;

    private StudentRegistrationModel registrationModel = new();
    private bool isSubmitting = false;
    private bool showTermsModal = false;
    private bool showAddSchoolYearModal = false;
    private bool showConfirmModal = false;
    private string confirmModalTitle = "";
    private string confirmModalMessage = "";
    private string confirmModalType = ""; // "submit" or "cancel"
    private bool showValidationModal = false;
    private List<string> missingFields = new();
    private bool showToast = false;
    private string toastMessage = "";
    private ToastType toastType = ToastType.Success;
    private bool showDebugError = false;
    private string debugErrorMessage = "";
    private string debugErrorDetails = "";
    private string newSchoolYear = "";
    private string startYear = "";
    private string endYear = "";
    private bool isValidSchoolYearFormat = false;
    private string schoolYearValidationError = "";
    private List<string> availableSchoolYears = new();
    private List<int> availableStartYears = new();
    private List<GradeLevel> availableGradeLevels = new();
    
    // Address handlers
    private CurrentAddressHandler? currentAddressHandler;
    private PermanentAddressHandler? permanentAddressHandler;
    
    // Place of Birth dropdown
    private bool showPlaceOfBirthDropdown = false;
    private List<string> filteredPlaceOfBirthCities = new();
    private string placeOfBirthSearchText = "";

    protected override async Task OnInitializedAsync()
    {
        editContext = new EditContext(registrationModel);
        LoadSchoolYears(); // This will also call LoadAvailableStartYears()
        await LoadGradeLevelsAsync(); // Load grade levels from database
        registrationModel.StudentType = ""; // Initialize to trigger requirements update
        dotNetRef = DotNetObjectReference.Create(this);
        
        // Initialize address handlers
        currentAddressHandler = new CurrentAddressHandler(AddressService, registrationModel, StateHasChanged);
        permanentAddressHandler = new PermanentAddressHandler(AddressService, registrationModel, StateHasChanged);
        
        // Link handlers to each other for cross-closing dropdowns
        currentAddressHandler.SetPermanentHandler(permanentAddressHandler);
        permanentAddressHandler.SetCurrentHandler(currentAddressHandler);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && dotNetRef != null)
        {
            // Set up click outside handler using JavaScript
            await JSRuntime.InvokeVoidAsync("setupClickOutsideHandler", dotNetRef);
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    public void Dispose()
    {
        dotNetRef?.Dispose();
    }

    private void HandleSearchKeyDown(KeyboardEventArgs e)
    {
        // Prevent Enter key from submitting the form when typing in search fields
        if (e.Key == "Enter")
        {
            // Stop the event from propagating to the form
            // This prevents form submission and validation
        }
    }

    private Task HandleSubmitClick()
    {
        // Only trigger validation when submit button is clicked
        if (editContext != null)
        {
            // Validate the form - this will show validation errors
            var isValid = editContext.Validate();
            
            // Notify all required fields to ensure validation state is updated
            if (!isValid)
            {
                // Explicitly notify dropdown fields to trigger validation display
                // This ensures validation messages are properly associated with the fields
                var placeOfBirthField = new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PlaceOfBirth));
                editContext.NotifyFieldChanged(placeOfBirthField);
                
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentBarangay)));
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentProvince)));
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentCity)));
                
                // Notify permanent address fields if SameAsCurrentAddress is false
                if (!registrationModel.SameAsCurrentAddress)
                {
                    editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentBarangay)));
                    editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentProvince)));
                    editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCity)));
                    editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCountry)));
                    editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentZipCode)));
                }
            }
            
            if (isValid)
            {
                // Show confirmation modal
                confirmModalTitle = "Confirm Registration";
                confirmModalMessage = "Are all the information correct? Do you want to submit the registration?";
                confirmModalType = "submit";
                showConfirmModal = true;
                StateHasChanged();
            }
            else
            {
                // Collect all validation errors
                CollectMissingFields();
                // Show validation error modal
                showValidationModal = true;
                // Force UI update to show validation errors
                StateHasChanged();
            }
        }
        
        return Task.CompletedTask;
    }

    private void CollectMissingFields()
    {
        missingFields.Clear();
        
        if (editContext == null) return;
        
        // Get all fields with validation errors
        var modelType = editContext.Model.GetType();
        var properties = modelType.GetProperties();
        
        foreach (var property in properties)
        {
            var fieldIdentifier = new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, property.Name);
            var validationMessages = editContext.GetValidationMessages(fieldIdentifier);
            
            if (validationMessages.Any())
            {
                // Get user-friendly field name
                var fieldName = GetFieldDisplayName(property.Name);
                if (!string.IsNullOrWhiteSpace(fieldName) && !missingFields.Contains(fieldName))
                {
                    missingFields.Add(fieldName);
                }
            }
        }
    }

    private string ExtractFieldName(string validationMessage)
    {
        // Try to extract field name from common validation message patterns
        if (validationMessage.Contains("required"))
        {
            // Look for field name before "is required"
            var parts = validationMessage.Split(new[] { " is required", " is required.", " field is required" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                return parts[0].Trim();
            }
        }
        return validationMessage;
    }

    private string GetFieldDisplayName(string fieldName)
    {
        // Map field names to user-friendly display names
        var fieldNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "FirstName", "First Name" },
            { "LastName", "Last Name" },
            { "MiddleName", "Middle Name" },
            { "BirthDate", "Birth Date" },
            { "Age", "Age" },
            { "PlaceOfBirth", "Place of Birth" },
            { "Sex", "Sex" },
            { "MotherTongue", "Mother Tongue" },
            { "CurrentBarangay", "Current Address - Barangay" },
            { "CurrentProvince", "Current Address - Province" },
            { "CurrentCity", "Current Address - City" },
            { "CurrentCountry", "Current Address - Country" },
            { "CurrentZipCode", "Current Address - ZIP Code" },
            { "PermanentBarangay", "Permanent Address - Barangay" },
            { "PermanentProvince", "Permanent Address - Province" },
            { "PermanentCity", "Permanent Address - City" },
            { "PermanentCountry", "Permanent Address - Country" },
            { "PermanentZipCode", "Permanent Address - ZIP Code" },
            { "GuardianFirstName", "Guardian - First Name" },
            { "GuardianLastName", "Guardian - Last Name" },
            { "GuardianContactNumber", "Guardian - Contact Number" },
            { "GuardianRelationship", "Guardian - Relationship" },
            { "StudentType", "Student Type" },
            { "LearnerReferenceNo", "Learner Reference Number" },
            { "SchoolYear", "School Year" },
            { "GradeToEnroll", "Grade to Enroll" },
            { "AgreeToTerms", "Terms and Conditions Agreement" }
        };
        
        return fieldNameMap.TryGetValue(fieldName, out var displayName) ? displayName : fieldName;
    }

    private void CloseValidationModal()
    {
        showValidationModal = false;
        missingFields.Clear();
        StateHasChanged();
    }

    private async Task SubmitRegistration()
    {
        isSubmitting = true;
        StateHasChanged(); // Update UI to show loading state
        
        try
        {
            // Map StudentRegistrationModel to StudentRegistrationData
            var studentData = new StudentRegistrationData
            {
                // Student Information
                FirstName = registrationModel.FirstName,
                MiddleName = registrationModel.MiddleName,
                LastName = registrationModel.LastName,
                Suffix = registrationModel.Suffix,
                BirthDate = registrationModel.BirthDate ?? DateTime.Now,
                Age = registrationModel.Age,
                PlaceOfBirth = registrationModel.PlaceOfBirth,
                Sex = registrationModel.Sex,
                MotherTongue = registrationModel.MotherTongue,
                IsIPCommunity = registrationModel.IsIPCommunity,
                IPCommunitySpecify = registrationModel.IPCommunitySpecify,
                Is4PsBeneficiary = registrationModel.Is4PsBeneficiary,
                FourPsHouseholdId = registrationModel.FourPsHouseholdId,

                // Current Address
                CurrentHouseNo = registrationModel.CurrentHouseNo,
                CurrentStreetName = registrationModel.CurrentStreetName,
                CurrentBarangay = registrationModel.CurrentBarangay,
                CurrentCity = registrationModel.CurrentCity,
                CurrentProvince = registrationModel.CurrentProvince,
                CurrentCountry = registrationModel.CurrentCountry,
                CurrentZipCode = registrationModel.CurrentZipCode,

                // Permanent Address
                PermanentHouseNo = registrationModel.PermanentHouseNo,
                PermanentStreetName = registrationModel.PermanentStreetName,
                PermanentBarangay = registrationModel.PermanentBarangay,
                PermanentCity = registrationModel.PermanentCity,
                PermanentProvince = registrationModel.PermanentProvince,
                PermanentCountry = registrationModel.PermanentCountry,
                PermanentZipCode = registrationModel.PermanentZipCode,

                // Guardian Information
                GuardianFirstName = registrationModel.GuardianFirstName,
                GuardianMiddleName = registrationModel.GuardianMiddleName,
                GuardianLastName = registrationModel.GuardianLastName,
                GuardianSuffix = registrationModel.GuardianSuffix,
                GuardianContactNumber = registrationModel.GuardianContactNumber,
                GuardianRelationship = registrationModel.GuardianRelationship,

                // Enrollment Details
                StudentType = registrationModel.StudentType,
                LearnerReferenceNo = registrationModel.LearnerReferenceNo,
                SchoolYear = registrationModel.SchoolYear,
                GradeToEnroll = registrationModel.GradeToEnroll,
                
                // Requirements - New Student
                HasPSABirthCert = registrationModel.HasPSABirthCert,
                HasBaptismalCert = registrationModel.HasBaptismalCert,
                HasReportCard = registrationModel.HasReportCard,
                
                // Requirements - Transferee
                HasForm138 = registrationModel.HasForm138,
                HasForm137 = registrationModel.HasForm137,
                HasGoodMoralCert = registrationModel.HasGoodMoralCert,
                HasTransferCert = registrationModel.HasTransferCert,
                
                // Requirements - Returnee
                HasUpdatedEnrollmentForm = registrationModel.HasUpdatedEnrollmentForm,
                HasClearance = registrationModel.HasClearance
            };

            // Register student using StudentService
            var registeredStudent = await StudentService.RegisterStudentAsync(studentData);
            
            // Success - navigate to login page, toast will show on destination page
            Navigation.NavigateTo("/login?toast=registration_submitted");
        }
        catch (Exception ex)
        {
            // Build comprehensive error details for debugging
            var errorDetails = new System.Text.StringBuilder();
            errorDetails.AppendLine($"Exception Type: {ex.GetType().FullName}");
            errorDetails.AppendLine($"Message: {ex.Message}");
            errorDetails.AppendLine($"Stack Trace: {ex.StackTrace}");
            
            // Recursively get all inner exceptions
            Exception? currentEx = ex.InnerException;
            int depth = 1;
            while (currentEx != null)
            {
                errorDetails.AppendLine($"\n--- Inner Exception #{depth} ---");
                errorDetails.AppendLine($"Type: {currentEx.GetType().FullName}");
                errorDetails.AppendLine($"Message: {currentEx.Message}");
                errorDetails.AppendLine($"Stack Trace: {currentEx.StackTrace}");
                
                // Check for SqlException in inner exceptions
                if (currentEx is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    errorDetails.AppendLine($"\nSQL Error Details:");
                    errorDetails.AppendLine($"  Error Number: {sqlEx.Number}");
                    errorDetails.AppendLine($"  Severity: {sqlEx.Class}");
                    errorDetails.AppendLine($"  State: {sqlEx.State}");
                    errorDetails.AppendLine($"  Procedure: {sqlEx.Procedure ?? "N/A"}");
                    errorDetails.AppendLine($"  Line Number: {sqlEx.LineNumber}");
                    errorDetails.AppendLine($"  Server: {sqlEx.Server ?? "N/A"}");
                }
                
                // Check for DbUpdateException
                if (currentEx is Microsoft.EntityFrameworkCore.DbUpdateException dbUpdateEx)
                {
                    errorDetails.AppendLine($"\nDbUpdateException Details:");
                    errorDetails.AppendLine($"  Entries Count: {dbUpdateEx.Entries?.Count ?? 0}");
                    if (dbUpdateEx.Entries != null && dbUpdateEx.Entries.Any())
                    {
                        foreach (var entry in dbUpdateEx.Entries)
                        {
                            errorDetails.AppendLine($"    Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
                        }
                    }
                }
                
                currentEx = currentEx.InnerException;
                depth++;
            }
            
            // Also check if the main exception is SqlException
            if (ex is Microsoft.Data.SqlClient.SqlException mainSqlEx)
            {
                errorDetails.AppendLine($"\n--- Main SQL Exception ---");
                errorDetails.AppendLine($"Error Number: {mainSqlEx.Number}");
                errorDetails.AppendLine($"Severity: {mainSqlEx.Class}");
                errorDetails.AppendLine($"State: {mainSqlEx.State}");
                errorDetails.AppendLine($"Procedure: {mainSqlEx.Procedure ?? "N/A"}");
                errorDetails.AppendLine($"Line Number: {mainSqlEx.LineNumber}");
                errorDetails.AppendLine($"Server: {mainSqlEx.Server ?? "N/A"}");
            }
            
            // Log to multiple places for debugging
            var fullErrorDetails = errorDetails.ToString();
            
            // Log to ILogger if available
            Logger?.LogError(ex, "STUDENT REGISTRATION ERROR:\n{ErrorDetails}", fullErrorDetails);
            
            // Log to Debug output (visible in Visual Studio Output window)
            Debug.WriteLine("===========================================");
            Debug.WriteLine("STUDENT REGISTRATION ERROR");
            Debug.WriteLine("===========================================");
            Debug.WriteLine(fullErrorDetails);
            Debug.WriteLine("===========================================");
            
            // Log to Console (visible in browser console for Blazor Server)
            Console.WriteLine("===========================================");
            Console.WriteLine("STUDENT REGISTRATION ERROR");
            Console.WriteLine("===========================================");
            Console.WriteLine(fullErrorDetails);
            Console.WriteLine("===========================================");
            
            // Store error details for UI display
            debugErrorMessage = ex.Message;
            debugErrorDetails = fullErrorDetails;
            showDebugError = true;
            
            // Show user-friendly message
            toastMessage = ErrorMessageHelper.ToHumanReadable($"Registration failed: {ex.Message}");
            toastType = ToastType.Error;
            showToast = true;
            isSubmitting = false;
            StateHasChanged();
        }
    }

    private void Cancel()
    {
        // Show confirmation modal
        confirmModalTitle = "Cancel Registration";
        confirmModalMessage = "Are you sure you want to cancel? All entered data will be cleared.";
        confirmModalType = "cancel";
        showConfirmModal = true;
        StateHasChanged();
    }

    private bool HasData()
    {
        return !string.IsNullOrWhiteSpace(registrationModel.FirstName) ||
               !string.IsNullOrWhiteSpace(registrationModel.LastName) ||
               !string.IsNullOrWhiteSpace(registrationModel.GuardianContactNumber) ||
               !string.IsNullOrWhiteSpace(registrationModel.CurrentHouseNo) ||
               !string.IsNullOrWhiteSpace(registrationModel.CurrentStreetName) ||
               !string.IsNullOrWhiteSpace(registrationModel.CurrentBarangay) ||
               !string.IsNullOrWhiteSpace(registrationModel.CurrentCity) ||
               registrationModel.BirthDate.HasValue ||
               !string.IsNullOrWhiteSpace(registrationModel.GradeToEnroll) ||
               !string.IsNullOrWhiteSpace(registrationModel.SchoolYear);
    }

    private void HandleBackdropClick()
    {
        // If nested modals are open, don't close on backdrop click
        if (showConfirmModal || showValidationModal || showAddSchoolYearModal)
        {
            return;
        }

        // If no data entered, allow closing (navigate to home)
        if (!HasData())
        {
            Navigation.NavigateTo("/");
        }
        // If data exists, prevent closing (do nothing)
    }

    private void CloseConfirmModal()
    {
        showConfirmModal = false;
        confirmModalTitle = "";
        confirmModalMessage = "";
        confirmModalType = "";
        StateHasChanged();
    }

    private async Task ConfirmAction()
    {
        showConfirmModal = false;
        StateHasChanged();
        
        if (confirmModalType == "submit")
        {
            await SubmitRegistration();
        }
        else if (confirmModalType == "cancel")
        {
            // Navigate without toast notification for cancel
            Navigation.NavigateTo("/");
        }
        
        confirmModalTitle = "";
        confirmModalMessage = "";
        confirmModalType = "";
    }

    private void CloseToast()
    {
        showToast = false;
        toastMessage = "";
        StateHasChanged();
    }

    private void CloseDebugError()
    {
        showDebugError = false;
        debugErrorMessage = "";
        debugErrorDetails = "";
        StateHasChanged();
    }

    private void OpenTermsModal()
    {
        showTermsModal = true;
    }

    private void CloseTermsModal()
    {
        showTermsModal = false;
        StateHasChanged();
    }

    private void HandleTermsUnderstand()
    {
        // Auto-check the terms checkbox when "I Understand" is clicked
        registrationModel.AgreeToTerms = true;
        // Notify EditContext that AgreeToTerms field has changed to clear validation errors
        if (editContext != null)
        {
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.AgreeToTerms)));
        }
        StateHasChanged();
    }

    private void LoadSchoolYears()
    {
        // Auto-remove finished school years before loading
        SchoolYearService.RemoveFinishedSchoolYears();
        availableSchoolYears = SchoolYearService.GetAvailableSchoolYears();
        // Reload available start years after school years are updated
        LoadAvailableStartYears();
    }

    private async Task LoadGradeLevelsAsync()
    {
        try
        {
            var gradeLevels = await FeeService.GetAllGradeLevelsAsync();
            availableGradeLevels = gradeLevels.OrderBy(g => g.GradeLevelId).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading grade levels: {ex.Message}");
            // Fallback to empty list if database fetch fails
            availableGradeLevels = new List<GradeLevel>();
        }
    }

    private void CalculateAge()
    {
        if (registrationModel.BirthDate.HasValue)
        {
            var today = DateTime.Today;
            var age = today.Year - registrationModel.BirthDate.Value.Year;
            if (registrationModel.BirthDate.Value.Date > today.AddYears(-age)) age--;
            registrationModel.Age = age;
            
            // Notify EditContext that Age field has changed so validation errors are cleared
            if (editContext != null)
            {
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.Age)));
            }
        }
        else
        {
            // Clear age if birth date is cleared
            registrationModel.Age = null;
            if (editContext != null)
            {
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.Age)));
            }
        }
        StateHasChanged();
    }

    private void HandleLRNChange()
    {
        if (registrationModel.HasLRN == "No")
        {
            registrationModel.LearnerReferenceNo = "N/A";
        }
    }

    private void HandleHasLRNChange()
    {
        if (registrationModel.HasLRN == "No")
        {
            registrationModel.LearnerReferenceNo = "N/A";
            // Notify EditContext that LRN field has changed to clear validation errors
            if (editContext != null)
            {
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.LearnerReferenceNo)));
            }
        }
        else if (registrationModel.HasLRN == "Yes")
        {
            // Clear the field if it was "N/A" so user can enter their LRN
            if (registrationModel.LearnerReferenceNo == "N/A")
            {
                registrationModel.LearnerReferenceNo = "";
            }
            // Notify EditContext that LRN field has changed
            if (editContext != null)
            {
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.LearnerReferenceNo)));
            }
        }
        StateHasChanged();
    }

    private void LoadAvailableStartYears()
    {
        availableStartYears.Clear();
        
        // Get current school year's end year based on system calendar
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;
        
        // Determine current school year's end year
        // School year typically starts in June, so:
        // - If we're in June or later (June-December), current SY is Year-Year+1, ends in Year+1
        // - If we're before June (January-May), current SY is Year-1-Year, ends in Year
        int currentEndYear;
        if (currentMonth >= 6)
        {
            currentEndYear = currentYear + 1; // Current SY ends next year (e.g., 2025-2026 ends in 2026)
        }
        else
        {
            currentEndYear = currentYear; // Current SY ends this year (e.g., 2024-2025 ends in 2025)
        }
        
        // Find the latest end year from existing school years
        int latestEndYear = currentEndYear;
        foreach (var sy in availableSchoolYears)
        {
            if (string.IsNullOrWhiteSpace(sy)) continue;
            var parts = sy.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int endYear))
            {
                if (endYear > latestEndYear)
                {
                    latestEndYear = endYear;
                }
            }
        }
        
        // Available start year is only the next year after the latest end year
        // Example: If current SY is 2025-2026 (ends in 2026), latest end year is 2026
        // Next available start year is: 2026 (which creates 2026-2027)
        // After adding 2026-2027, next available start year becomes: 2027 (which creates 2027-2028)
        int nextStartYear = latestEndYear;
        availableStartYears.Add(nextStartYear);
    }

    private void OnStartYearChanged()
    {
        // Auto-set end year to start year + 1
        if (!string.IsNullOrWhiteSpace(startYear) && int.TryParse(startYear, out int start))
        {
            endYear = (start + 1).ToString();
            ValidateSchoolYearFormat();
            StateHasChanged(); // Force UI update for End Year dropdown
        }
        else
        {
            endYear = "";
            isValidSchoolYearFormat = false;
        }
    }

    private void ValidateSchoolYearFormat()
    {
        isValidSchoolYearFormat = false;
        schoolYearValidationError = "";

        // Check if both fields are filled
        if (string.IsNullOrWhiteSpace(startYear) || string.IsNullOrWhiteSpace(endYear))
        {
            schoolYearValidationError = "Both Start Year and End Year are required";
            return;
        }

        // Check if both are valid years
        if (!int.TryParse(startYear, out int start))
        {
            schoolYearValidationError = "Start Year must be a valid year";
            return;
        }

        if (!int.TryParse(endYear, out int end))
        {
            schoolYearValidationError = "End Year must be a valid year";
            return;
        }

        // Validation: End Year - Start Year must equal 1
        int yearGap = end - start;
        if (yearGap != 1)
        {
            schoolYearValidationError = "End Year must be exactly 1 year after Start Year (e.g., 2026-2027)";
            return;
        }

        // All validations passed
        isValidSchoolYearFormat = true;
        newSchoolYear = $"{startYear}-{endYear}";
    }

    private void AddSchoolYear()
    {
        // Check if we've reached the maximum of 3 school years
        if (availableSchoolYears.Count >= 3)
        {
            schoolYearValidationError = "Maximum of 3 school years allowed. Please remove an existing school year before adding a new one.";
            return;
        }

        if (isValidSchoolYearFormat && SchoolYearService.AddSchoolYear(newSchoolYear))
        {
            LoadSchoolYears();
            registrationModel.SchoolYear = newSchoolYear;
            CloseAddSchoolYearModal();
        }
        else if (!isValidSchoolYearFormat)
        {
            // Validation error is already set in ValidateSchoolYearFormat
        }
        else
        {
            schoolYearValidationError = "Failed to add school year. It may already exist.";
        }
    }

    private void OpenAddSchoolYearModal()
    {
        // Reload available start years to ensure they're up to date
        LoadAvailableStartYears();
        
        showAddSchoolYearModal = true;
        newSchoolYear = "";
        startYear = "";
        endYear = "";
        isValidSchoolYearFormat = false;
        
        // Show error message if 3 slots are already occupied
        if (availableSchoolYears.Count >= 3)
        {
            schoolYearValidationError = "Maximum of 3 school years allowed. All slots are currently occupied. Please wait for a school year to finish before adding a new one.";
        }
        else if (availableStartYears.Count > 0)
        {
            // Auto-select the only available start year and set end year
            startYear = availableStartYears[0].ToString();
            OnStartYearChanged(); // This will auto-set the end year
            schoolYearValidationError = "";
        }
        else
        {
            schoolYearValidationError = "";
        }
    }

    private void CloseAddSchoolYearModal()
    {
        showAddSchoolYearModal = false;
        newSchoolYear = "";
        startYear = "";
        endYear = "";
        isValidSchoolYearFormat = false;
        schoolYearValidationError = "";
    }

    // Watch for School Year dropdown change
    private void OnSchoolYearChanged()
    {
        if (registrationModel.SchoolYear == "ADD_NEW")
        {
            OpenAddSchoolYearModal();
            registrationModel.SchoolYear = "";
        }
    }

    // Current Address methods - delegate to handler
    private void ToggleBarangayDropdown()
    {
        // Close permanent address and place of birth dropdowns
        permanentAddressHandler?.CloseDropdowns();
        if (showPlaceOfBirthDropdown)
        {
            showPlaceOfBirthDropdown = false;
        }
        currentAddressHandler?.ToggleBarangayDropdown();
    }
    
    private void ToggleCityDropdown()
    {
        // Close permanent address and place of birth dropdowns
        permanentAddressHandler?.CloseDropdowns();
        if (showPlaceOfBirthDropdown)
        {
            showPlaceOfBirthDropdown = false;
        }
        currentAddressHandler?.ToggleCityDropdown();
    }
    
    private void ToggleProvinceDropdown()
    {
        // Close permanent address and place of birth dropdowns
        permanentAddressHandler?.CloseDropdowns();
        if (showPlaceOfBirthDropdown)
        {
            showPlaceOfBirthDropdown = false;
        }
        currentAddressHandler?.ToggleProvinceDropdown();
    }
    
    private void ToggleCountryDropdown()
    {
        // Close permanent address and place of birth dropdowns
        permanentAddressHandler?.CloseDropdowns();
        if (showPlaceOfBirthDropdown)
        {
            showPlaceOfBirthDropdown = false;
        }
        currentAddressHandler?.ToggleCountryDropdown();
    }
    
    private void FilterBarangays() => currentAddressHandler?.FilterBarangays();
    private void FilterCities() => currentAddressHandler?.FilterCities();
    private void FilterProvinces() => currentAddressHandler?.FilterProvinces();
    private void FilterCountries() => currentAddressHandler?.FilterCountries();
    private void SelectBarangay(string barangay)
    {
        currentAddressHandler?.SelectBarangay(barangay);
        if (editContext != null)
        {
            var fieldIdentifier = new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentBarangay));
            editContext.NotifyFieldChanged(fieldIdentifier);
            // Notify EditContext about auto-set Country and ZipCode
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentCountry)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentZipCode)));
        }
        StateHasChanged();
    }
    private void SelectCity(string city)
    {
        currentAddressHandler?.SelectCity(city);
        if (editContext != null)
        {
            var fieldIdentifier = new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentCity));
            editContext.NotifyFieldChanged(fieldIdentifier);
            // Notify EditContext about auto-set Country and ZipCode
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentCountry)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentZipCode)));
        }
        StateHasChanged();
    }
    private void SelectProvince(string province)
    {
        currentAddressHandler?.SelectProvince(province);
        if (editContext != null)
        {
            var fieldIdentifier = new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentProvince));
            editContext.NotifyFieldChanged(fieldIdentifier);
            // Notify EditContext about auto-set Country (ZipCode is cleared when province changes)
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentCountry)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentZipCode)));
        }
        StateHasChanged();
    }
    private void SelectCountry(string country)
    {
        currentAddressHandler?.SelectCountry(country);
        if (editContext != null)
        {
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentCountry)));
        }
        StateHasChanged();
    }
    
    private void HandleCurrentCountryChange()
    {
        // If country is not Philippines, clear Philippine address fields
        if (registrationModel.CurrentCountry != "Philippines")
        {
            registrationModel.CurrentProvince = "";
            registrationModel.CurrentCity = "";
            registrationModel.CurrentBarangay = "";
            registrationModel.CurrentZipCode = "";
            
            // Notify EditContext about cleared fields
            if (editContext != null)
            {
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentProvince)));
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentCity)));
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentBarangay)));
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.CurrentZipCode)));
            }
        }
    }
    
    private string GetCurrentCountryClass()
    {
        var baseClass = "w-full px-3 py-2 sm:px-4 sm:py-2.5 border border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none focus:ring-0 text-sm sm:text-base";
        if (currentAddressHandler?.IsCountryDisabled() == true)
        {
            return $"{baseClass} bg-gray-100 cursor-not-allowed";
        }
        return baseClass;
    }

    private void CloseAllDropdowns()
    {
        currentAddressHandler?.CloseDropdowns();
        permanentAddressHandler?.CloseDropdowns();
        
        if (showPlaceOfBirthDropdown)
        {
            showPlaceOfBirthDropdown = false;
            StateHasChanged();
        }
    }

    // Permanent Address methods - delegate to handler
    private void TogglePermanentBarangayDropdown()
    {
        // Close current address and place of birth dropdowns
        currentAddressHandler?.CloseDropdowns();
        if (showPlaceOfBirthDropdown)
        {
            showPlaceOfBirthDropdown = false;
        }
        permanentAddressHandler?.ToggleBarangayDropdown();
    }
    
    private void TogglePermanentCityDropdown()
    {
        // Close current address and place of birth dropdowns
        currentAddressHandler?.CloseDropdowns();
        if (showPlaceOfBirthDropdown)
        {
            showPlaceOfBirthDropdown = false;
        }
        permanentAddressHandler?.ToggleCityDropdown();
    }
    
    private void TogglePermanentProvinceDropdown()
    {
        // Close current address and place of birth dropdowns
        currentAddressHandler?.CloseDropdowns();
        if (showPlaceOfBirthDropdown)
        {
            showPlaceOfBirthDropdown = false;
        }
        permanentAddressHandler?.ToggleProvinceDropdown();
    }
    
    private void TogglePermanentCountryDropdown()
    {
        // Close current address and place of birth dropdowns
        currentAddressHandler?.CloseDropdowns();
        if (showPlaceOfBirthDropdown)
        {
            showPlaceOfBirthDropdown = false;
        }
        permanentAddressHandler?.ToggleCountryDropdown();
    }
    
    private void FilterPermanentBarangays() => permanentAddressHandler?.FilterBarangays();
    private void FilterPermanentCities() => permanentAddressHandler?.FilterCities();
    private void FilterPermanentProvinces() => permanentAddressHandler?.FilterProvinces();
    private void FilterPermanentCountries() => permanentAddressHandler?.FilterCountries();
    private void SelectPermanentBarangay(string barangay)
    {
        permanentAddressHandler?.SelectBarangay(barangay);
        if (editContext != null)
        {
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentBarangay)));
            // Notify about auto-set fields
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCountry)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentZipCode)));
        }
        StateHasChanged();
    }
    
    private void SelectPermanentCity(string city)
    {
        permanentAddressHandler?.SelectCity(city);
        if (editContext != null)
        {
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCity)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentProvince)));
            // Notify about auto-set fields
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCountry)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentZipCode)));
        }
        StateHasChanged();
    }
    
    private void SelectPermanentProvince(string province)
    {
        permanentAddressHandler?.SelectProvince(province);
        if (editContext != null)
        {
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentProvince)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCity)));
            // Notify about auto-set fields
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCountry)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentZipCode)));
        }
        StateHasChanged();
    }
    
    private void SelectPermanentCountry(string country)
    {
        permanentAddressHandler?.SelectCountry(country);
        if (editContext != null)
        {
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCountry)));
            // If country changed, notify about cleared fields
            if (country != "Philippines")
            {
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentProvince)));
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCity)));
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentBarangay)));
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentZipCode)));
            }
        }
        StateHasChanged();
    }
    
    private void HandlePermanentCountryChange()
    {
        // If country is not Philippines, clear Philippine address fields
        if (registrationModel.PermanentCountry != "Philippines")
        {
            registrationModel.PermanentProvince = "";
            registrationModel.PermanentCity = "";
            registrationModel.PermanentBarangay = "";
            registrationModel.PermanentZipCode = "";
            
            // Notify EditContext of field changes
            if (editContext != null)
            {
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentProvince)));
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCity)));
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentBarangay)));
                editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentZipCode)));
            }
        }
        
        // Notify EditContext about all permanent address fields to trigger validation
        if (editContext != null)
        {
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCountry)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentProvince)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCity)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentBarangay)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentZipCode)));
        }
        StateHasChanged();
    }
    
    // Returns CSS class for permanent address dropdown based on validation
    private string GetPermanentDropdownClass(string fieldName)
    {
        if (editContext == null) 
        {
            var baseClass = "flex w-full cursor-pointer items-center justify-between rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus-within:border-blue-500 focus-within:outline-none focus-within:ring-0 sm:px-4 sm:py-2.5 sm:text-base";
            if (registrationModel.SameAsCurrentAddress)
            {
                return $"{baseClass} bg-gray-100 cursor-not-allowed";
            }
            return baseClass;
        }

        var fieldIdentifier = new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, fieldName);
        var hasError = editContext.GetValidationMessages(fieldIdentifier).Any();
        var isModified = editContext.IsModified(fieldIdentifier);
        
        var baseClassWithState = "flex w-full cursor-pointer items-center justify-between rounded-lg border bg-white px-3 py-2 text-sm focus-within:outline-none focus-within:ring-0 sm:px-4 sm:py-2.5 sm:text-base";
        
        if (registrationModel.SameAsCurrentAddress)
        {
            baseClassWithState = $"{baseClassWithState} bg-gray-100 cursor-not-allowed";
        }
        
        if (hasError)
        {
            return $"{baseClassWithState} border-red-500 focus-within:border-red-500";
        }
        
        // Show green border for valid fields that have been modified
        if (isModified && !hasError && !registrationModel.SameAsCurrentAddress)
        {
            return $"{baseClassWithState} border-green-500 focus-within:border-green-500";
        }
        
        return $"{baseClassWithState} border-gray-300 focus-within:border-blue-500";
    }

    private string GetPermanentCountryClass()
    {
        if (editContext == null)
        {
            var defaultBaseClass = "w-full px-3 py-2 sm:px-4 sm:py-2.5 border border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none focus:ring-0 text-sm sm:text-base";
            if (registrationModel.SameAsCurrentAddress || permanentAddressHandler?.IsCountryDisabled() == true)
            {
                return $"{defaultBaseClass} bg-gray-100 cursor-not-allowed";
            }
            return defaultBaseClass;
        }

        var fieldIdentifier = new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCountry));
        var hasError = editContext.GetValidationMessages(fieldIdentifier).Any();
        
        var baseClass = "w-full px-3 py-2 sm:px-4 sm:py-2.5 border rounded-lg focus:outline-none focus:ring-0 text-sm sm:text-base";
        
        if (registrationModel.SameAsCurrentAddress || permanentAddressHandler?.IsCountryDisabled() == true)
        {
            baseClass = $"{baseClass} bg-gray-100 cursor-not-allowed";
        }
        
        if (hasError)
        {
            return $"{baseClass} border-red-500 focus:border-red-500";
        }
        
        return $"{baseClass} border-gray-300 focus:border-blue-500";
    }

    private string GetPermanentZipCodeClass()
    {
        if (editContext == null)
        {
            var defaultBaseClass = "w-full px-3 py-2 sm:px-4 sm:py-2.5 border border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none focus:ring-0 text-sm sm:text-base";
            if (registrationModel.SameAsCurrentAddress || permanentAddressHandler?.IsCountryDisabled() == true)
            {
                return $"{defaultBaseClass} bg-gray-100 cursor-not-allowed";
            }
            return defaultBaseClass;
        }

        var fieldIdentifier = new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentZipCode));
        var hasError = editContext.GetValidationMessages(fieldIdentifier).Any();
        
        var baseClass = "w-full px-3 py-2 sm:px-4 sm:py-2.5 border rounded-lg focus:outline-none focus:ring-0 text-sm sm:text-base";
        
        if (registrationModel.SameAsCurrentAddress || permanentAddressHandler?.IsCountryDisabled() == true)
        {
            baseClass = $"{baseClass} bg-gray-100 cursor-not-allowed";
        }
        
        if (hasError)
        {
            return $"{baseClass} border-red-500 focus:border-red-500";
        }
        
        return $"{baseClass} border-gray-300 focus:border-blue-500";
    }

    // Format name to capitalize first letter of each word, rest lowercase
    private string FormatName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        // Split by spaces and format each word
        var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var formattedWords = words.Select(word =>
        {
            if (string.IsNullOrEmpty(word))
                return word;
            
            // First letter uppercase, rest lowercase
            return char.ToUpper(word[0]) + word.Substring(1).ToLower();
        });

        return string.Join(" ", formattedWords);
    }

    // Name formatting handlers
    private void FormatFirstName()
    {
        registrationModel.FirstName = FormatName(registrationModel.FirstName);
    }

    private void FormatMiddleName()
    {
        registrationModel.MiddleName = FormatName(registrationModel.MiddleName);
    }

    private void FormatLastName()
    {
        registrationModel.LastName = FormatName(registrationModel.LastName);
    }

    private void FormatGuardianFirstName()
    {
        registrationModel.GuardianFirstName = FormatName(registrationModel.GuardianFirstName);
    }

    private void FormatGuardianMiddleName()
    {
        registrationModel.GuardianMiddleName = FormatName(registrationModel.GuardianMiddleName);
    }

    private void FormatGuardianLastName()
    {
        registrationModel.GuardianLastName = FormatName(registrationModel.GuardianLastName);
    }



    // Place of Birth dropdown methods
    private void TogglePlaceOfBirthDropdown()
    {
        // Close other dropdowns first
        currentAddressHandler?.CloseDropdowns();
        permanentAddressHandler?.CloseDropdowns();
        
        showPlaceOfBirthDropdown = !showPlaceOfBirthDropdown;
        if (showPlaceOfBirthDropdown)
        {
            placeOfBirthSearchText = "";
            LoadAllPlaceOfBirthCities();
        }
        StateHasChanged();
    }

    private void LoadAllPlaceOfBirthCities()
    {
        var allCities = new List<string>();
        foreach (var province in AddressService.GetAllProvinces())
        {
            allCities.AddRange(AddressService.GetCitiesByProvince(province));
        }
        filteredPlaceOfBirthCities = allCities.Distinct().OrderBy(c => c).ToList();
    }

    private void FilterPlaceOfBirth()
    {
        if (string.IsNullOrWhiteSpace(placeOfBirthSearchText))
        {
            LoadAllPlaceOfBirthCities();
            return;
        }
        var allCities = new List<string>();
        LoadAllPlaceOfBirthCities();
        allCities = filteredPlaceOfBirthCities;
        filteredPlaceOfBirthCities = allCities
            .Where(c => c.ToLower().Contains(placeOfBirthSearchText.ToLower()))
            .OrderBy(c => c)
            .ToList();
        StateHasChanged();
    }

    private void SelectPlaceOfBirth(string city)
    {
        registrationModel.PlaceOfBirth = city;
        placeOfBirthSearchText = "";
        showPlaceOfBirthDropdown = false;
        if (editContext != null)
        {
            var fieldIdentifier = new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PlaceOfBirth));
            editContext.NotifyFieldChanged(fieldIdentifier);
        }
        StateHasChanged();
    }

    private void HandleSameAsCurrentAddressChange()
    {
        if (registrationModel.SameAsCurrentAddress)
        {
            // Copy current address to permanent address
            registrationModel.PermanentHouseNo = registrationModel.CurrentHouseNo;
            registrationModel.PermanentStreetName = registrationModel.CurrentStreetName;
            registrationModel.PermanentBarangay = registrationModel.CurrentBarangay;
            registrationModel.PermanentCity = registrationModel.CurrentCity;
            registrationModel.PermanentProvince = registrationModel.CurrentProvince;
            registrationModel.PermanentCountry = registrationModel.CurrentCountry;
            registrationModel.PermanentZipCode = registrationModel.CurrentZipCode;
        }
        else
        {
            // Clear permanent address when unchecked
            registrationModel.PermanentHouseNo = "";
            registrationModel.PermanentStreetName = "";
            registrationModel.PermanentBarangay = "";
            registrationModel.PermanentCity = "";
            registrationModel.PermanentProvince = "";
            registrationModel.PermanentCountry = "";
            registrationModel.PermanentZipCode = "";
        }
        
        // Notify EditContext about all permanent address fields to trigger validation
        // This ensures validation re-runs when checkbox state changes
        if (editContext != null)
        {
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.SameAsCurrentAddress)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentBarangay)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCity)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentProvince)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentCountry)));
            editContext.NotifyFieldChanged(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, nameof(registrationModel.PermanentZipCode)));
        }
        StateHasChanged();
    }

    private void HandleFormClick()
    {
        // Close all dropdowns when clicking anywhere in the form (but not on dropdown elements themselves)
        // The dropdown elements have @onclick:stopPropagation to prevent this from firing
        CloseAllDropdowns();
    }

    [JSInvokable]
    public void CloseDropdownsFromJS()
    {
        // Called from JavaScript when clicking outside dropdowns
        CloseAllDropdowns();
    }

    // Returns CSS class for input fields based on validation
    private string GetInputClass(string fieldName)
    {
        if (editContext == null) 
            return "w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-0 sm:px-4 sm:py-2.5 sm:text-base";

        var fieldIdentifier = new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, fieldName);
        var hasError = editContext.GetValidationMessages(fieldIdentifier).Any();
        var isModified = editContext.IsModified(fieldIdentifier);
        
        var baseClass = "w-full rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-0 sm:px-4 sm:py-2.5 sm:text-base";
        
        if (hasError)
        {
            return $"{baseClass} border-red-500 focus:border-red-500";
        }
        
        // Show green border for valid fields that have been modified
        if (isModified && !hasError)
        {
            return $"{baseClass} border-green-500 focus:border-green-500";
        }
        
        return $"{baseClass} border-gray-300 focus:border-blue-500";
    }

    // Returns CSS class for dropdown fields based on validation
    private string GetDropdownClass(string fieldName)
    {
        var baseClass = "flex w-full cursor-pointer items-center justify-between rounded-lg border bg-white px-3 py-2 text-sm focus-within:outline-none focus-within:ring-0 sm:px-4 sm:py-2.5 sm:text-base";
        
        if (editContext == null) 
            return $"{baseClass} border-gray-300 focus-within:border-blue-500";

        var fieldIdentifier = new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(editContext.Model, fieldName);
        var validationMessages = editContext.GetValidationMessages(fieldIdentifier);
        var hasError = validationMessages.Any();
        var isModified = editContext.IsModified(fieldIdentifier);
        
        // Explicitly check for validation errors
        if (hasError)
        {
            return $"{baseClass} border-red-500 focus-within:border-red-500";
        }
        
        // Show green border for valid fields that have been modified
        if (isModified && !hasError)
        {
            return $"{baseClass} border-green-500 focus-within:border-green-500";
        }
        
        return $"{baseClass} border-gray-300 focus-within:border-blue-500";
    }

    // Returns CSS class for input fields with extra classes
    private string GetInputClassWithExtra(string fieldName, string extraClasses = "")
    {
        var baseClass = GetInputClass(fieldName);
        if (!string.IsNullOrWhiteSpace(extraClasses))
        {
            return $"{baseClass} {extraClasses}";
        }
        return baseClass;
    }

    // Returns CSS class for input fields that can be disabled
    private string GetInputClassWithDisabled(string fieldName, bool isDisabled)
    {
        var baseClass = GetInputClass(fieldName);
        if (isDisabled)
        {
            return $"{baseClass} bg-gray-100 cursor-not-allowed";
        }
        return baseClass;
    }
}

