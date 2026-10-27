// Service Worker cho GiaoNhanh PWA - Network First để đảm bảo luôn cập nhật CSS/JS mới nhất
const CACHE_NAME = 'giaonhanh-pwa-v2';

self.addEventListener('install', event => {
  self.skipWaiting();
});

self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(keys => {
      return Promise.all(
        keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key))
      );
    })
  );
  self.clients.claim();
});

self.addEventListener('fetch', event => {
  if (event.request.method !== 'GET') return;
  const url = new URL(event.request.url);
  
  // Chỉ xử lý static assets: Network-First (ưu tiên nạp mới từ server, nếu offline mới dùng cache)
  if (url.pathname.startsWith('/css/') || 
      url.pathname.startsWith('/js/') || 
      url.pathname.startsWith('/lib/') || 
      url.pathname.startsWith('/img/') ||
      url.hostname.includes('fonts.googleapis.com') ||
      url.hostname.includes('fonts.gstatic.com') ||
      url.hostname.includes('cdn.jsdelivr.net')) {
    event.respondWith(
      fetch(event.request).then(response => {
        if (response && response.status === 200) {
          const copy = response.clone();
          caches.open(CACHE_NAME).then(cache => cache.put(event.request, copy));
        }
        return response;
      }).catch(() => caches.match(event.request))
    );
  }
});
