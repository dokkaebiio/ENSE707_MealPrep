// Minimal service worker. Caches the core app shell so the page still loads
// offline once visited, and — combined with manifest.json — satisfies the
// browser criteria for "installable as an app" on desktop and phone.
const CACHE_NAME = 'grocery-budget-cache-v1';
const ASSETS_TO_CACHE = [
    './',
    './index.html',
    './css/app.css',
    './manifest.json',
    './data/items.json'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(ASSETS_TO_CACHE))
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key)))
        )
    );
    self.clients.claim();
});

self.addEventListener('fetch', event => {
    event.respondWith(
        caches.match(event.request).then(cached => cached || fetch(event.request))
    );
});
