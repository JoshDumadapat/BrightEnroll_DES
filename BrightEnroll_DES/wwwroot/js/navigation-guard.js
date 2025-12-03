// Navigation guard to intercept navigation attempts
window.setupNavigationGuard = function (dotNetRef, currentPath) {
    // Intercept all anchor clicks
    document.addEventListener('click', function(event) {
        const target = event.target.closest('a');
        if (!target) return;
        
        const href = target.getAttribute('href');
        if (!href) return;
        
        // Skip if it's the same page or external link
        if (href.startsWith('http') || href.startsWith('mailto:') || href.startsWith('tel:')) {
            return;
        }
        
        // Skip if it's the current page
        if (href === currentPath || href === window.location.pathname) {
            return;
        }
        
        // Prevent default navigation first
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        
        // Check if form has data before allowing navigation
        dotNetRef.invokeMethodAsync('CheckBeforeNavigate', href).then(function(shouldPrevent) {
            if (!shouldPrevent) {
                // Allow navigation - navigate manually
                window.location.href = href;
            }
            // If shouldPrevent is true, modal is shown and navigation is blocked
        }).catch(function(error) {
            console.error('Error checking navigation:', error);
            // On error, allow navigation
            window.location.href = href;
        });
    }, true); // Use capture phase to intercept early
};

