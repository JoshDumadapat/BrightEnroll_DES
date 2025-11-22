// Tooltip delay handler - shows tooltips after 1 second delay
document.addEventListener('DOMContentLoaded', function() {
    let tooltipTimeout = null;
    
    // Function to position tooltip using fixed positioning
    function positionTooltip(group, tooltipText) {
        const rect = group.getBoundingClientRect();
        
        // Calculate position: bottom center of the element
        // Center the tooltip horizontally relative to the hovered element
        const left = rect.left + (rect.width / 2);
        const top = rect.bottom + 4; // 4px margin below the element
        
        tooltipText.style.left = left + 'px';
        tooltipText.style.top = top + 'px';
        tooltipText.style.transform = 'translateX(-50%)';
    }
    
    // Handle tooltip groups
    const tooltipGroups = document.querySelectorAll('.group');
    
    tooltipGroups.forEach(group => {
        const tooltipText = group.querySelector('.tooltip-delay');
        if (!tooltipText) return;
        
        // Mouse enter - start delay timer
        group.addEventListener('mouseenter', function() {
            tooltipTimeout = setTimeout(() => {
                tooltipText.classList.add('show');
                // Position tooltip after showing - use double RAF to ensure DOM is updated
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => {
                        positionTooltip(group, tooltipText);
                    });
                });
            }, 1000); // 1 second delay
        });
        
        // Mouse leave - hide immediately and clear timer
        group.addEventListener('mouseleave', function() {
            if (tooltipTimeout) {
                clearTimeout(tooltipTimeout);
                tooltipTimeout = null;
            }
            tooltipText.classList.remove('show');
        });
        
        // Reposition on scroll/resize
        window.addEventListener('scroll', () => {
            if (tooltipText.classList.contains('show')) {
                positionTooltip(group, tooltipText);
            }
        }, true);
        
        window.addEventListener('resize', () => {
            if (tooltipText.classList.contains('show')) {
                positionTooltip(group, tooltipText);
            }
        });
    });
    
    // Handle dynamically added tooltips (for Blazor)
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            mutation.addedNodes.forEach(function(node) {
                if (node.nodeType === 1) { // Element node
                    const newGroups = node.querySelectorAll ? node.querySelectorAll('.group') : [];
                    newGroups.forEach(group => {
                        const tooltipText = group.querySelector('.tooltip-delay');
                        if (tooltipText && !group.dataset.tooltipInitialized) {
                            group.dataset.tooltipInitialized = 'true';
                            let tooltipTimeout = null;
                            
                            // Function to position tooltip
                            function positionTooltip(group, tooltipText) {
                                const rect = group.getBoundingClientRect();
                                const left = rect.left + (rect.width / 2);
                                const top = rect.bottom + 4;
                                tooltipText.style.left = left + 'px';
                                tooltipText.style.top = top + 'px';
                                tooltipText.style.transform = 'translateX(-50%)';
                            }
                            
                            group.addEventListener('mouseenter', function() {
                                tooltipTimeout = setTimeout(() => {
                                    tooltipText.classList.add('show');
                                    requestAnimationFrame(() => {
                                        requestAnimationFrame(() => {
                                            positionTooltip(group, tooltipText);
                                        });
                                    });
                                }, 1000);
                            });
                            
                            group.addEventListener('mouseleave', function() {
                                if (tooltipTimeout) {
                                    clearTimeout(tooltipTimeout);
                                    tooltipTimeout = null;
                                }
                                tooltipText.classList.remove('show');
                            });
                            
                            // Reposition on scroll/resize
                            window.addEventListener('scroll', () => {
                                if (tooltipText.classList.contains('show')) {
                                    positionTooltip(group, tooltipText);
                                }
                            }, true);
                            
                            window.addEventListener('resize', () => {
                                if (tooltipText.classList.contains('show')) {
                                    positionTooltip(group, tooltipText);
                                }
                            });
                        }
                    });
                }
            });
        });
    });
    
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
});

