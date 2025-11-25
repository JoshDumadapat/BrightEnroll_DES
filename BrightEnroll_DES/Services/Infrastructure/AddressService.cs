namespace BrightEnroll_DES.Services.Infrastructure;

// Service that provides Philippine address data and autocomplete functionality for forms
public class AddressService
{
    // Philippine Address Data Structure
    private readonly Dictionary<string, Dictionary<string, List<string>>> _philippineAddresses;
    
    // ZIP Code mapping: Province -> City -> ZIP Code
    private readonly Dictionary<string, Dictionary<string, string>> _zipCodes;

    public AddressService()
    {
        _philippineAddresses = InitializePhilippineAddresses();
        _zipCodes = InitializeZipCodes();
    }

    // Returns a list of barangays that match the search term, optionally filtered by city and province
    public List<string> GetFilteredBarangays(string searchTerm, string? city = null, string? province = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<string>();

        var results = new List<string>();
        searchTerm = searchTerm.ToLower();

        if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(province))
        {
            // Filter by specific city and province
            if (_philippineAddresses.ContainsKey(province) &&
                _philippineAddresses[province].ContainsKey(city))
            {
                results = _philippineAddresses[province][city]
                    .Where(b => b.ToLower().Contains(searchTerm))
                    .ToList();
            }
        }
        else if (!string.IsNullOrEmpty(province))
        {
            // Filter by province only
            if (_philippineAddresses.ContainsKey(province))
            {
                foreach (var cityData in _philippineAddresses[province])
                {
                    results.AddRange(cityData.Value
                        .Where(b => b.ToLower().Contains(searchTerm)));
                }
            }
        }
        else
        {
            // Search all barangays
            foreach (var provinceData in _philippineAddresses)
            {
                foreach (var cityData in provinceData.Value)
                {
                    results.AddRange(cityData.Value
                        .Where(b => b.ToLower().Contains(searchTerm)));
                }
            }
        }

        return results.Distinct().OrderBy(b => b).Take(10).ToList();
    }

    // Returns a list of cities that match the search term, optionally filtered by province
    public List<string> GetFilteredCities(string searchTerm, string? province = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<string>();

        var results = new List<string>();
        searchTerm = searchTerm.ToLower();

        if (!string.IsNullOrEmpty(province))
        {
            if (_philippineAddresses.ContainsKey(province))
            {
                results = _philippineAddresses[province].Keys
                    .Where(c => c.ToLower().Contains(searchTerm))
                    .ToList();
            }
        }
        else
        {
            foreach (var provinceData in _philippineAddresses)
            {
                results.AddRange(provinceData.Value.Keys
                    .Where(c => c.ToLower().Contains(searchTerm)));
            }
        }

        return results.Distinct().OrderBy(c => c).Take(10).ToList();
    }

    // Finds and returns the province that contains the specified city
    public string? GetProvinceByCity(string city)
    {
        foreach (var provinceData in _philippineAddresses)
        {
            if (provinceData.Value.ContainsKey(city))
            {
                return provinceData.Key;
            }
        }
        return null;
    }

    // Returns all cities within the specified province
    public List<string> GetCitiesByProvince(string province)
    {
        if (_philippineAddresses.ContainsKey(province))
        {
            return _philippineAddresses[province].Keys.OrderBy(c => c).ToList();
        }
        return new List<string>();
    }

    // Returns all barangays within the specified city and province
    public List<string> GetBarangaysByCity(string city, string province)
    {
        if (_philippineAddresses.ContainsKey(province) &&
            _philippineAddresses[province].ContainsKey(city))
        {
            return _philippineAddresses[province][city].OrderBy(b => b).ToList();
        }
        return new List<string>();
    }

    // Returns all available provinces in the Philippines
    public List<string> GetAllProvinces()
    {
        return _philippineAddresses.Keys.OrderBy(p => p).ToList();
    }

    // Returns a limited list of countries (Philippines first, then other common countries)
    public List<string> GetAllCountries()
    {
        return new List<string>
        {
            "Philippines", "United States", "Canada", "United Kingdom", "Australia",
            "Japan", "South Korea", "China", "Singapore", "Malaysia", "Thailand",
            "Indonesia", "India", "Saudi Arabia", "United Arab Emirates", "Other"
        }.OrderBy(c => c == "Philippines" ? "" : c).ToList(); // Philippines first, then alphabetical
    }

    // Returns countries that match the search term from the limited country list
    public List<string> GetFilteredCountries(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return GetAllCountries();
        }

        return GetAllCountries()
            .Where(c => c.ToLower().Contains(searchTerm.ToLower()))
            .OrderBy(c => c)
            .ToList();
    }

    // Returns the ZIP code for a specific city and province combination
    public string? GetZipCodeByCity(string city, string province)
    {
        if (_zipCodes.ContainsKey(province) && _zipCodes[province].ContainsKey(city))
        {
            return _zipCodes[province][city];
        }
        return null;
    }

    // Returns a default ZIP code for the province (usually the main city's ZIP code)
    public string? GetZipCodeByProvince(string province)
    {
        if (_zipCodes.ContainsKey(province))
        {
            // Return the first ZIP code found for the province (usually the main city)
            var firstCity = _zipCodes[province].FirstOrDefault();
            return firstCity.Value;
        }
        return null;
    }

    // Sets up the ZIP code mapping for major Philippine cities and provinces
    private Dictionary<string, Dictionary<string, string>> InitializeZipCodes()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            {
                "Metro Manila",
                new Dictionary<string, string>
                {
                    { "Manila", "1000" },
                    { "Quezon City", "1100" },
                    { "Makati", "1200" },
                    { "Pasig", "1600" },
                    { "Mandaluyong", "1550" },
                    { "San Juan", "1500" },
                    { "Taguig", "1630" },
                    { "Pasay", "1300" },
                    { "Parañaque", "1700" },
                    { "Las Piñas", "1740" },
                    { "Muntinlupa", "1770" },
                    { "Marikina", "1800" },
                    { "Caloocan", "1400" },
                    { "Malabon", "1470" },
                    { "Navotas", "1485" },
                    { "Valenzuela", "1440" }
                }
            },
            {
                "Davao del Sur",
                new Dictionary<string, string>
                {
                    { "Davao City", "8000" },
                    { "Digos", "8002" },
                    { "Santa Cruz", "8001" }
                }
            },
            {
                "Cebu",
                new Dictionary<string, string>
                {
                    { "Cebu City", "6000" },
                    { "Lapu-Lapu", "6015" },
                    { "Mandaue", "6014" },
                    { "Talisay", "6045" }
                }
            },
            {
                "Laguna",
                new Dictionary<string, string>
                {
                    { "Calamba", "4027" },
                    { "Los Baños", "4030" },
                    { "San Pedro", "4023" },
                    { "Santa Rosa", "4026" },
                    { "Biñan", "4024" }
                }
            },
            {
                "Cavite",
                new Dictionary<string, string>
                {
                    { "Bacoor", "4102" },
                    { "Dasmariñas", "4114" },
                    { "Imus", "4103" },
                    { "General Trias", "4107" }
                }
            },
            {
                "Bulacan",
                new Dictionary<string, string>
                {
                    { "Malolos", "3000" },
                    { "Meycauayan", "3020" },
                    { "San Jose del Monte", "3023" }
                }
            },
            {
                "Pampanga",
                new Dictionary<string, string>
                {
                    { "Angeles", "2009" },
                    { "San Fernando", "2000" },
                    { "Mabalacat", "2010" }
                }
            },
            {
                "Rizal",
                new Dictionary<string, string>
                {
                    { "Antipolo", "1870" },
                    { "Cainta", "1900" },
                    { "Taytay", "1920" }
                }
            },
            {
                "Batangas",
                new Dictionary<string, string>
                {
                    { "Batangas City", "4200" },
                    { "Lipa", "4217" },
                    { "Tanauan", "4232" }
                }
            },
            {
                "Iloilo",
                new Dictionary<string, string>
                {
                    { "Iloilo City", "5000" },
                    { "Pavia", "5001" }
                }
            },
            {
                "Negros Occidental",
                new Dictionary<string, string>
                {
                    { "Bacolod", "6100" },
                    { "Talisay", "6115" }
                }
            },
            {
                "Misamis Oriental",
                new Dictionary<string, string>
                {
                    { "Cagayan de Oro", "9000" },
                    { "El Salvador", "9017" }
                }
            },
            {
                "Zamboanga del Sur",
                new Dictionary<string, string>
                {
                    { "Pagadian", "7016" },
                    { "Zamboanga City", "7000" }
                }
            }
        };
    }

    // Sets up the complete Philippine address database with provinces, cities, and barangays for all regions
    private Dictionary<string, Dictionary<string, List<string>>> InitializePhilippineAddresses()
    {
        return new Dictionary<string, Dictionary<string, List<string>>>
        {
            // ========== LUZON REGIONS ==========
            {
                "Metro Manila",
                new Dictionary<string, List<string>>
                {
                    {
                        "Manila",
                        new List<string>
                        {
                            "Binondo", "Ermita", "Intramuros", "Malate", "Paco", "Pandacan", "Quiapo",
                            "Sampaloc", "San Andres", "San Miguel", "San Nicolas", "Santa Ana", "Santa Cruz",
                            "Santa Mesa", "Tondo", "Port Area"
                        }
                    },
                    {
                        "Quezon City",
                        new List<string>
                        {
                            "Bagong Silangan", "Commonwealth", "Cubao", "Diliman", "Kamuning", "Katipunan",
                            "Loyola Heights", "New Manila", "Project 2", "Project 3", "Project 4", "Project 6",
                            "Project 7", "Project 8", "Roxas District", "San Francisco del Monte", "Tandang Sora",
                            "UP Village", "West Triangle"
                        }
                    },
                    {
                        "Makati",
                        new List<string>
                        {
                            "Bel-Air", "Cembo", "Comembo", "Dasmariñas Village", "Forbes Park", "Guadalupe Nuevo",
                            "Guadalupe Viejo", "Kasilawan", "Magallanes", "Pembo", "Pio del Pilar", "Pitogo",
                            "Poblacion", "Rizal", "San Antonio", "San Isidro", "San Lorenzo", "Singkamas",
                            "South Cembo", "Tejeros", "Urdaneta", "Valenzuela", "West Rembo"
                        }
                    },
                    {
                        "Pasig",
                        new List<string>
                        {
                            "Bagong Ilog", "Bagong Katipunan", "Bambang", "Buting", "Caniogan", "Capitolio",
                            "Dela Paz", "Kapitolyo", "Manggahan", "Maybunga", "Ortigas", "Palatiw", "Pinagbuhatan",
                            "Pineda", "Rosario", "Sagad", "San Antonio", "San Joaquin", "San Jose", "San Miguel",
                            "San Nicolas", "Santa Cruz", "Santa Lucia", "Santa Rosa", "Santo Tomas", "Santolan",
                            "Sumilang", "Ugong"
                        }
                    }
                }
            },
            {
                "Cavite",
                new Dictionary<string, List<string>>
                {
                    {
                        "Bacoor",
                        new List<string>
                        {
                            "Alima", "Aniban", "Banalo", "Bayanan", "Camposanto", "Daang Bukid", "Daang Hari",
                            "Digman", "Dulong Bayan", "Habay", "Kaingin", "Ligas", "Longos", "Maliksi", "Mambog",
                            "Molino", "Niog", "Panapaan", "Poblacion", "Queens Row", "Real", "Salinas", "San Nicolas",
                            "Sineguelasan", "Tabing Dagat", "Talaba", "Zapote"
                        }
                    },
                    {
                        "Dasmariñas",
                        new List<string>
                        {
                            "Burol", "Datu Esmael", "Emmanuel Bergado", "Fatima", "H-2", "Langkaan", "Luzviminda",
                            "Paliparan", "Poblacion", "Salawag", "San Agustin", "San Andres", "San Antonio",
                            "San Jose", "San Lorenzo", "San Luis", "San Manuel", "San Miguel", "San Nicolas",
                            "San Pedro", "Santo Cristo", "Santo Niño", "Victoria Reyes", "Zone 1", "Zone 2", "Zone 3"
                        }
                    },
                    {
                        "Imus",
                        new List<string>
                        {
                            "Alapan", "Anabu", "Bayan Luma", "Bucandala", "Buhay na Tubig", "Carsadang Bago",
                            "Malagasang", "Mariano Espeleta", "Medicion", "Palico", "Poblacion", "Toclong",
                            "Tanzang Luma"
                        }
                    }
                }
            },
            {
                "Laguna",
                new Dictionary<string, List<string>>
                {
                    {
                        "Calamba",
                        new List<string>
                        {
                            "Bagong Kalsada", "Banlic", "Barandal", "Bucal", "Bunggo", "Burol", "Camaligan",
                            "Canlubang", "Halang", "Hornalan", "Kay-Anlog", "La Mesa", "Laguerta", "Lawa",
                            "Lecheria", "Lingga", "Looc", "Mabato", "Majada Labas", "Makiling", "Mapagong",
                            "Masili", "Maunong", "Mayapa", "Milagrosa", "Paciano Rizal", "Palingon", "Palo-alto",
                            "Pansol", "Parian", "Prinza", "Punta", "Puting Lupa", "Real", "Saimsim", "Sampiruhan",
                            "San Cristobal", "San Jose", "San Juan", "Sirang Lupa", "Sucol", "Turbina", "Ulango",
                            "Uwisan"
                        }
                    },
                    {
                        "Santa Rosa",
                        new List<string>
                        {
                            "Aplaya", "Balibago", "Caingin", "Dila", "Dita", "Don Jose", "Ibaba", "Labas",
                            "Macabling", "Malitlit", "Malusak", "Market Area", "Poblacion", "Pulong Santa Cruz",
                            "Santo Domingo", "Sinalhan", "Tagapo"
                        }
                    },
                    {
                        "San Pedro",
                        new List<string>
                        {
                            "Bagong Silang", "Calendola", "Chrysanthemum", "Cuyab", "Estrella", "Landayan",
                            "Langgam", "Laram", "Magsaysay", "Narra", "Nueva", "Pacita", "Poblacion", "Rosario",
                            "Sampaguita", "San Antonio", "San Roque", "San Vicente", "Santo Niño", "United Bayanihan",
                            "United Better Living"
                        }
                    }
                }
            },
            {
                "Bulacan",
                new Dictionary<string, List<string>>
                {
                    {
                        "Malolos",
                        new List<string>
                        {
                            "Anilao", "Atlag", "Babatnin", "Bagna", "Bagong Bayan", "Balayong", "Balite",
                            "Bangkal", "Barihan", "Bulihan", "Bungahan", "Caingin", "Calero", "Caliligawan",
                            "Canalate", "Canino", "Catmon", "Cofradia", "Dakila", "Guinhawa", "Ligas", "Mabolo",
                            "Mambog", "Masile", "Matimbo", "Mojon", "Namayan", "Niugan", "Pamarawan", "Panasahan",
                            "Pinagbakawan", "San Agustin", "San Gabriel", "San Juan", "San Pablo", "San Vicente",
                            "Santiago", "Santisima Trinidad", "Santo Cristo", "Santo Niño", "Santo Rosario",
                            "Sikatuna", "Sumapang Matanda", "Sumapang Bata", "Taal", "Tikay"
                        }
                    },
                    {
                        "Meycauayan",
                        new List<string>
                        {
                            "Bagong Barrio", "Bahay Pare", "Bancal", "Bangkal", "Bayan", "Caingin", "Calvario",
                            "Camalig", "Gasak", "Hulo", "Iba", "Langka", "Lawang", "Libtong", "Liputan", "Longos",
                            "Malhacan", "Pajo", "Pandayan", "Pantoc", "Perez", "Poblacion", "Saluysoy", "Tugatog",
                            "Ubihan", "Wakas", "Zamora"
                        }
                    }
                }
            },
            // ========== VISAYAS REGIONS ==========
            {
                "Cebu",
                new Dictionary<string, List<string>>
                {
                    {
                        "Cebu City",
                        new List<string>
                        {
                            "Adlaon", "Agsungot", "Apas", "Babag", "Bacayan", "Banilad", "Basak Pardo",
                            "Basak San Nicolas", "Binaliw", "Bonbon", "Budlaan", "Buhisan", "Bulacao Pardo",
                            "Busay", "Calamba", "Cambinocot", "Capitol Site", "Carreta", "Central", "Cogon Pardo",
                            "Cogon Ramos", "Day-as", "Duljo Fatima", "Ermita", "Guadalupe", "Guba", "Hipodromo",
                            "Inayawan", "Kalubihan", "Kalunasan", "Kamagayan", "Kamputhaw", "Kasambagan",
                            "Kinasang-an Pardo", "Labangon", "Lahug", "Lorega San Miguel", "Lusaran", "Luz",
                            "Mabini", "Mabolo", "Malubog", "Pahina Central", "Pahina San Nicolas", "Pamutan",
                            "Pari-an", "Parian", "Pasil", "Pit-os", "Poblacion Pardo", "Pulangbato", "Pung-ol Sibugay",
                            "Punta Princesa", "Quiot Pardo", "Sambag", "Sambag", "San Antonio", "San Jose",
                            "San Nicolas Proper", "San Roque", "Santa Cruz", "Santo Niño", "Sapangdaku", "Sawang Calero",
                            "Sinsin", "Sirao", "Subangdaku", "Sudlon", "T. Padilla", "Tabunan", "Tagba-o",
                            "Talamban", "Taptap", "Tejero", "Tinago", "Tisa", "To-ong Pardo", "Zapatera"
                        }
                    },
                    {
                        "Lapu-Lapu City",
                        new List<string>
                        {
                            "Agus", "Babag", "Bankal", "Baring", "Basak", "Buaya", "Calawisan", "Canjulao",
                            "Gun-ob", "Ibo", "Looc", "Mactan", "Maribago", "Marigondon", "Pajac", "Pajo",
                            "Poblacion", "Punta Engaño", "Pusok", "Sabang", "Santa Rosa", "Subabasbas",
                            "Tayud", "Tungasan"
                        }
                    },
                    {
                        "Mandaue",
                        new List<string>
                        {
                            "Alang-alang", "Bakilid", "Banilad", "Basak", "Cabancalan", "Cambaro", "Canduman",
                            "Casili", "Casuntingan", "Centro", "Cubacub", "Guizo", "Ibabao-Estancia", "Jagobiao",
                            "Labogon", "Looc", "Maguikay", "Mantuyong", "Opao", "Paknaan", "Pagsabungan",
                            "Subangdaku", "Tabok", "Tawason", "Tingub", "Tipolo", "Umapad"
                        }
                    }
                }
            },
            {
                "Iloilo",
                new Dictionary<string, List<string>>
                {
                    {
                        "Iloilo City",
                        new List<string>
                        {
                            "Aganan", "Alalasan", "Alangilan", "Alimodian", "Anilao", "Araneta", "Arroyo",
                            "Bacolod", "Balabago", "Balantang", "Baldoza", "Baluarte", "Banuyao", "Bantud",
                            "Bantud Fabrica", "Barangay 1", "Barangay 2", "Barangay 3", "Barangay 4",
                            "Barangay 5", "Barangay 6", "Barangay 7", "Barangay 8", "Barangay 9", "Barangay 10",
                            "Barangay 11", "Barangay 12", "Barangay 13", "Barangay 14", "Barangay 15",
                            "Barangay 16", "Barangay 17", "Barangay 18", "Barangay 19", "Barangay 20",
                            "Barangay 21", "Barangay 22", "Barangay 23", "Barangay 24", "Barangay 25",
                            "Barangay 26", "Barangay 27", "Barangay 28", "Barangay 29", "Barangay 30",
                            "Barangay 31", "Barangay 32", "Barangay 33", "Barangay 34", "Barangay 35",
                            "Barangay 36", "Barangay 37", "Barangay 38", "Barangay 39", "Barangay 40",
                            "Barangay 41", "Barangay 42", "Barangay 43", "Barangay 44", "Barangay 45",
                            "Barangay 46", "Barangay 47", "Barangay 48", "Barangay 49", "Barangay 50",
                            "Barangay 51", "Barangay 52", "Barangay 53", "Barangay 54", "Barangay 55",
                            "Barangay 56", "Barangay 57", "Barangay 58", "Barangay 59", "Barangay 60",
                            "Barangay 61", "Barangay 62", "Barangay 63", "Barangay 64", "Barangay 65",
                            "Barangay 66", "Barangay 67", "Barangay 68", "Barangay 69", "Barangay 70",
                            "Barangay 71", "Barangay 72", "Barangay 73", "Barangay 74", "Barangay 75",
                            "Barangay 76", "Barangay 77", "Barangay 78", "Barangay 79", "Barangay 80",
                            "Barangay 81", "Barangay 82", "Barangay 83", "Barangay 84", "Barangay 85",
                            "Barangay 86", "Barangay 87", "Barangay 88", "Barangay 89", "Barangay 90",
                            "Barangay 91", "Barangay 92", "Barangay 93", "Barangay 94", "Barangay 95",
                            "Barangay 96", "Barangay 97", "Barangay 98", "Barangay 99", "Barangay 100",
                            "Barangay 101", "Barangay 102", "Barangay 103", "Barangay 104", "Barangay 105",
                            "Barangay 106", "Barangay 107", "Barangay 108", "Barangay 109", "Barangay 110",
                            "Barangay 111", "Barangay 112", "Barangay 113", "Barangay 114", "Barangay 115",
                            "Barangay 116", "Barangay 117", "Barangay 118", "Barangay 119", "Barangay 120",
                            "Barangay 121", "Barangay 122", "Barangay 123", "Barangay 124", "Barangay 125",
                            "Barangay 126", "Barangay 127", "Barangay 128", "Barangay 129", "Barangay 130",
                            "Barangay 131", "Barangay 132", "Barangay 133", "Barangay 134", "Barangay 135",
                            "Barangay 136", "Barangay 137", "Barangay 138", "Barangay 139", "Barangay 140",
                            "Barangay 141", "Barangay 142", "Barangay 143", "Barangay 144", "Barangay 145",
                            "Barangay 146", "Barangay 147", "Barangay 148", "Barangay 149", "Barangay 150",
                            "Barangay 151", "Barangay 152", "Barangay 153", "Barangay 154", "Barangay 155",
                            "Barangay 156", "Barangay 157", "Barangay 158", "Barangay 159", "Barangay 160",
                            "Barangay 161", "Barangay 162", "Barangay 163", "Barangay 164", "Barangay 165",
                            "Barangay 166", "Barangay 167", "Barangay 168", "Barangay 169", "Barangay 170",
                            "Barangay 171", "Barangay 172", "Barangay 173", "Barangay 174", "Barangay 175",
                            "Barangay 176", "Barangay 177", "Barangay 178", "Barangay 179", "Barangay 180"
                        }
                    }
                }
            },
            {
                "Negros Occidental",
                new Dictionary<string, List<string>>
                {
                    {
                        "Bacolod",
                        new List<string>
                        {
                            "Alangilan", "Alijis", "Banago", "Bata", "Cabug", "Estefania", "Felisa",
                            "Granada", "Handumanan", "Lopez Jaena", "Mabini", "Mandalagan", "Mansilingan",
                            "Montevista", "Pahanocoy", "Punta Taytay", "Singcang-Airport", "Sum-ag", "Taculing",
                            "Tangub", "Villamonte", "Vista Alegre"
                        }
                    }
                }
            },
            // ========== MINDANAO REGIONS (Davao) ==========
{
    "Davao del Sur",
    new Dictionary<string, List<string>>
    {
        {
            "Davao City",
            new List<string>
            {
                "Acacia", "Agdao", "Alambre", "Alejandra Navarro (Lasang)", "Alfonso Angliongto Sr.",
                "Angalan", "Atan-Awe", "Baganihan", "Bago Aplaya", "Bago Gallera", "Bago Oshiro",
                "Baguio", "Balengaeng", "Baliok", "Bangkas Heights", "Bantol", "Baracatan",
                "Barangay 1-A", "Barangay 2-A", "Barangay 3-A", "Barangay 4-A", "Barangay 5-A",
                "Barangay 6-A", "Barangay 7-A", "Barangay 8-A", "Barangay 9-A", "Barangay 10-A",
                "Barangay 11-B", "Barangay 12-B", "Barangay 13-B", "Barangay 14-B", "Barangay 15-B",
                "Barangay 16-B", "Barangay 17-B", "Barangay 18-B", "Barangay 19-B", "Barangay 20-B",
                "Barangay 21-C", "Barangay 22-C", "Barangay 23-C", "Barangay 24-C", "Barangay 25-C",
                "Barangay 26-C", "Barangay 27-C", "Barangay 28-C", "Barangay 29-C", "Barangay 30-C",
                "Barangay 31-D", "Barangay 32-D", "Barangay 33-D", "Barangay 34-D", "Barangay 35-D",
                "Barangay 36-D", "Barangay 37-D", "Barangay 38-D", "Barangay 39-D", "Barangay 40-D",
                "Bato", "Bayabas", "Biao Escuela", "Biao Guianga", "Biao Joaquin", "Binugao",
                "Bucana", "Buda", "Buhangin", "Bunawan", "Cabantian", "Cadalian", "Calinan",
                "Callawa", "Camansi", "Carmen", "Catalunan Grande", "Catalunan Pequeño", "Catigan",
                "Cawayan", "Centro (San Juan)", "Colosas", "Communal", "Crossing Bayabas", "Dacudao",
                "Dalag", "Dalagdag", "Daliao", "Daliaon Plantation", "Datu Salumay", "Dominga",
                "Dumoy", "Eden", "Fatima (Benowang)", "Gatungan", "Gov. Paciano Bangoy",
                "Gov. Vicente Duterte", "Gumalang", "Gumitan", "Ilang", "Inayangan", "Indangan",
                "Kap. Tomas Monteverde Sr.", "Kilate", "Lacson", "Lamanan", "Lampianao", "Langub",
                "Lapu-lapu", "Leon Garcia Sr.", "Lizada", "Los Amigos", "Lubogan", "Lumiad", "Ma-a",
                "Mabuhay", "Magsaysay", "Magtuod", "Mahayag", "Malabog", "Malagos", "Malamba",
                "Manambulan", "Mandug", "Manuel Guianga", "Mapula", "Marapangi", "Marilog",
                "Matina Aplaya", "Matina Biao", "Matina Crossing", "Matina Pangi", "Megkawayan",
                "Mintal", "Mudiang", "Mulig", "New Carmen", "New Valencia", "Pampanga", "Panacan",
                "Panalum", "Pandaitan", "Pangyan", "Paquibato", "Paradise Embak", "Rafael Castillo",
                "Riverside", "Salapawan", "Salaysay", "Saloy", "San Antonio", "San Isidro (Licanan)",
                "Santo Niño", "Sasa", "Sibulan", "Sirawan", "Sirib", "Suawan (Tuli)", "Subasta",
                "Sumimao", "Tacunan", "Tagakpan", "Tagluno", "Tagurano", "Talandang", "Talomo",
                "Talomo River", "Tamayong", "Tambobong", "Tamugan", "Tapak", "Tawan-Tawan",
                "Tibuloy", "Tibungco", "Tigatto", "Toril", "Tugbok", "Tungakalan", "Ubalde", "Ula",
                "Vicente Hizon Sr.", "Waan", "Wangan", "Wilfredo Aquino", "Wines"
            }
        },
        {
            "Digos",
            new List<string>
            {
                "Aplaya", "Binaton", "Cogon", "Dawis", "Dulangan", "Goma", "Kapatagan", "Kiagot",
                "Leling", "Matti", "Ruparan", "San Agustin", "San Jose", "San Miguel", "Sinawilan",
                "Soong", "Tiguman", "Tres de Mayo"
            }
        },
        {
            "Bansalan",
            new List<string>
            {
                "Bonifacio", "Eman", "Kinuskusan", "Liberty", "Linawan", "Malinawon", "Managa",
                "Marber", "New Clarin", "New Lourdes", "New Visayas", "Poblacion", "Rizal", "Union",
                "Upper Dado", "Upper Bala", "Upper Kibangay", "Upper New Cebu"
            }
        }
    }
},
{
    "Misamis Oriental",
    new Dictionary<string, List<string>>
    {
        {
            "Cagayan de Oro",
            new List<string>
            {
                "Agusan", "Balulang", "Bayabas", "Bugo", "Bulua", "Camaman-an", "Canitoan",
                "Carmen", "Consolacion", "Gusa", "Iponan", "Kauswagan", "Lapasan", "Lumbia",
                "Macabalan", "Macasandig", "Nazareth", "Patag", "Puntod", "Tignapoloan"
            }
        },
        {
            "El Salvador",
            new List<string>
            {
                "Amoros", "Bolisong", "Himaya", "Hinigdaan", "Kibonbon", "Molugan", "Poblacion",
                "Sambulawan", "Sinaloc", "Taytay"
            }
        }
    }
},
{
    "Zamboanga del Sur",
    new Dictionary<string, List<string>>
    {
        {
            "Pagadian",
            new List<string>
            {
                "Balangasan", "Baliwasan", "Banale", "Bomba", "Buenavista", "Danlugan",
                "Ditoray", "Gatas", "Kawit", "Lumbia", "Muricay", "Pedulonan", "San Jose",
                "Santiago", "Santa Lucia", "Santo Niño", "Tiguma"
            }
        },
        {
            "Zamboanga City",
            new List<string>
            {
                "Ayala", "Boalan", "Calarian", "Culianan", "Divisoria", "Guiwan", "Lunzuran",
                "Manicahan", "Pasonanca", "Putik", "Recodo", "San Jose Gusu", "San Roque",
                "Sta. Catalina", "Sta. Maria", "Tetuan", "Tugbungan", "Tumaga", "Zambowood"
            }
        }
    }
}
};
    }
}
