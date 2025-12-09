// Simple tooltip handler - shows tooltips below cursor
document.addEventListener('DOMContentLoaded', function() {
    // Function to position tooltip below cursor
    function positionTooltip(event, tooltipText) {
        const spacing = 10; // Distance below cursor
        const margin = 8;
        
        // Get tooltip dimensions
        tooltipText.style.visibility = 'hidden';
        tooltipText.style.display = 'block';
        const tooltipRect = tooltipText.getBoundingClientRect();
        const tooltipWidth = tooltipRect.width;
        const tooltipHeight = tooltipRect.height;
        tooltipText.style.visibility = 'visible';
        
        // Position below cursor, centered horizontally
        let left = event.clientX;
        let top = event.clientY + spacing;
        
        // Adjust if tooltip goes off screen edges
        const viewportWidth = window.innerWidth;
        const viewportHeight = window.innerHeight;
        
        // Horizontal adjustment
        if (left + (tooltipWidth / 2) > viewportWidth - margin) {
            left = viewportWidth - margin - (tooltipWidth / 2);
        }
        if (left - (tooltipWidth / 2) < margin) {
            left = margin + (tooltipWidth / 2);
        }
        
        // Vertical adjustment - show above if no room below
        if (top + tooltipHeight > viewportHeight - margin) {
            top = event.clientY - tooltipHeight - spacing;
        }
        
        tooltipText.style.left = left + 'px';
        tooltipText.style.top = top + 'px';
        tooltipText.style.transform = 'translateX(-50%)';
    }
    
    // Initialize tooltips for existing elements
    function initTooltip(group) {
        const tooltipText = group.querySelector('.tooltip-delay');
        if (!tooltipText || group.dataset.tooltipInitialized) return;
        
        group.dataset.tooltipInitialized = 'true';
        
        let tooltipTimeout = null;
        let isTooltipVisible = false;
        
        group.addEventListener('mouseenter', function(event) {
            // Clear any existing timeout
            if (tooltipTimeout) {
                clearTimeout(tooltipTimeout);
                tooltipTimeout = null;
            }
            
            // Set timeout for 2 seconds (2000ms)
            tooltipTimeout = setTimeout(function() {
                if (!isTooltipVisible) {
                    tooltipText.classList.add('show');
                    positionTooltip(event, tooltipText);
                    isTooltipVisible = true;
                }
            }, 2000);
        });
        
        group.addEventListener('mousemove', function(event) {
            // Only update position if tooltip is already visible
            if (isTooltipVisible && tooltipText.classList.contains('show')) {
                positionTooltip(event, tooltipText);
            }
        });
        
        group.addEventListener('mouseleave', function() {
            // Clear timeout immediately
            if (tooltipTimeout) {
                clearTimeout(tooltipTimeout);
                tooltipTimeout = null;
            }
            
            // Hide tooltip immediately and ensure it's not visible
            tooltipText.classList.remove('show');
            tooltipText.style.display = 'none';
            tooltipText.style.visibility = 'hidden';
            isTooltipVisible = false;
        });
        
        // Hide tooltip on click
        group.addEventListener('click', function() {
            // Clear timeout immediately
            if (tooltipTimeout) {
                clearTimeout(tooltipTimeout);
                tooltipTimeout = null;
            }
            
            // Hide tooltip immediately and ensure it's not visible
            tooltipText.classList.remove('show');
            tooltipText.style.display = 'none';
            tooltipText.style.visibility = 'hidden';
            isTooltipVisible = false;
        });
        
        // Hide tooltip on focus (for keyboard navigation)
        const button = group.querySelector('button, a, [tabindex]');
        if (button) {
            button.addEventListener('focus', function() {
                if (tooltipTimeout) {
                    clearTimeout(tooltipTimeout);
                    tooltipTimeout = null;
                }
                tooltipText.classList.remove('show');
                isTooltipVisible = false;
            });
        }
    }
    
    // Hide all tooltips when clicking anywhere on the document
    document.addEventListener('click', function(event) {
        // Check if the click is not on a tooltip element itself or its parent group
        if (!event.target.closest('.tooltip-delay') && !event.target.closest('.group')) {
            document.querySelectorAll('.tooltip-delay.show').forEach(function(tooltip) {
                tooltip.classList.remove('show');
                tooltip.style.display = 'none';
                tooltip.style.visibility = 'hidden';
            });
        }
    });
    
    // Hide all tooltips when pressing any key
    document.addEventListener('keydown', function() {
        document.querySelectorAll('.tooltip-delay.show').forEach(function(tooltip) {
            tooltip.classList.remove('show');
            tooltip.style.display = 'none';
            tooltip.style.visibility = 'hidden';
        });
    });
    
    // Hide all tooltips when mouse moves outside of any group
    document.addEventListener('mousemove', function(event) {
        // Check if mouse is not over any group element
        if (!event.target.closest('.group')) {
            document.querySelectorAll('.tooltip-delay.show').forEach(function(tooltip) {
                const group = tooltip.closest('.group');
                if (!group || !group.contains(event.target)) {
                    tooltip.classList.remove('show');
                    tooltip.style.display = 'none';
                    tooltip.style.visibility = 'hidden';
                }
            });
        }
    });
    
    // Initialize all existing tooltips
    document.querySelectorAll('.group').forEach(initTooltip);
    
    // Watch for dynamically added tooltips (Blazor)
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            mutation.addedNodes.forEach(function(node) {
                if (node.nodeType === 1) { // Element node
                    if (node.classList && node.classList.contains('group')) {
                        initTooltip(node);
                    }
                    const newGroups = node.querySelectorAll ? node.querySelectorAll('.group') : [];
                    newGroups.forEach(initTooltip);
                }
            });
        });
    });
    
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
});
