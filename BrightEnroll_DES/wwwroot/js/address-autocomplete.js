// Address Autocomplete JavaScript Helper
// Logic Path: wwwroot/js/address-autocomplete.js
// This file provides client-side autocomplete functionality for address fields

window.addressAutocomplete = {
    // Initialize autocomplete for an input field
    initialize: function (inputId, dropdownId, options, dotNetHelper) {
        const input = document.getElementById(inputId);
        const dropdown = document.getElementById(dropdownId);
        
        if (!input || !dropdown) return;

        // Store options and callback
        input._autocompleteOptions = options || [];
        input._autocompleteCallback = dotNetHelper;
        input._dropdownId = dropdownId;

        // Handle input events (fires on every keystroke)
        input.addEventListener('input', function (e) {
            const searchTerm = e.target.value.trim();
            
            if (searchTerm.length === 0) {
                dropdown.style.display = 'none';
                return;
            }

            // Filter options
            const filtered = input._autocompleteOptions.filter(option =>
                option.toLowerCase().includes(searchTerm.toLowerCase())
            ).slice(0, 10);

            // Update dropdown
            if (filtered.length > 0) {
                dropdown.innerHTML = '';
                filtered.forEach(option => {
                    const item = document.createElement('div');
                    item.className = 'px-4 py-2 hover:bg-blue-50 cursor-pointer text-sm';
                    item.textContent = option;
                    item.addEventListener('click', function () {
                        e.target.value = option;
                        e.target.dispatchEvent(new Event('input', { bubbles: true }));
                        e.target.dispatchEvent(new Event('change', { bubbles: true }));
                        dropdown.style.display = 'none';
                        if (dotNetHelper) {
                            dotNetHelper.invokeMethodAsync('OnOptionSelected', option);
                        }
                    });
                    dropdown.appendChild(item);
                });
                dropdown.style.display = 'block';
            } else {
                dropdown.style.display = 'none';
            }
        });

        // Hide dropdown when clicking outside
        document.addEventListener('click', function (e) {
            if (!input.contains(e.target) && !dropdown.contains(e.target)) {
                dropdown.style.display = 'none';
            }
        });

        // Show dropdown on focus if there's text
        input.addEventListener('focus', function (e) {
            if (e.target.value.trim().length > 0) {
                e.target.dispatchEvent(new Event('input'));
            }
        });
    },

    // Update options for an input field
    updateOptions: function (inputId, newOptions) {
        const input = document.getElementById(inputId);
        if (input && input._autocompleteOptions) {
            input._autocompleteOptions = newOptions;
        }
    },

    // Filter options based on input
    filterOptions: function (input, options) {
        if (!input || !options) return [];
        const searchTerm = input.toLowerCase();
        return options.filter(option => 
            option.toLowerCase().includes(searchTerm)
        ).slice(0, 10);
    },

    // Show dropdown
    showDropdown: function (elementId) {
        const dropdown = document.getElementById(elementId + '-dropdown');
        if (dropdown) {
            dropdown.style.display = 'block';
        }
    },

    // Hide dropdown
    hideDropdown: function (elementId) {
        const dropdown = document.getElementById(elementId + '-dropdown');
        if (dropdown) {
            dropdown.style.display = 'none';
        }
    },

    // Select option
    selectOption: function (inputId, value) {
        const input = document.getElementById(inputId);
        if (input) {
            input.value = value;
            input.dispatchEvent(new Event('input', { bubbles: true }));
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }
        this.hideDropdown(inputId);
    }
};
