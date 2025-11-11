// Dropdown click outside handler
window.setupClickOutsideHandler = function (dotNetRef) {
    document.addEventListener('click', function (event) {
        // Use setTimeout to allow Blazor to render the dropdown first
        setTimeout(function() {
            // Check if click is outside all dropdowns
            const clickedElement = event.target;
            const dropdowns = document.querySelectorAll('[data-dropdown]');
            let clickedInsideDropdown = false;
            
            // Check if clicked element or any parent has data-dropdown attribute
            let currentElement = clickedElement;
            while (currentElement && currentElement !== document.body) {
                if (currentElement.hasAttribute && currentElement.hasAttribute('data-dropdown')) {
                    clickedInsideDropdown = true;
                    break;
                }
                currentElement = currentElement.parentElement;
            }
            
            // Also check all dropdown containers
            dropdowns.forEach(function (dropdown) {
                if (dropdown.contains(clickedElement)) {
                    clickedInsideDropdown = true;
                }
            });
            
            if (!clickedInsideDropdown) {
                // Notify Blazor to close dropdowns
                dotNetRef.invokeMethodAsync('CloseDropdownsFromJS');
            }
        }, 50); // Small delay to allow dropdown to render
    });
};

