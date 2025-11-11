using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services;

namespace BrightEnroll_DES.Components.Pages.Auth.Handlers;

public class PermanentAddressHandler
{
    private readonly AddressService _addressService;
    private readonly StudentRegistrationModel _model;
    private readonly Action _stateHasChanged;
    private CurrentAddressHandler? _currentHandler;

    // Dropdown state
    public bool ShowBarangayDropdown { get; set; }
    public bool ShowCityDropdown { get; set; }
    public bool ShowProvinceDropdown { get; set; }
    public bool ShowCountryDropdown { get; set; }

    // Filtered lists
    public List<string> FilteredBarangays { get; set; } = new();
    public List<string> FilteredCities { get; set; } = new();
    public List<string> FilteredProvinces { get; set; } = new();
    public List<string> FilteredCountries { get; set; } = new();

    // Search text
    public string BarangaySearchText { get; set; } = "";
    public string CitySearchText { get; set; } = "";
    public string ProvinceSearchText { get; set; } = "";
    public string CountrySearchText { get; set; } = "";

    public PermanentAddressHandler(AddressService addressService, StudentRegistrationModel model, Action stateHasChanged)
    {
        _addressService = addressService;
        _model = model;
        _stateHasChanged = stateHasChanged;
    }

    public void SetCurrentHandler(CurrentAddressHandler currentHandler)
    {
        _currentHandler = currentHandler;
    }

    public void ToggleBarangayDropdown()
    {
        if (_model.SameAsCurrentAddress) return;
        
        // Close other dropdowns
        ShowCityDropdown = false;
        ShowProvinceDropdown = false;
        ShowCountryDropdown = false;
        _currentHandler?.CloseDropdowns();
        
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
        if (_model.SameAsCurrentAddress) return;
        
        // Close other dropdowns
        ShowBarangayDropdown = false;
        ShowProvinceDropdown = false;
        ShowCountryDropdown = false;
        _currentHandler?.CloseDropdowns();
        
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
        if (_model.SameAsCurrentAddress) return;
        
        // Close other dropdowns
        ShowBarangayDropdown = false;
        ShowCityDropdown = false;
        ShowCountryDropdown = false;
        _currentHandler?.CloseDropdowns();
        
        ShowProvinceDropdown = !ShowProvinceDropdown;
        if (ShowProvinceDropdown)
        {
            ProvinceSearchText = "";
            LoadAllProvinces();
        }
        _stateHasChanged();
    }

    public void ToggleCountryDropdown()
    {
        if (_model.SameAsCurrentAddress) return;
        
        // Close other dropdowns
        ShowBarangayDropdown = false;
        ShowCityDropdown = false;
        ShowProvinceDropdown = false;
        _currentHandler?.CloseDropdowns();
        
        ShowCountryDropdown = !ShowCountryDropdown;
        if (ShowCountryDropdown)
        {
            CountrySearchText = "";
            LoadAllCountries();
        }
        _stateHasChanged();
    }

    private void LoadAllBarangays()
    {
        if (!string.IsNullOrWhiteSpace(_model.PermanentCity) && 
            !string.IsNullOrWhiteSpace(_model.PermanentProvince))
        {
            FilteredBarangays = _addressService.GetBarangaysByCity(
                _model.PermanentCity,
                _model.PermanentProvince
            );
        }
        else if (!string.IsNullOrWhiteSpace(_model.PermanentProvince))
        {
            var allBarangays = new List<string>();
            foreach (var city in _addressService.GetCitiesByProvince(_model.PermanentProvince))
            {
                allBarangays.AddRange(_addressService.GetBarangaysByCity(city, _model.PermanentProvince));
            }
            FilteredBarangays = allBarangays.Distinct().OrderBy(b => b).ToList();
        }
        else
        {
            var allBarangays = new List<string>();
            foreach (var province in _addressService.GetAllProvinces())
            {
                foreach (var city in _addressService.GetCitiesByProvince(province))
                {
                    allBarangays.AddRange(_addressService.GetBarangaysByCity(city, province));
                }
            }
            FilteredBarangays = allBarangays.Distinct().OrderBy(b => b).ToList();
        }
    }

    private void LoadAllCities()
    {
        if (!string.IsNullOrWhiteSpace(_model.PermanentProvince))
        {
            FilteredCities = _addressService.GetCitiesByProvince(_model.PermanentProvince);
        }
        else
        {
            var allCities = new List<string>();
            foreach (var province in _addressService.GetAllProvinces())
            {
                allCities.AddRange(_addressService.GetCitiesByProvince(province));
            }
            FilteredCities = allCities.Distinct().OrderBy(c => c).ToList();
        }
    }

    private void LoadAllProvinces()
    {
        FilteredProvinces = _addressService.GetAllProvinces().OrderBy(p => p).ToList();
    }

    private void LoadAllCountries()
    {
        FilteredCountries = _addressService.GetAllCountries();
    }

    public void FilterBarangays()
    {
        if (string.IsNullOrWhiteSpace(BarangaySearchText))
        {
            LoadAllBarangays();
            return;
        }
        var allBarangays = new List<string>();
        LoadAllBarangays();
        allBarangays = FilteredBarangays;
        FilteredBarangays = allBarangays
            .Where(b => b.ToLower().Contains(BarangaySearchText.ToLower()))
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
        var allCities = new List<string>();
        LoadAllCities();
        allCities = FilteredCities;
        FilteredCities = allCities
            .Where(c => c.ToLower().Contains(CitySearchText.ToLower()))
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
        LoadAllProvinces();
        FilteredProvinces = FilteredProvinces
            .Where(p => p.ToLower().Contains(ProvinceSearchText.ToLower()))
            .OrderBy(p => p)
            .ToList();
        _stateHasChanged();
    }

    public void FilterCountries()
    {
        FilteredCountries = _addressService.GetFilteredCountries(CountrySearchText);
        _stateHasChanged();
    }

    public void SelectBarangay(string barangay)
    {
        _model.PermanentBarangay = barangay;
        BarangaySearchText = "";
        ShowBarangayDropdown = false;
        // Auto-set country to Philippines when selecting Philippine address
        _model.PermanentCountry = "Philippines";
        // Auto-set ZIP code if city and province are available
        UpdateZipCode();
        _stateHasChanged();
    }

    public void SelectCity(string city)
    {
        _model.PermanentCity = city;
        CitySearchText = "";
        var province = _addressService.GetProvinceByCity(city);
        if (!string.IsNullOrEmpty(province))
        {
            _model.PermanentProvince = province;
        }
        ShowCityDropdown = false;
        // Auto-set country to Philippines when selecting Philippine address
        _model.PermanentCountry = "Philippines";
        // Auto-set ZIP code
        UpdateZipCode();
        // Reload barangays based on selected city
        if (ShowBarangayDropdown)
        {
            LoadAllBarangays();
        }
        _stateHasChanged();
    }

    public void SelectProvince(string province)
    {
        _model.PermanentProvince = province;
        ProvinceSearchText = "";
        _model.PermanentCity = ""; // Reset city when province changes
        // Keep barangay when province changes - don't reset it
        _model.PermanentZipCode = ""; // Reset ZIP code when province changes
        ShowProvinceDropdown = false;
        // Auto-set country to Philippines when selecting Philippine address
        _model.PermanentCountry = "Philippines";
        // Reload cities and barangays
        if (ShowCityDropdown)
        {
            LoadAllCities();
        }
        if (ShowBarangayDropdown)
        {
            LoadAllBarangays();
        }
        _stateHasChanged();
    }
    
    private void UpdateZipCode()
    {
        if (!string.IsNullOrWhiteSpace(_model.PermanentCity) && !string.IsNullOrWhiteSpace(_model.PermanentProvince))
        {
            var zipCode = _addressService.GetZipCodeByCity(_model.PermanentCity, _model.PermanentProvince);
            if (!string.IsNullOrEmpty(zipCode))
            {
                _model.PermanentZipCode = zipCode;
            }
            else
            {
                // Try to get ZIP code by province only
                zipCode = _addressService.GetZipCodeByProvince(_model.PermanentProvince);
                if (!string.IsNullOrEmpty(zipCode))
                {
                    _model.PermanentZipCode = zipCode;
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(_model.PermanentProvince))
        {
            var zipCode = _addressService.GetZipCodeByProvince(_model.PermanentProvince);
            if (!string.IsNullOrEmpty(zipCode))
            {
                _model.PermanentZipCode = zipCode;
            }
        }
    }

    public void SelectCountry(string country)
    {
        _model.PermanentCountry = country;
        CountrySearchText = "";
        ShowCountryDropdown = false;
        
        // If country is not Philippines, clear Philippine address fields
        if (country != "Philippines")
        {
            _model.PermanentProvince = "";
            _model.PermanentCity = "";
            _model.PermanentBarangay = "";
            _model.PermanentZipCode = "";
        }
        
        _stateHasChanged();
    }
    
    // Check if country dropdown should be disabled (when Philippine address is selected)
    public bool IsCountryDisabled()
    {
        return !string.IsNullOrWhiteSpace(_model.PermanentProvince) || 
               !string.IsNullOrWhiteSpace(_model.PermanentCity) || 
               !string.IsNullOrWhiteSpace(_model.PermanentBarangay);
    }

    public void CloseDropdowns()
    {
        if (ShowBarangayDropdown || ShowCityDropdown || ShowProvinceDropdown || ShowCountryDropdown)
        {
            ShowBarangayDropdown = false;
            ShowCityDropdown = false;
            ShowProvinceDropdown = false;
            ShowCountryDropdown = false;
            _stateHasChanged();
        }
    }
}

