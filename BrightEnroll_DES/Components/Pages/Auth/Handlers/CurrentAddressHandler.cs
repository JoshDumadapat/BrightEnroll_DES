using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Infrastructure;

namespace BrightEnroll_DES.Components.Pages.Auth.Handlers;

public class CurrentAddressHandler
{
    private readonly AddressService _addressService;
    private readonly StudentRegistrationModel _model;
    private readonly Action _stateHasChanged;
    private PermanentAddressHandler? _permanentHandler;

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

    public CurrentAddressHandler(AddressService addressService, StudentRegistrationModel model, Action stateHasChanged)
    {
        _addressService = addressService;
        _model = model;
        _stateHasChanged = stateHasChanged;
    }

    public void SetPermanentHandler(PermanentAddressHandler permanentHandler)
    {
        _permanentHandler = permanentHandler;
    }

    public void ToggleBarangayDropdown()
    {
        // Close other dropdowns first
        ShowCityDropdown = false;
        ShowProvinceDropdown = false;
        ShowCountryDropdown = false;
        _permanentHandler?.CloseDropdowns();
        
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
        // Close other dropdowns first
        ShowBarangayDropdown = false;
        ShowProvinceDropdown = false;
        ShowCountryDropdown = false;
        _permanentHandler?.CloseDropdowns();
        
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
        // Close other dropdowns first
        ShowBarangayDropdown = false;
        ShowCityDropdown = false;
        ShowCountryDropdown = false;
        _permanentHandler?.CloseDropdowns();
        
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
        // Close other dropdowns first
        ShowBarangayDropdown = false;
        ShowCityDropdown = false;
        ShowProvinceDropdown = false;
        _permanentHandler?.CloseDropdowns();
        
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
        if (!string.IsNullOrWhiteSpace(_model.CurrentCity) && 
            !string.IsNullOrWhiteSpace(_model.CurrentProvince))
        {
            FilteredBarangays = _addressService.GetBarangaysByCity(
                _model.CurrentCity,
                _model.CurrentProvince
            );
        }
        else if (!string.IsNullOrWhiteSpace(_model.CurrentProvince))
        {
            var allBarangays = new List<string>();
            foreach (var city in _addressService.GetCitiesByProvince(_model.CurrentProvince))
            {
                allBarangays.AddRange(_addressService.GetBarangaysByCity(city, _model.CurrentProvince));
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
        if (!string.IsNullOrWhiteSpace(_model.CurrentProvince))
        {
            FilteredCities = _addressService.GetCitiesByProvince(_model.CurrentProvince);
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
        _model.CurrentBarangay = barangay;
        BarangaySearchText = "";
        ShowBarangayDropdown = false;
        // Auto-set country to Philippines when selecting Philippine address
        _model.CurrentCountry = "Philippines";
        // Auto-set ZIP code if city and province are available
        UpdateZipCode();
        _stateHasChanged();
    }

    public void SelectCity(string city)
    {
        _model.CurrentCity = city;
        CitySearchText = "";
        var province = _addressService.GetProvinceByCity(city);
        if (!string.IsNullOrEmpty(province))
        {
            _model.CurrentProvince = province;
        }
        ShowCityDropdown = false;
        // Auto-set country to Philippines when selecting Philippine address
        _model.CurrentCountry = "Philippines";
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
        _model.CurrentProvince = province;
        ProvinceSearchText = "";
        _model.CurrentCity = ""; // Reset city when province changes
        // Keep barangay when province changes - don't reset it
        _model.CurrentZipCode = ""; // Reset ZIP code when province changes
        ShowProvinceDropdown = false;
        // Auto-set country to Philippines when selecting Philippine address
        _model.CurrentCountry = "Philippines";
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
        if (!string.IsNullOrWhiteSpace(_model.CurrentCity) && !string.IsNullOrWhiteSpace(_model.CurrentProvince))
        {
            var zipCode = _addressService.GetZipCodeByCity(_model.CurrentCity, _model.CurrentProvince);
            if (!string.IsNullOrEmpty(zipCode))
            {
                _model.CurrentZipCode = zipCode;
            }
            else
            {
                // Try to get ZIP code by province only
                zipCode = _addressService.GetZipCodeByProvince(_model.CurrentProvince);
                if (!string.IsNullOrEmpty(zipCode))
                {
                    _model.CurrentZipCode = zipCode;
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(_model.CurrentProvince))
        {
            var zipCode = _addressService.GetZipCodeByProvince(_model.CurrentProvince);
            if (!string.IsNullOrEmpty(zipCode))
            {
                _model.CurrentZipCode = zipCode;
            }
        }
    }

    public void SelectCountry(string country)
    {
        _model.CurrentCountry = country;
        CountrySearchText = "";
        ShowCountryDropdown = false;
        
        // If country is not Philippines, clear Philippine address fields
        if (country != "Philippines")
        {
            _model.CurrentProvince = "";
            _model.CurrentCity = "";
            _model.CurrentBarangay = "";
            _model.CurrentZipCode = "";
        }
        
        _stateHasChanged();
    }
    
    // Check if country dropdown should be disabled (when Philippine address is selected)
    public bool IsCountryDisabled()
    {
        return !string.IsNullOrWhiteSpace(_model.CurrentProvince) || 
               !string.IsNullOrWhiteSpace(_model.CurrentCity) || 
               !string.IsNullOrWhiteSpace(_model.CurrentBarangay);
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

