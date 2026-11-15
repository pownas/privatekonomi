// Service Worker for Privatekonomi PWA
const CACHE_VERSION = 'v3';
const STATIC_CACHE_NAME = `privatekonomi-static-${CACHE_VERSION}`;
const DYNAMIC_CACHE_NAME = `privatekonomi-dynamic-${CACHE_VERSION}`;
const OFFLINE_PAGE = '/offline.html';

// Static resources to cache on install
const STATIC_ASSETS = [
  '/',
  '/app.css',
  '/app.js',
  '/mobile-gestures.js',
  '/favicon.png',
  '/icon-192x192.png',
  '/icon-512x512.png',
  '/apple-touch-icon.png',
  '/manifest.json',
  '/_framework/blazor.web.js',
  '/_content/MudBlazor/MudBlazor.min.css',
  '/_content/MudBlazor/MudBlazor.min.js'
];

// IndexedDB configuration for offline transaction queue
const DB_NAME = 'PrivatekonomyOfflineDB';
const DB_VERSION = 1;
const TRANSACTION_STORE = 'pendingTransactions';

// Install event - cache static resources
self.addEventListener('install', (event) => {
  console.log('[Service Worker] Installing...');
  event.waitUntil(
    caches.open(STATIC_CACHE_NAME)
      .then((cache) => {
        console.log('[Service Worker] Caching static assets');
        return cache.addAll(STATIC_ASSETS.map(url => new Request(url, {cache: 'reload'})));
      })
      .catch((error) => {
        console.error('[Service Worker] Failed to cache static assets:', error);
      })
      .then(() => self.skipWaiting())
  );
});

// Activate event - cleanup old caches and claim clients
self.addEventListener('activate', (event) => {
  console.log('[Service Worker] Activating...');
  event.waitUntil(
    caches.keys()
      .then((cacheNames) => {
        return Promise.all(
          cacheNames.map((cacheName) => {
            if (cacheName !== STATIC_CACHE_NAME && cacheName !== DYNAMIC_CACHE_NAME) {
              console.log('[Service Worker] Deleting old cache:', cacheName);
              return caches.delete(cacheName);
            }
          })
        );
      })
      .then(() => self.clients.claim())
      .then(() => notifyClients({ type: 'SW_ACTIVATED', version: CACHE_VERSION }))
  );
});

// Fetch event - Network first, fallback to cache strategy
self.addEventListener('fetch', (event) => {
  const { request } = event;
  const url = new URL(request.url);

  // Skip non-GET requests
  if (request.method !== 'GET') {
    return;
  }

  // Skip chrome extension requests
  if (url.protocol === 'chrome-extension:') {
    return;
  }

  // Handle Blazor SignalR connections (don't cache)
  if (url.pathname.includes('/_blazor')) {
    return;
  }

  // Handle API calls and dynamic content with network-first strategy
  if (url.pathname.startsWith('/api/') || 
      url.pathname.startsWith('/Account/') ||
      url.pathname.includes('/_framework/') ||
      url.pathname.startsWith('/_content/')) {
    event.respondWith(networkFirst(request));
    return;
  }

  // Handle static assets with cache-first strategy
  event.respondWith(cacheFirst(request));
});

// Network-first strategy (with cache fallback)
async function networkFirst(request) {
  try {
    const networkResponse = await fetch(request);
    
    // Cache successful responses
    if (networkResponse && networkResponse.status === 200) {
      const cache = await caches.open(DYNAMIC_CACHE_NAME);
      cache.put(request, networkResponse.clone());
    }
    
    return networkResponse;
  } catch (error) {
    console.log('[Service Worker] Network request failed, trying cache:', request.url);
    
    const cachedResponse = await caches.match(request);
    if (cachedResponse) {
      return cachedResponse;
    }
    
    // Return offline page for navigation requests
    if (request.mode === 'navigate') {
      const offlinePage = await caches.match(OFFLINE_PAGE);
      if (offlinePage) {
        return offlinePage;
      }
    }
    
    throw error;
  }
}

// Cache-first strategy (with network fallback)
async function cacheFirst(request) {
  const cachedResponse = await caches.match(request);
  
  if (cachedResponse) {
    return cachedResponse;
  }
  
  try {
    const networkResponse = await fetch(request);
    
    // Cache the response for future use
    if (networkResponse && networkResponse.status === 200) {
      const cache = await caches.open(DYNAMIC_CACHE_NAME);
      cache.put(request, networkResponse.clone());
    }
    
    return networkResponse;
  } catch (error) {
    console.log('[Service Worker] Failed to fetch from network and cache:', request.url);
    throw error;
  }
}

// Background Sync for offline transactions
self.addEventListener('sync', (event) => {
  console.log('[Service Worker] Background sync triggered:', event.tag);
  
  if (event.tag === 'sync-transactions') {
    event.waitUntil(syncPendingTransactions());
  }
});

// Sync pending transactions from IndexedDB
async function syncPendingTransactions() {
  try {
    const db = await openDatabase();
    const transactions = await getAllPendingTransactions(db);
    
    console.log(`[Service Worker] Syncing ${transactions.length} pending transactions`);
    
    for (const transaction of transactions) {
      try {
        const response = await fetch('/api/transactions', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(transaction.data)
        });
        
        if (response.ok) {
          console.log('[Service Worker] Transaction synced successfully:', transaction.id);
          await deletePendingTransaction(db, transaction.id);
          
          // Notify clients about successful sync
          await notifyClients({
            type: 'TRANSACTION_SYNCED',
            transactionId: transaction.id
          });
        }
      } catch (error) {
        console.error('[Service Worker] Failed to sync transaction:', error);
      }
    }
    
    db.close();
  } catch (error) {
    console.error('[Service Worker] Background sync failed:', error);
    throw error; // Retry sync later
  }
}

// IndexedDB helpers
function openDatabase() {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);
    
    request.onerror = () => reject(request.error);
    request.onsuccess = () => resolve(request.result);
    
    request.onupgradeneeded = (event) => {
      const db = event.target.result;
      
      if (!db.objectStoreNames.contains(TRANSACTION_STORE)) {
        const store = db.createObjectStore(TRANSACTION_STORE, { keyPath: 'id', autoIncrement: true });
        store.createIndex('timestamp', 'timestamp', { unique: false });
      }
    };
  });
}

function getAllPendingTransactions(db) {
  return new Promise((resolve, reject) => {
    const transaction = db.transaction([TRANSACTION_STORE], 'readonly');
    const store = transaction.objectStore(TRANSACTION_STORE);
    const request = store.getAll();
    
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

function deletePendingTransaction(db, id) {
  return new Promise((resolve, reject) => {
    const transaction = db.transaction([TRANSACTION_STORE], 'readwrite');
    const store = transaction.objectStore(TRANSACTION_STORE);
    const request = store.delete(id);
    
    request.onsuccess = () => resolve();
    request.onerror = () => reject(request.error);
  });
}

// Notify all clients
async function notifyClients(message) {
  const clients = await self.clients.matchAll({ includeUncontrolled: true });
  clients.forEach(client => {
    client.postMessage(message);
  });
}

// Push notification support
self.addEventListener('push', (event) => {
  console.log('[Service Worker] Push notification received');
  
  const data = event.data ? event.data.json() : {};
  const title = data.title || 'Privatekonomi';
  const options = {
    body: data.body || 'Du har en ny notifikation',
    icon: '/icon-192x192.png',
    badge: '/favicon.png',
    vibrate: [200, 100, 200],
    data: data,
    actions: data.actions || []
  };
  
  event.waitUntil(
    self.registration.showNotification(title, options)
  );
});

// Handle notification clicks
self.addEventListener('notificationclick', (event) => {
  console.log('[Service Worker] Notification clicked:', event.notification.tag);
  
  event.notification.close();
  
  event.waitUntil(
    clients.openWindow(event.notification.data?.url || '/')
  );
});

// Message handler for communication with clients
self.addEventListener('message', (event) => {
  console.log('[Service Worker] Message received:', event.data);
  
  if (event.data && event.data.type === 'SKIP_WAITING') {
    self.skipWaiting();
  }
  
  if (event.data && event.data.type === 'GET_VERSION') {
    if (event.source) {
      event.source.postMessage({ type: 'SW_VERSION', version: CACHE_VERSION });
    }
  }
  
  if (event.data && event.data.type === 'CACHE_URLS') {
    event.waitUntil(
      caches.open(DYNAMIC_CACHE_NAME)
        .then(cache => cache.addAll(event.data.urls))
    );
  }
});
