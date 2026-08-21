// Blazor reconnection handling
window.Blazor = window.Blazor || {};

// Handle circuit errors gracefully
if (window.Blazor && !window.Blazor._reconnectHandlerAttached) {
    window.Blazor._reconnectHandlerAttached = true;
    
    // Suppress excessive error logging for component operations
    const originalConsoleError = console.error;
    console.error = function(...args) {
        const message = args[0]?.toString() || '';
        
        // Filter out known component operation errors during navigation/state changes
        if (message.includes('The list of component operations is not valid') ||
            message.includes('Cannot send data if the connection is not in the Connected State')) {
            // Log as warning instead of error
            console.warn('[Blazor] Suppressed component operation error:', ...args);
            return;
        }
        
        originalConsoleError.apply(console, args);
    };
}

// Debug logging for MudBlazor NavMenu interactions (Development only)
window.mudNavMenuDebug = {
    enabled: false,
    
    init: function() {
        // Enable debug logging if in development mode
        const isDevelopment = document.documentElement.hasAttribute('data-development-mode');
        this.enabled = isDevelopment;
        
        if (this.enabled) {
            console.log('[MudNavMenu Debug] Debug logging enabled');
            this.attachEventListeners();
        }
    },
    
    attachEventListeners: function() {
        // Listen for clicks on nav groups
        document.addEventListener('click', (e) => {
            const navGroup = e.target.closest('.mud-nav-group');
            if (navGroup) {
                const title = navGroup.querySelector('.mud-nav-group-text')?.textContent;
                const isExpanded = navGroup.classList.contains('mud-nav-group-expanded');
                console.log('[MudNavMenu Debug] NavGroup clicked:', {
                    title: title,
                    currentlyExpanded: isExpanded,
                    element: navGroup
                });
            }
        }, true);
        
        // Monitor DOM mutations for NavGroup state changes
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                    const element = mutation.target;
                    if (element.classList.contains('mud-nav-group')) {
                        const title = element.querySelector('.mud-nav-group-text')?.textContent;
                        const isExpanded = element.classList.contains('mud-nav-group-expanded');
                        console.log('[MudNavMenu Debug] NavGroup state changed:', {
                            title: title,
                            isExpanded: isExpanded
                        });
                    }
                }
            });
        });
        
        // Observe the nav menu for changes
        setTimeout(() => {
            const navMenu = document.querySelector('.mud-navmenu');
            if (navMenu) {
                observer.observe(navMenu, {
                    attributes: true,
                    subtree: true,
                    attributeFilter: ['class']
                });
                console.log('[MudNavMenu Debug] Mutation observer attached to nav menu');
            }
        }, 1000);
    }
};

// Download file function for CSV exports
window.downloadFile = function(filename, contentType, base64Content) {
    const blob = base64ToBlob(base64Content, contentType);
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

// Helper function to convert base64 to Blob
function base64ToBlob(base64, contentType) {
    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    return new Blob([byteArray], { type: contentType });
}

// Theme management functions
window.themeManager = {
    getTheme: function() {
        return localStorage.getItem('darkMode') === 'true';
    },
    setTheme: function(isDarkMode) {
        localStorage.setItem('darkMode', isDarkMode.toString());
        // Also apply the theme class immediately
        if (isDarkMode) {
            document.documentElement.classList.add('mud-theme-dark');
        } else {
            document.documentElement.classList.remove('mud-theme-dark');
        }
    },
    hasPreference: function() {
        return localStorage.getItem('darkMode') !== null;
    },
    // Get the initial theme state (what was pre-applied in the head script)
    // NOTE: This logic mirrors the inline script in App.razor <head>
    // Both must stay in sync to prevent theme flash on page load
    getInitialTheme: function() {
        var darkMode = localStorage.getItem('darkMode');
        if (darkMode === 'true') {
            return true;
        } else if (darkMode === 'false') {
            return false;
        } else {
            // No preference saved, check system preference
            return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        }
    }
};

// View density management functions
window.viewDensityManager = {
    getViewDensity: function() {
        return localStorage.getItem('viewDensity') === 'compact' ? 'compact' : 'spacious';
    },
    setViewDensity: function(isCompact) {
        const density = isCompact ? 'compact' : 'spacious';
        localStorage.setItem('viewDensity', density);
        // Apply the view density class immediately
        document.documentElement.classList.remove('view-density-compact', 'view-density-spacious');
        document.documentElement.classList.add('view-density-' + density);
    },
    hasPreference: function() {
        return localStorage.getItem('viewDensity') !== null;
    },
    // Get the initial view density state (what was pre-applied in the head script)
    getInitialViewDensity: function() {
        var viewDensity = localStorage.getItem('viewDensity');
        if (viewDensity === 'compact') {
            return true; // true = compact
        } else {
            return false; // false = spacious (default)
        }
    },
    isSmallScreen: function() {
        if (window.matchMedia) {
            return window.matchMedia('(max-width: 640px)').matches;
        }

        return window.innerWidth <= 640;
    }
};

// Keyboard shortcuts manager
window.keyboardShortcuts = {
    init: function(dotNetHelper) {
        // Store reference to .NET helper
        this.dotNetHelper = dotNetHelper;
        
        // Add keyboard event listener
        document.addEventListener('keydown', (e) => {
            // Ignore if user is typing in an input field
            if (e.target.tagName === 'INPUT' || 
                e.target.tagName === 'TEXTAREA' || 
                e.target.isContentEditable) {
                return;
            }
            
            // Ctrl/Cmd + N: New Transaction
            if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
                e.preventDefault();
                this.navigate('/transactions/new');
            }
            // Ctrl/Cmd + B: Budgets
            else if ((e.ctrlKey || e.metaKey) && e.key === 'b') {
                e.preventDefault();
                this.navigate('/budgets');
            }
            // Ctrl/Cmd + T: Transactions
            else if ((e.ctrlKey || e.metaKey) && e.key === 't') {
                e.preventDefault();
                this.navigate('/transactions');
            }
            // Ctrl/Cmd + H: Home/Dashboard
            else if ((e.ctrlKey || e.metaKey) && e.key === 'h') {
                e.preventDefault();
                this.navigate('/');
            }
            // Ctrl/Cmd + G: Goals
            else if ((e.ctrlKey || e.metaKey) && e.key === 'g') {
                e.preventDefault();
                this.navigate('/goals');
            }
            // Ctrl/Cmd + I: Investments
            else if ((e.ctrlKey || e.metaKey) && e.key === 'i') {
                e.preventDefault();
                this.navigate('/investments');
            }
            // Ctrl/Cmd + K: Calendar
            else if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                this.navigate('/transactions/calendar');
            }
            // Ctrl/Cmd + L: Tags
            else if ((e.ctrlKey || e.metaKey) && e.key === 'l') {
                e.preventDefault();
                this.navigate('/tags');
            }
            // Ctrl/Cmd + /: Show keyboard shortcuts help
            else if ((e.ctrlKey || e.metaKey) && e.key === '/') {
                e.preventDefault();
                if (this.dotNetHelper) {
                    this.dotNetHelper.invokeMethodAsync('ShowKeyboardShortcutsHelp');
                }
            }
        });
    },
    
    // Navigate while preserving theme to prevent flash
    navigate: function(url) {
        // Ensure theme class persists during navigation
        var isDark = document.documentElement.classList.contains('mud-theme-dark');
        if (isDark) {
            // Add a temporary attribute to ensure theme persists
            document.documentElement.setAttribute('data-force-dark', 'true');
        }
        window.location.href = url;
    },
    
    dispose: function() {
        // Remove event listener if needed
        this.dotNetHelper = null;
    }
};

// PWA Service Worker registration and management
window.pwaManager = {
    deferredPrompt: null,
    isOnline: navigator.onLine,
    pendingTransactions: [],
    _isReloading: false,
    
    // Initialize PWA features
    init: function() {
        this.register();
        this.setupInstallPrompt();
        this.setupOnlineStatusMonitoring();
        this.setupServiceWorkerMessages();
        this.checkPendingTransactions();
    },
    
    // Register service worker
    register: function() {
        if (!('serviceWorker' in navigator)) {
            return;
        }

        const isLocalhost = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
        const isDevMode = document.documentElement.hasAttribute('data-development-mode');

        // In development/debug mode, unregister any stale service workers to prevent
        // cached workers from interfering with local changes, then exit.
        if (isLocalhost || isDevMode) {
            navigator.serviceWorker.getRegistrations().then(registrations => {
                registrations.forEach(registration => {
                    registration.unregister();
                    console.log('[PWA] Stale Service Worker unregistered in development mode');
                });
            }).catch(err => {
                console.error('[PWA] Failed to unregister Service Worker:', err);
            });
            console.log('[PWA] Service Worker disabled in development/debug mode');
            return;
        }

        // Reload the page whenever a new service worker takes control (e.g. after update).
        // The _isReloading flag prevents multiple reloads if the event fires more than once.
        navigator.serviceWorker.addEventListener('controllerchange', () => {
            if (!this._isReloading) {
                this._isReloading = true;
                console.log('[PWA] Service Worker controller changed, reloading...');
                window.location.reload();
            }
        });

        // Use updateViaCache: 'none' so the browser never serves the SW script from
        // HTTP cache, ensuring the latest version is always fetched on every check.
        navigator.serviceWorker.register('/service-worker.js', { updateViaCache: 'none' })
            .then((registration) => {
                console.log('[PWA] Service Worker registered:', registration);
                
                // Check for updates
                registration.addEventListener('updatefound', () => {
                    const newWorker = registration.installing;
                    console.log('[PWA] New service worker found');
                    
                    newWorker.addEventListener('statechange', () => {
                        if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                            // New service worker available
                            console.log('[PWA] New version available');
                            this.showUpdateNotification();
                        }
                    });
                });
                
                // Periodic update check (every hour)
                setInterval(() => {
                    registration.update();
                }, 60 * 60 * 1000);
            })
            .catch((error) => {
                console.error('[PWA] Service Worker registration failed:', error);
            });
    },
    
    // Setup install prompt capture
    setupInstallPrompt: function() {
        window.addEventListener('beforeinstallprompt', (e) => {
            console.log('[PWA] Install prompt available');
            e.preventDefault();
            this.deferredPrompt = e;
            
            // Notify app that install is available
            if (window.blazorPwaCallbacks && window.blazorPwaCallbacks.onInstallAvailable) {
                window.blazorPwaCallbacks.onInstallAvailable();
            }
        });
        
        window.addEventListener('appinstalled', () => {
            console.log('[PWA] App installed');
            this.deferredPrompt = null;
            
            // Notify app of successful installation
            if (window.blazorPwaCallbacks && window.blazorPwaCallbacks.onInstalled) {
                window.blazorPwaCallbacks.onInstalled();
            }
        });
    },
    
    // Show install prompt
    showInstallPrompt: async function() {
        if (!this.deferredPrompt) {
            console.log('[PWA] Install prompt not available');
            return false;
        }
        
        this.deferredPrompt.prompt();
        const { outcome } = await this.deferredPrompt.userChoice;
        console.log(`[PWA] User response to install prompt: ${outcome}`);
        
        this.deferredPrompt = null;
        return outcome === 'accepted';
    },
    
    // Check if app can be installed
    canInstall: function() {
        return this.deferredPrompt !== null;
    },
    
    // Monitor online/offline status
    setupOnlineStatusMonitoring: function() {
        window.addEventListener('online', () => {
            console.log('[PWA] Browser is online');
            this.isOnline = true;
            this.onOnline();
        });
        
        window.addEventListener('offline', () => {
            console.log('[PWA] Browser is offline');
            this.isOnline = false;
            this.onOffline();
        });
    },
    
    // Handle online event
    onOnline: function() {
        // Notify Blazor app
        if (window.blazorPwaCallbacks && window.blazorPwaCallbacks.onOnline) {
            window.blazorPwaCallbacks.onOnline();
        }
        
        // Trigger background sync if supported
        if ('serviceWorker' in navigator && 'sync' in navigator.serviceWorker) {
            navigator.serviceWorker.ready.then(registration => {
                return registration.sync.register('sync-transactions');
            }).then(() => {
                console.log('[PWA] Background sync registered');
            }).catch(err => {
                console.error('[PWA] Background sync failed:', err);
            });
        }
    },
    
    // Handle offline event
    onOffline: function() {
        // Notify Blazor app
        if (window.blazorPwaCallbacks && window.blazorPwaCallbacks.onOffline) {
            window.blazorPwaCallbacks.onOffline();
        }
    },
    
    // Setup service worker message handling
    setupServiceWorkerMessages: function() {
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker.addEventListener('message', (event) => {
                console.log('[PWA] Message from service worker:', event.data);
                
                if (event.data && event.data.type === 'TRANSACTION_SYNCED') {
                    // Notify Blazor app about synced transaction
                    if (window.blazorPwaCallbacks && window.blazorPwaCallbacks.onTransactionSynced) {
                        window.blazorPwaCallbacks.onTransactionSynced(event.data.transactionId);
                    }
                }

                if (event.data && event.data.type === 'SW_ACTIVATED') {
                    console.log('[PWA] Service Worker activated, version:', event.data.version);
                }

                if (event.data && event.data.type === 'SW_VERSION') {
                    console.log('[PWA] Service Worker version:', event.data.version);
                }
            });
        }
    },
    
    // Queue transaction for offline sync
    queueTransaction: async function(transactionData) {
        if (!('indexedDB' in window)) {
            console.error('[PWA] IndexedDB not supported');
            return false;
        }
        
        try {
            const db = await this.openDatabase();
            const transaction = db.transaction(['pendingTransactions'], 'readwrite');
            const store = transaction.objectStore('pendingTransactions');
            
            const item = {
                data: transactionData,
                timestamp: new Date().toISOString()
            };
            
            await new Promise((resolve, reject) => {
                const request = store.add(item);
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
            });
            
            console.log('[PWA] Transaction queued for sync');
            
            // Update pending count
            await this.checkPendingTransactions();
            
            db.close();
            return true;
        } catch (error) {
            console.error('[PWA] Failed to queue transaction:', error);
            return false;
        }
    },
    
    // Get pending transactions count
    checkPendingTransactions: async function() {
        if (!('indexedDB' in window)) {
            return 0;
        }
        
        try {
            const db = await this.openDatabase();
            const transaction = db.transaction(['pendingTransactions'], 'readonly');
            const store = transaction.objectStore('pendingTransactions');
            
            const count = await new Promise((resolve, reject) => {
                const request = store.count();
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
            });
            
            db.close();
            
            // Notify Blazor app
            if (window.blazorPwaCallbacks && window.blazorPwaCallbacks.onPendingCountChanged) {
                window.blazorPwaCallbacks.onPendingCountChanged(count);
            }
            
            return count;
        } catch (error) {
            console.error('[PWA] Failed to check pending transactions:', error);
            return 0;
        }
    },
    
    // Open IndexedDB
    openDatabase: function() {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open('PrivatekonomyOfflineDB', 1);
            
            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(request.result);
            
            request.onupgradeneeded = (event) => {
                const db = event.target.result;
                
                if (!db.objectStoreNames.contains('pendingTransactions')) {
                    const store = db.createObjectStore('pendingTransactions', { 
                        keyPath: 'id', 
                        autoIncrement: true 
                    });
                    store.createIndex('timestamp', 'timestamp', { unique: false });
                }
            };
        });
    },
    
    // Show update notification
    showUpdateNotification: function() {
        if (window.blazorPwaCallbacks && window.blazorPwaCallbacks.onUpdateAvailable) {
            window.blazorPwaCallbacks.onUpdateAvailable();
        }
    },
    
    // Apply update (activate waiting service worker; page reload is handled by controllerchange)
    applyUpdate: function() {
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker.ready.then(registration => {
                if (registration.waiting) {
                    registration.waiting.postMessage({ type: 'SKIP_WAITING' });
                }
            });
        }
    },
    
    // Request notification permission
    requestNotificationPermission: async function() {
        if (!('Notification' in window)) {
            console.log('[PWA] Notifications not supported');
            return false;
        }
        
        const permission = await Notification.requestPermission();
        console.log('[PWA] Notification permission:', permission);
        return permission === 'granted';
    },
    
    // Subscribe to push notifications
    subscribeToPush: async function() {
        if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
            console.log('[PWA] Push notifications not supported');
            return null;
        }
        
        try {
            const registration = await navigator.serviceWorker.ready;
            
            // Check if already subscribed
            let subscription = await registration.pushManager.getSubscription();
            
            if (!subscription) {
                // Request permission first
                const hasPermission = await this.requestNotificationPermission();
                if (!hasPermission) {
                    return null;
                }
                
                // TODO: Get VAPID public key from server API instead of hard-coding
                // Example: const vapidKey = await fetch('/api/push/vapid-key').then(r => r.text());
                // This is a placeholder key and should be replaced with the actual server key
                const vapidKey = 'BEl62iUYgUivxIkv69yViEuiBIa-Ib37J8xQmrpcPBblQjBIL1WsJ3-eN6_JG-eL5E2QdN3qZPTaC-lJQJqG1XY';
                
                // Subscribe to push
                subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: this.urlBase64ToUint8Array(vapidKey)
                });
            }
            
            return subscription;
        } catch (error) {
            console.error('[PWA] Failed to subscribe to push:', error);
            return null;
        }
    },
    
    // Helper to convert VAPID key
    urlBase64ToUint8Array: function(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/\-/g, '+')
            .replace(/_/g, '/');
        
        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);
        
        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    },
    
    // Check if app is running as PWA
    isRunningAsPWA: function() {
        return window.matchMedia('(display-mode: standalone)').matches ||
               window.navigator.standalone === true ||
               document.referrer.includes('android-app://');
    },

    // Register .NET callbacks for the OfflineIndicator component
    registerOfflineIndicator: function(dotNetRef) {
        window.blazorPwaCallbacks = window.blazorPwaCallbacks || {};
        window.blazorPwaCallbacks.onOnline = () => {
            dotNetRef.invokeMethodAsync('OnOnlineStatusChanged', true);
        };
        window.blazorPwaCallbacks.onOffline = () => {
            dotNetRef.invokeMethodAsync('OnOnlineStatusChanged', false);
        };
        window.blazorPwaCallbacks.onPendingCountChanged = (count) => {
            dotNetRef.invokeMethodAsync('OnPendingCountChanged', count);
        };
        window.blazorPwaCallbacks.offlineIndicatorRef = dotNetRef;
    },

    // Unregister OfflineIndicator callbacks
    unregisterOfflineIndicator: function() {
        if (window.blazorPwaCallbacks) {
            delete window.blazorPwaCallbacks.onOnline;
            delete window.blazorPwaCallbacks.onOffline;
            delete window.blazorPwaCallbacks.onPendingCountChanged;
            delete window.blazorPwaCallbacks.offlineIndicatorRef;
        }
    },

    // Get current online status
    getOnlineStatus: function() {
        return navigator.onLine;
    },

    // Register .NET callback for the UpdateNotification component
    registerUpdateNotification: function(dotNetRef) {
        window.blazorPwaCallbacks = window.blazorPwaCallbacks || {};
        window.blazorPwaCallbacks.onUpdateAvailable = () => {
            dotNetRef.invokeMethodAsync('OnUpdateAvailable');
        };
        window.blazorPwaCallbacks.updateNotificationRef = dotNetRef;
    },

    // Unregister UpdateNotification callbacks
    unregisterUpdateNotification: function() {
        if (window.blazorPwaCallbacks) {
            delete window.blazorPwaCallbacks.onUpdateAvailable;
            delete window.blazorPwaCallbacks.updateNotificationRef;
        }
    },

    // Register .NET callbacks for the InstallPwaPrompt component
    registerInstallPrompt: function(dotNetRef) {
        window.blazorPwaCallbacks = window.blazorPwaCallbacks || {};
        window.blazorPwaCallbacks.onInstallAvailable = () => {
            dotNetRef.invokeMethodAsync('OnInstallAvailable');
        };
        window.blazorPwaCallbacks.onInstalled = () => {
            dotNetRef.invokeMethodAsync('OnInstalled');
        };
        window.blazorPwaCallbacks.installPromptRef = dotNetRef;
    },

    // Unregister InstallPwaPrompt callbacks
    unregisterInstallPrompt: function() {
        if (window.blazorPwaCallbacks) {
            delete window.blazorPwaCallbacks.onInstallAvailable;
            delete window.blazorPwaCallbacks.onInstalled;
            delete window.blazorPwaCallbacks.installPromptRef;
        }
    }
};