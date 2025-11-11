using BrightEnroll_DES.Services;

namespace BrightEnroll_DES.Components.Pages.Admin.HRComponents;

// Handles the searchable dropdown functionality for employee address fields (province, city, barangay)
public class EmployeeAddressHandler
{
    private readonly AddressService _addressService;
    private readonly EmployeeFormData _model;
    private readonly Action _stateHasChanged;

    // Dropdown visibility states
    public bool ShowBarangayDropdown { get; set; }
    public bool ShowCityDropdown { get; set; }
    public bool ShowProvinceDropdown { get; set; }

    // Filtered lists for dropdown options
    public List<string> FilteredBarangays { get; set; } = new();
    public List<string> FilteredCities { get; set; } = new();
    public List<string> FilteredProvinces { get; set; } = new();

    // Search text for filtering
    public string BarangaySearchText { get; set; } = "";
    public string CitySearchText { get; set; } = "";
    public string ProvinceSearchText { get; set; } = "";

    public EmployeeAddressHandler(AddressService addressService, EmployeeFormData model, Action stateHasChanged)
    {
        _addressService = addressService;
        _model = model;
        _stateHasChanged = stateHasChanged;
    }

    public void ToggleBarangayDropdown()
    {
        ShowCityDropdown = false;
        ShowProvinceDropdown = false;
        
        ShowBarangayDropdown = !ShowBarangayDropdown;
        if (ShowBarangayDropdown)
        {
            BarangaySearchText = "";
            LoadAllBarangays();
        }
        _stateHasChanged();
    }

    public void ToggleCityDropdown()
    {
        ShowBarangayDropdown = false;
        ShowProvinceDropdown = false;
        
        ShowCityDropdown = !ShowCityDropdown;
        if (ShowCityDropdown)
        {
            CitySearchText = "";
            LoadAllCities();
        }
        _stateHasChanged();
    }

    public void ToggleProvinceDropdown()
    {
        ShowBarangayDropdown = false;
        ShowCityDropdown = false;
        
        ShowProvinceDropdown = !ShowProvinceDropdown;
        if (ShowProvinceDropdown)
        {
            ProvinceSearchText = "";
            LoadAllProvinces();
        }
        _stateHasChanged();
    }

    public void LoadAllBarangays()
    {
        if (string.IsNullOrWhiteSpace(_model.Province) || string.IsNullOrWhiteSpace(_model.City))
        {
            FilteredBarangays = new List<string>();
            return;
        }

        var barangays = _addressService.GetBarangaysByCity(_model.City, _model.Province);
        FilteredBarangays = barangays.OrderBy(b => b).ToList();
    }

    public void LoadAllCities()
    {
        if (string.IsNullOrWhiteSpace(_model.Province))
        {
            FilteredCities = new List<string>();
            return;
        }

        var cities = _addressService.GetCitiesByProvince(_model.Province);
        FilteredCities = cities.OrderBy(c => c).ToList();
    }

    public void LoadAllProvinces()
    {
        var provinces = _addressService.GetAllProvinces();
        FilteredProvinces = provinces.OrderBy(p => p).ToList();
    }

    public void FilterBarangays()
    {
        if (string.IsNullOrWhiteSpace(BarangaySearchText))
        {
            LoadAllBarangays();
            return;
        }

        var allBarangays = _addressService.GetBarangaysByCity(_model.City, _model.Province);
        FilteredBarangays = allBarangays
            .Where(b => b.Contains(BarangaySearchText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(b => b)
            .ToList();
        _stateHasChanged();
    }

    public void FilterCities()
    {
        if (string.IsNullOrWhiteSpace(CitySearchText))
        {
            LoadAllCities();
            return;
        }

        var allCities = _addressService.GetCitiesByProvince(_model.Province);
        FilteredCities = allCities
            .Where(c => c.Contains(CitySearchText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c)
            .ToList();
        _stateHasChanged();
    }

    public void FilterProvinces()
    {
        if (string.IsNullOrWhiteSpace(ProvinceSearchText))
        {
            LoadAllProvinces();
            return;
        }

        var allProvinces = _addressService.GetAllProvinces();
        FilteredProvinces = allProvinces
            .Where(p => p.Contains(ProvinceSearchText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .ToList();
        _stateHasChanged();
    }

    public void SelectBarangay(string barangay)
    {
        _model.Barangay = barangay;
        BarangaySearchText = "";
        ShowBarangayDropdown = false;
        _model.Country = "Philippines";
        UpdateZipCode();
        _stateHasChanged();
    }

    public void SelectCity(string city)
    {
        _model.City = city;
        CitySearchText = "";
        _model.Barangay = ""; // Reset barangay when city changes
        ShowCityDropdown = false;
        _model.Country = "Philippines";
        UpdateZipCode();
        LoadAllBarangays();
        _stateHasChanged();
    }

    public void SelectProvince(string province)
    {
        _model.Province = province;
        ProvinceSearchText = "";
        _model.City = ""; // Reset city when province changes
        // Keep barangay when province changes - don't reset it
        ShowProvinceDropdown = false;
        _model.Country = "Philippines";
        UpdateZipCode();
        LoadAllCities();
        if (ShowBarangayDropdown)
        {
            LoadAllBarangays();
        }
        _stateHasChanged();
    }

    // Automatically updates ZIP code based on selected city and province
    private void UpdateZipCode()
    {
        if (!string.IsNullOrWhiteSpace(_model.City) && !string.IsNullOrWhiteSpace(_model.Province))
        {
            var zipCode = _addressService.GetZipCodeByCity(_model.City, _model.Province);
            if (!string.IsNullOrWhiteSpace(zipCode))
            {
                _model.ZipCode = zipCode;
            }
        }
    }

    public void CloseDropdowns()
    {
        if (ShowBarangayDropdown || ShowCityDropdown || ShowProvinceDropdown)
        {
            ShowBarangayDropdown = false;
            ShowCityDropdown = false;
            ShowProvinceDropdown = false;
            _stateHasChanged();
        }
    }

    // Checks if country field should be disabled (when Philippine address is selected)
    public bool IsCountryDisabled()
    {
        return !string.IsNullOrWhiteSpace(_model.Province) || 
               !string.IsNullOrWhiteSpace(_model.City) || 
               !string.IsNullOrWhiteSpace(_model.Barangay);
    }
}

