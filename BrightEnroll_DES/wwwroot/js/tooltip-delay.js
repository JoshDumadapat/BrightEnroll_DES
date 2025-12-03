// Tooltip delay handler - shows tooltips after 1 second delay
document.addEventListener('DOMContentLoaded', function() {
    let tooltipTimeout = null;
    
    // Function to position tooltip using fixed positioning with viewport boundary detection
    function positionTooltip(group, tooltipText) {
        const rect = group.getBoundingClientRect();
        const tooltipRect = tooltipText.getBoundingClientRect();
        const viewportWidth = window.innerWidth;
        const viewportHeight = window.innerHeight;
        const margin = 8; // Margin from edges
        const spacing = 4; // Spacing from element
        
        // Get tooltip dimensions (approximate if not visible)
        const tooltipWidth = tooltipRect.width || 200; // Fallback width
        const tooltipHeight = tooltipRect.height || 30; // Fallback height
        
        let left, top;
        let transform = '';
        
        // Calculate preferred position (bottom center)
        const preferredLeft = rect.left + (rect.width / 2);
        const preferredTop = rect.bottom + spacing;
        
        // Check if tooltip fits at bottom
        const fitsBottom = preferredTop + tooltipHeight + margin <= viewportHeight;
        // Check if tooltip fits at top
        const fitsTop = rect.top - tooltipHeight - spacing - margin >= 0;
        // Check if tooltip fits horizontally
        const fitsLeft = preferredLeft - (tooltipWidth / 2) - margin >= 0;
        const fitsRight = preferredLeft + (tooltipWidth / 2) + margin <= viewportWidth;
        
        // Determine vertical position
        if (fitsBottom) {
            // Position below element
            top = preferredTop;
            transform = 'translateX(-50%)';
        } else if (fitsTop) {
            // Position above element
            top = rect.top - tooltipHeight - spacing;
            transform = 'translateX(-50%)';
        } else {
            // Center vertically if neither fits
            top = Math.max(margin, Math.min(viewportHeight - tooltipHeight - margin, rect.top + (rect.height / 2) - (tooltipHeight / 2)));
            transform = 'translateX(-50%)';
        }
        
        // Determine horizontal position
        if (fitsLeft && fitsRight) {
            // Center horizontally
            left = preferredLeft;
        } else if (!fitsLeft) {
            // Align to left edge with margin
            left = margin + (tooltipWidth / 2);
        } else {
            // Align to right edge with margin
            left = viewportWidth - margin - (tooltipWidth / 2);
        }
        
        tooltipText.style.left = left + 'px';
        tooltipText.style.top = top + 'px';
        tooltipText.style.transform = transform;
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
                            
                            // Function to position tooltip with viewport boundary detection
                            function positionTooltip(group, tooltipText) {
                                const rect = group.getBoundingClientRect();
                                const tooltipRect = tooltipText.getBoundingClientRect();
                                const viewportWidth = window.innerWidth;
                                const viewportHeight = window.innerHeight;
                                const margin = 8;
                                const spacing = 4;
                                
                                const tooltipWidth = tooltipRect.width || 200;
                                const tooltipHeight = tooltipRect.height || 30;
                                
                                let left, top;
                                let transform = '';
                                
                                const preferredLeft = rect.left + (rect.width / 2);
                                const preferredTop = rect.bottom + spacing;
                                
                                const fitsBottom = preferredTop + tooltipHeight + margin <= viewportHeight;
                                const fitsTop = rect.top - tooltipHeight - spacing - margin >= 0;
                                const fitsLeft = preferredLeft - (tooltipWidth / 2) - margin >= 0;
                                const fitsRight = preferredLeft + (tooltipWidth / 2) + margin <= viewportWidth;
                                
                                if (fitsBottom) {
                                    top = preferredTop;
                                    transform = 'translateX(-50%)';
                                } else if (fitsTop) {
                                    top = rect.top - tooltipHeight - spacing;
                                    transform = 'translateX(-50%)';
                                } else {
                                    top = Math.max(margin, Math.min(viewportHeight - tooltipHeight - margin, rect.top + (rect.height / 2) - (tooltipHeight / 2)));
                                    transform = 'translateX(-50%)';
                                }
                                
                                if (fitsLeft && fitsRight) {
                                    left = preferredLeft;
                                } else if (!fitsLeft) {
                                    left = margin + (tooltipWidth / 2);
                                } else {
                                    left = viewportWidth - margin - (tooltipWidth / 2);
                                }
                                
                                tooltipText.style.left = left + 'px';
                                tooltipText.style.top = top + 'px';
                                tooltipText.style.transform = transform;
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

