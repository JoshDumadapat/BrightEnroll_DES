using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BrightEnroll_DES.Services;
using BrightEnroll_DES.Models;
using BrightEnroll_DES.Components.Pages.Auth.Handlers;
using System;
using System.Linq;

namespace BrightEnroll_DES.Components.Pages.Auth;

public partial class StudentRegistration : ComponentBase, IDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AddressService AddressService { get; set; } = null!;
    [Inject] private SchoolYearService SchoolYearService { get; set; } = null!;
    [Inject] private StudentService StudentService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    
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
    private bool showToast = false;
    private string toastMessage = "";
    private string newSchoolYear = "";
    private string startYear = "";
    private string endYear = "";
    private bool isValidSchoolYearFormat = false;
    private string schoolYearValidationError = "";
    private List<string> availableSchoolYears = new();
    private List<int> availableStartYears = new();
    
    // Address handlers
    private CurrentAddressHandler? currentAddressHandler;
    private PermanentAddressHandler? permanentAddressHandler;
    
    // Place of Birth dropdown
    private bool showPlaceOfBirthDropdown = false;
    private List<string> filteredPlaceOfBirthCities = new();
    private string placeOfBirthSearchText = "";

    protected override void OnInitialized()
    {
        editContext = new EditContext(registrationModel);
        LoadSchoolYears(); // This will also call LoadAvailableStartYears()
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
                // If invalid, update UI to show validation errors
                StateHasChanged();
            }
        }
        
        return Task.CompletedTask;
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
                GradeToEnroll = registrationModel.GradeToEnroll
            };

            // Register student using StudentService
            var registeredStudent = await StudentService.RegisterStudentAsync(studentData);
            
            // Success - navigate to login page, toast will show on destination page
            Navigation.NavigateTo("/login?toast=registration_submitted");
        }
        catch (Exception ex)
        {
            // Handle error - show error message to user
            Console.WriteLine($"Error submitting registration: {ex.Message}");
            toastMessage = $"Registration failed: {ex.Message}";
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
            // Navigate first, then toast will show on destination page
            Navigation.NavigateTo("/?toast=registration_cancelled");
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

    private void CalculateAge()
    {
        if (registrationModel.BirthDate.HasValue)
        {
            var today = DateTime.Today;
            var age = today.Year - registrationModel.BirthDate.Value.Year;
            if (registrationModel.BirthDate.Value.Date > today.AddYears(-age)) age--;
            registrationModel.Age = age;
        }
    }

    private void HandleLRNChange()
    {
        if (registrationModel.HasLRN == "No")
        {
            registrationModel.LearnerReferenceNo = "Pending";
        }
    }

    private void HandleHasLRNChange()
    {
        if (registrationModel.HasLRN == "No")
        {
            registrationModel.LearnerReferenceNo = "Pending";
        }
        else if (registrationModel.HasLRN == "Yes")
        {
            // Clear the field if it was "Pending" so user can enter their LRN
            if (registrationModel.LearnerReferenceNo == "Pending")
            {
                registrationModel.LearnerReferenceNo = "";
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
    private void SelectBarangay(string barangay) => currentAddressHandler?.SelectBarangay(barangay);
    private void SelectCity(string city) => currentAddressHandler?.SelectCity(city);
    private void SelectProvince(string province) => currentAddressHandler?.SelectProvince(province);
    private void SelectCountry(string country) => currentAddressHandler?.SelectCountry(country);
    
    private void HandleCurrentCountryChange()
    {
        // If country is not Philippines, clear Philippine address fields
        if (registrationModel.CurrentCountry != "Philippines")
        {
            registrationModel.CurrentProvince = "";
            registrationModel.CurrentCity = "";
            registrationModel.CurrentBarangay = "";
            registrationModel.CurrentZipCode = "";
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
    private void SelectPermanentBarangay(string barangay) => permanentAddressHandler?.SelectBarangay(barangay);
    private void SelectPermanentCity(string city) => permanentAddressHandler?.SelectCity(city);
    private void SelectPermanentProvince(string province) => permanentAddressHandler?.SelectProvince(province);
    private void SelectPermanentCountry(string country) => permanentAddressHandler?.SelectCountry(country);
    
    private void HandlePermanentCountryChange()
    {
        // If country is not Philippines, clear Philippine address fields
        if (registrationModel.PermanentCountry != "Philippines")
        {
            registrationModel.PermanentProvince = "";
            registrationModel.PermanentCity = "";
            registrationModel.PermanentBarangay = "";
            registrationModel.PermanentZipCode = "";
        }
    }
    
    private string GetPermanentCountryClass()
    {
        var baseClass = "w-full px-3 py-2 sm:px-4 sm:py-2.5 border border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none focus:ring-0 text-sm sm:text-base";
        if (registrationModel.SameAsCurrentAddress || permanentAddressHandler?.IsCountryDisabled() == true)
        {
            return $"{baseClass} bg-gray-100 cursor-not-allowed";
        }
        return baseClass;
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
}

