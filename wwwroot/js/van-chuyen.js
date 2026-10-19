// Vận chuyển nhiều chặng: quét mã QR / barcode, vẽ mã vận đơn, bản đồ vị trí đơn, toạ độ lúc quét, chia sẻ GPS của shipper.
// Thư viện ngoài (nạp ở trang cần dùng): html5-qrcode (quét), qrcode-generator + JsBarcode (vẽ mã), Leaflet (bản đồ).

// ---------- Toạ độ lúc thao tác: điền vào mọi input[name=viDo] / [name=kinhDo] nếu người dùng cho phép ----------
(function ganToaDo() {
  const o = document.querySelectorAll('input[name="viDo"]');
  if (!o.length || !navigator.geolocation) return;
  navigator.geolocation.getCurrentPosition(p => {
    document.querySelectorAll('input[name="viDo"]').forEach(i => i.value = p.coords.latitude.toFixed(6));
    document.querySelectorAll('input[name="kinhDo"]').forEach(i => i.value = p.coords.longitude.toFixed(6));
  }, () => { /* không cho phép vị trí → bỏ qua, thao tác vẫn hợp lệ */ }, { enableHighAccuracy: true, timeout: 8000, maximumAge: 60000 });
})();

// ---------- Quét QR / barcode bằng camera (html5-qrcode); luôn có ô nhập mã tay dự phòng ----------
function khoiTaoQuet(idVung, idO, idForm, idNut) {
  const nut = document.getElementById(idNut), vung = document.getElementById(idVung);
  if (!nut || !vung) return;
  let mayQuet = null;
  const dung = () => { if (mayQuet) { mayQuet.stop().catch(() => {}); mayQuet = null; } vung.classList.add('d-none'); nut.innerHTML = '<i class="bi bi-camera"></i> Bật camera quét mã'; };
  nut.addEventListener('click', async () => {
    if (mayQuet) { dung(); return; }
    if (typeof Html5Qrcode === 'undefined') { alert('Không tải được thư viện quét mã – hãy nhập mã vận đơn bằng tay.'); return; }
    vung.classList.remove('d-none');
    nut.innerHTML = '<i class="bi bi-camera-video-off"></i> Tắt camera';
    mayQuet = new Html5Qrcode(idVung);
    try {
      await mayQuet.start({ facingMode: 'environment' }, { fps: 10, qrbox: { width: 260, height: 160 } }, ma => {
        document.getElementById(idO).value = ma.trim();
        dung();
        if (typeof phatAmBip === 'function') phatAmBip();
        else if (navigator.vibrate) navigator.vibrate(80);
        document.getElementById(idForm).submit();
      }, () => {});
    } catch (e) {
      dung();
      alert('Không mở được camera (cần cho phép camera và chạy trên HTTPS / localhost). Hãy nhập mã vận đơn bằng tay.');
    }
  });
}

// ---------- Vẽ mã vận đơn: <div data-qr="DH..."> → QR (SVG); <svg data-barcode="DH..."> → barcode CODE128 ----------
function veMaVanDon() {
  document.querySelectorAll('[data-qr]').forEach(el => {
    if (typeof qrcode === 'undefined') return;
    const qr = qrcode(0, 'M');
    qr.addData(el.dataset.qr);
    qr.make();
    el.innerHTML = qr.createSvgTag({ cellSize: Number(el.dataset.coO || 4), margin: 2, scalable: true });
  });
  document.querySelectorAll('svg[data-barcode]').forEach(el => {
    if (typeof JsBarcode === 'undefined') return;
    JsBarcode(el, el.dataset.barcode, { format: 'CODE128', height: Number(el.dataset.cao || 50), displayValue: el.dataset.hienChu !== '0', fontSize: 14, margin: 4 });
  });
}

// ---------- Bản đồ vị trí đơn (Leaflet + OpenStreetMap); data-url: JSON ViTriDon để tự làm mới khi shipper đang giữ hàng ----------
function veBanDo() {
  document.querySelectorAll('[data-ban-do]').forEach(el => {
    if (typeof L === 'undefined') return;
    const vi = Number(el.dataset.viDo), kinh = Number(el.dataset.kinhDo);
    if (!vi || !kinh) return;
    const banDo = L.map(el, { scrollWheelZoom: false }).setView([vi, kinh], Number(el.dataset.zoom || 14));
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19, attribution: '&copy; OpenStreetMap'
    }).addTo(banDo);
    const icon = L.divIcon({ className: 'ghim-ban-do', html: `<span class="${el.dataset.loai === 'shipper' ? 'ghim-shipper' : 'ghim-buu-cuc'}"><i class="bi ${el.dataset.loai === 'shipper' ? 'bi-truck' : 'bi-building'}"></i></span>`, iconSize: [36, 36], iconAnchor: [18, 18] });
    const ghim = L.marker([vi, kinh], { icon }).addTo(banDo).bindPopup(el.dataset.nhan || '');
    const url = el.dataset.url;
    if (url && el.dataset.loai === 'shipper') {
      setInterval(async () => {
        try {
          const r = await fetch(url, { headers: { 'Accept': 'application/json' } });
          if (!r.ok) return;
          const v = await r.json();
          if (v.viDo && v.kinhDo) { ghim.setLatLng([v.viDo, v.kinhDo]); banDo.panTo([v.viDo, v.kinhDo]); }
          const moTa = document.querySelector(el.dataset.moTa || '#viTriMoTa');
          if (moTa && v.moTa) moTa.textContent = v.moTa;
        } catch { /* mất mạng tạm thời */ }
      }, 30000);
    }
  });
}

// ---------- Shipper chia sẻ vị trí: gửi GPS mỗi ≥ 30 giây (hoặc khi đi được > 50 m) khi bật công tắc ----------
function chiaSeViTri(idNut, urlGui) {
  const nut = document.getElementById(idNut);
  if (!nut || !navigator.geolocation) { if (nut) nut.classList.add('d-none'); return; }
  const KHOA = 'giaonhan.chiaSeViTri';
  let theoDoi = null, lanGui = 0, viTriGui = null;
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
  const khoangCach = (a, b) => {
    const R = 6371000, r = x => x * Math.PI / 180;
    const d1 = r(b.lat - a.lat), d2 = r(b.lng - a.lng);
    const h = Math.sin(d1 / 2) ** 2 + Math.cos(r(a.lat)) * Math.cos(r(b.lat)) * Math.sin(d2 / 2) ** 2;
    return 2 * R * Math.asin(Math.sqrt(h));
  };
  const hienThi = (bat, ghiChu) => {
    nut.classList.toggle('dang-bat', bat);
    nut.innerHTML = bat ? `<i class="bi bi-broadcast"></i> Đang chia sẻ vị trí${ghiChu ? ' · ' + ghiChu : ''}` : '<i class="bi bi-geo-alt"></i> Bật chia sẻ vị trí';
  };
  const gui = async p => {
    const moi = { lat: p.coords.latitude, lng: p.coords.longitude };
    if (Date.now() - lanGui < 30000 && viTriGui && khoangCach(viTriGui, moi) < 50) return;
    lanGui = Date.now(); viTriGui = moi;
    const du = new URLSearchParams({ viDo: moi.lat.toFixed(6), kinhDo: moi.lng.toFixed(6) });
    try {
      const r = await fetch(urlGui, { method: 'POST', headers: { 'RequestVerificationToken': token || '', 'Content-Type': 'application/x-www-form-urlencoded' }, body: du });
      if (r.ok) { const k = await r.json(); hienThi(true, k.luc); }
    } catch { /* thử lại ở lần cập nhật sau */ }
  };
  const bat = () => {
    theoDoi = navigator.geolocation.watchPosition(gui, () => { tat(); alert('Không lấy được vị trí – hãy cho phép truy cập vị trí cho trang này.'); },
                                                  { enableHighAccuracy: true, maximumAge: 15000 });
    try { localStorage.setItem(KHOA, '1'); } catch { }
    hienThi(true);
  };
  const tat = () => {
    if (theoDoi !== null) navigator.geolocation.clearWatch(theoDoi);
    theoDoi = null;
    try { localStorage.removeItem(KHOA); } catch { }
    hienThi(false);
  };
  nut.addEventListener('click', () => theoDoi === null ? bat() : tat());
  let daBat = false;
  try { daBat = localStorage.getItem(KHOA) === '1'; } catch { }
  daBat ? bat() : hienThi(false);
}

document.addEventListener('DOMContentLoaded', () => { veMaVanDon(); veBanDo(); });
