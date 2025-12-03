// Connectivity Monitor for Blazor
// Monitors browser online/offline events and notifies C# code

window.connectivityMonitor = {
    dotNetRef: null,
    isInitialized: false,

    // Initialize the connectivity monitor with a .NET reference
    initialize: function (dotNetRef) {
        if (this.isInitialized) {
            console.warn('Connectivity monitor already initialized');
            return;
        }

        if (!dotNetRef) {
            console.error('Connectivity monitor: dotNetRef is required');
            return;
        }

        this.dotNetRef = dotNetRef;
        this.isInitialized = true;

        // Store bound handlers for cleanup
        this.onlineHandler = () => {
            this.handleOnlineStatusChange(true);
        };
        this.offlineHandler = () => {
            this.handleOnlineStatusChange(false);
        };

        // Listen to online event
        window.addEventListener('online', this.onlineHandler);

        // Listen to offline event
        window.addEventListener('offline', this.offlineHandler);

        console.log('Connectivity monitor initialized');
    },

    // Handle connectivity status changes
    handleOnlineStatusChange: function (isOnline) {
        if (this.dotNetRef) {
            try {
                this.dotNetRef.invokeMethodAsync('OnConnectivityChanged', isOnline);
            } catch (error) {
                console.error('Error notifying .NET about connectivity change:', error);
            }
        }
    },

    // Check current connectivity status
    checkConnectivity: function () {
        return navigator.onLine;
    },

    // Cleanup - remove event listeners
    dispose: function () {
        if (this.isInitialized) {
            if (this.onlineHandler) {
                window.removeEventListener('online', this.onlineHandler);
            }
            if (this.offlineHandler) {
                window.removeEventListener('offline', this.offlineHandler);
            }
            this.dotNetRef = null;
            this.onlineHandler = null;
            this.offlineHandler = null;
            this.isInitialized = false;
            console.log('Connectivity monitor disposed');
        }
    }
};

