// Session timeout management
let sessionTimeoutMinutes = 60; // Default: 1 hour
let idleTimer = null;
let warningTimer = null;
let lastActivityTime = Date.now();
let isWarningShown = false;

// Get session timeout from localStorage
function initializeSessionTimeout() {
    const storedTimeout = localStorage.getItem('sessionTimeout');
    if (storedTimeout && !isNaN(parseInt(storedTimeout))) {
        sessionTimeoutMinutes = parseInt(storedTimeout);
    }
    
    // If timeout is 0 (never), don't start the timer
    if (sessionTimeoutMinutes > 0) {
        resetIdleTimer();
    }
}

// Update session timeout
window.updateSessionTimeout = function(minutes) {
    sessionTimeoutMinutes = minutes;
    localStorage.setItem('sessionTimeout', minutes.toString());
    
    // Clear existing timers
    if (idleTimer) {
        clearTimeout(idleTimer);
    }
    if (warningTimer) {
        clearTimeout(warningTimer);
    }
    
    isWarningShown = false;
    lastActivityTime = Date.now();
    
    // If timeout is 0 (never), don't start the timer
    if (sessionTimeoutMinutes > 0) {
        resetIdleTimer();
    }
};

// Reset idle timer
function resetIdleTimer() {
    // Clear existing timers
    if (idleTimer) {
        clearTimeout(idleTimer);
    }
    if (warningTimer) {
        clearTimeout(warningTimer);
    }
    
    isWarningShown = false;
    lastActivityTime = Date.now();
    
    // Don't start timer if timeout is 0 (never)
    if (sessionTimeoutMinutes <= 0) {
        return;
    }
    
    const timeoutMs = sessionTimeoutMinutes * 60 * 1000;
    const warningTimeMs = Math.max(60000, timeoutMs - 60000); // Show warning 1 minute before timeout, or 1 minute if timeout is less than 2 minutes
    
    // Set warning timer (1 minute before logout)
    warningTimer = setTimeout(() => {
        showSessionWarning();
    }, Math.max(0, timeoutMs - warningTimeMs));
    
    // Set logout timer
    idleTimer = setTimeout(() => {
        handleSessionTimeout();
    }, timeoutMs);
}

// Store reference to MainLayout component
let mainLayoutRef = null;

window.setMainLayoutRef = function(dotNetRef) {
    mainLayoutRef = dotNetRef;
};

// Make resetIdleTimer available globally
window.resetIdleTimer = resetIdleTimer;

// Show session warning
function showSessionWarning() {
    if (isWarningShown) return;
    
    isWarningShown = true;
    const remainingMinutes = Math.ceil((sessionTimeoutMinutes * 60 * 1000 - (Date.now() - lastActivityTime)) / 60000);
    
    // Show warning to user via DotNet interop
    if (mainLayoutRef) {
        mainLayoutRef.invokeMethodAsync('ShowSessionWarning', remainingMinutes).catch(err => {
            console.error('Error showing session warning:', err);
            // Fallback: Show browser alert
            if (confirm(`Your session will expire in ${remainingMinutes} minute(s) due to inactivity. Click OK to continue your session.`)) {
                resetIdleTimer();
            }
        });
    } else {
        // Fallback: Show browser alert
        if (confirm(`Your session will expire in ${remainingMinutes} minute(s) due to inactivity. Click OK to continue your session.`)) {
            resetIdleTimer();
        }
    }
}

// Handle session timeout
function handleSessionTimeout() {
    // Clear timers
    if (idleTimer) {
        clearTimeout(idleTimer);
    }
    if (warningTimer) {
        clearTimeout(warningTimer);
    }
    
    // Show session expired modal via DotNet interop
    if (mainLayoutRef) {
        mainLayoutRef.invokeMethodAsync('HandleSessionTimeout').catch(err => {
            console.error('Error handling session timeout:', err);
            // Fallback: Redirect to login after a short delay
            setTimeout(() => {
                window.location.href = '/login';
            }, 2000);
        });
    } else {
        // Fallback: Redirect to login after a short delay
        setTimeout(() => {
            window.location.href = '/login';
        }, 2000);
    }
}

// Track user activity
const activityEvents = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart', 'click'];
activityEvents.forEach(event => {
    document.addEventListener(event, () => {
        if (sessionTimeoutMinutes > 0) {
            resetIdleTimer();
        }
    }, true);
});

// Initialize on page load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeSessionTimeout);
} else {
    initializeSessionTimeout();
}
