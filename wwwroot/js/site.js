const dinhDangTien = v => Number(v).toLocaleString('vi-VN') + ' đ';

// ===================== HỘP THOẠI XÁC NHẬN & THÔNG BÁO NỔI (POPUP MODAL) =====================
function layPopupModal() {
  let overlay = document.getElementById('popupChung');
  if (!overlay) {
    overlay = document.createElement('div');
    overlay.id = 'popupChung';
    overlay.className = 'popup-overlay';
    overlay.setAttribute('role', 'dialog');
    overlay.setAttribute('aria-modal', 'true');
    overlay.innerHTML = `
      <div class="popup-hop">
        <div class="popup-icon-khung" id="popupIcon"><i class="bi bi-question-circle"></i></div>
        <div class="popup-tieu-de" id="popupTieuDe">Xác nhận</div>
        <div class="popup-noi-dung" id="popupNoiDung"></div>
        <div class="popup-nut-khung">
          <button type="button" class="btn btn-light" id="popupNutHuy">Hủy</button>
          <button type="button" class="btn btn-cam" id="popupNutXacNhan">Xác nhận</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
  }
  return overlay;
}

function hienPopupXacNhan(options) {
  return new Promise(resolve => {
    const opts = typeof options === 'string' ? { noiDung: options } : (options || {});
    const noiDung = (opts.noiDung || '').trim();

    // Tự động phân loại dựa trên nội dung câu hỏi (sử dụng unicode boundary (?<!\p{L}) để tránh 'chuyến'/'chuyển' bị trùng với 'hủy')
    const laXuatBen = /(?<![\p{L}\p{N}])(xuất bến|xuat ben)(?![\p{L}\p{N}])/iu.test(noiDung);
    const laRoiTram = /(?<![\p{L}\p{N}])(rời trạm|roi tram)(?![\p{L}\p{N}])/iu.test(noiDung);
    const laGheTram = /(?<![\p{L}\p{N}])(ghé trạm|ghe tram)(?![\p{L}\p{N}])/iu.test(noiDung);
    const laTiepNhan = /(?<![\p{L}\p{N}])(tiếp nhận|tiep nhan)(?![\p{L}\p{N}])/iu.test(noiDung);
    const laDongYeuCau = /(?<![\p{L}\p{N}])(đóng yêu cầu|dong yeu cau)(?![\p{L}\p{N}])/iu.test(noiDung);
    const laDangXuat = /(?<![\p{L}\p{N}])(đăng xuất|dang xuat)(?![\p{L}\p{N}])/iu.test(noiDung);
    const laXoa = /(?<![\p{L}\p{N}])(xóa|xoa|dỡ kiện hàng|do kien hang)(?![\p{L}\p{N}])/iu.test(noiDung);
    const laKhoa = /(?<![\p{L}\p{N}])(khóa tài khoản|khoa tai khoan|khóa|khoa)(?![\p{L}\p{N}])/iu.test(noiDung);
    const laTuChoi = /(?<![\p{L}\p{N}])(từ chối|tu choi)(?![\p{L}\p{N}])/iu.test(noiDung);
    const laHuy = /(?<![\p{L}\p{N}])(hủy|huy)(?![\p{L}\p{N}])/iu.test(noiDung);
    const laHoanTat = /(?<![\p{L}\p{N}])(hoàn tất|hoan tat|nhận lại|nhan lai|xác nhận đã|xac nhan da|thành công|thanh cong)(?![\p{L}\p{N}])/iu.test(noiDung);

    let kieu = opts.kieu;
    let iconClass = opts.icon;
    let tieuDe = opts.tieuDe;
    let nutOkText = opts.nutXacNhan;

    if (!kieu) {
      if (laXuatBen || laRoiTram || laHoanTat) kieu = 'success';
      else if (laDangXuat || laXoa || laKhoa || laTuChoi) kieu = 'danger';
      else if (laGheTram || laTiepNhan || laHuy) kieu = 'warning';
      else if (laDongYeuCau) kieu = 'info';
      else kieu = 'cam';
    }

    if (!iconClass) {
      if (laXuatBen) iconClass = 'bi-send-fill';
      else if (laRoiTram) iconClass = 'bi-truck';
      else if (laGheTram || laTiepNhan) iconClass = 'bi-box-arrow-in-down';
      else if (laDangXuat) iconClass = 'bi-box-arrow-right';
      else if (laDongYeuCau) iconClass = 'bi-check-circle-fill';
      else if (laXoa) iconClass = 'bi-trash3-fill';
      else if (laKhoa) iconClass = 'bi-lock-fill';
      else if (laTuChoi || laHuy) iconClass = 'bi-x-circle-fill';
      else if (laHoanTat) iconClass = 'bi-check2-circle';
      else iconClass = 'bi-question-circle-fill';
    }

    if (!tieuDe) {
      if (laXuatBen) tieuDe = 'Xác nhận xuất bến';
      else if (laRoiTram) tieuDe = 'Xác nhận rời trạm';
      else if (laGheTram) tieuDe = 'Xác nhận ghé trạm';
      else if (laTiepNhan) tieuDe = 'Xác nhận tiếp nhận';
      else if (laDongYeuCau) tieuDe = 'Đóng yêu cầu hỗ trợ';
      else if (laDangXuat) tieuDe = 'Đăng xuất tài khoản';
      else if (laXoa) tieuDe = 'Xác nhận xóa';
      else if (laKhoa) tieuDe = 'Khóa / Mở khóa tài khoản';
      else if (laTuChoi) tieuDe = 'Từ chối giao dịch';
      else if (laHuy) tieuDe = 'Xác nhận hủy';
      else if (laHoanTat) tieuDe = 'Xác nhận hoàn tất';
      else tieuDe = 'Xác nhận thao tác';
    }

    if (!nutOkText) {
      if (laXuatBen) nutOkText = 'Xuất bến ngay';
      else if (laRoiTram) nutOkText = 'Rời trạm đi tiếp';
      else if (laGheTram) nutOkText = 'Ghé trạm & dỡ hàng';
      else if (laTiepNhan) nutOkText = 'Tiếp nhận';
      else if (laDongYeuCau) nutOkText = 'Đóng yêu cầu';
      else if (laDangXuat) nutOkText = 'Đăng xuất';
      else if (laXoa) nutOkText = 'Xóa';
      else if (laKhoa) nutOkText = 'Đồng ý';
      else if (laTuChoi) nutOkText = 'Từ chối';
      else if (laHuy) nutOkText = 'Hủy';
      else nutOkText = 'Xác nhận';
    }

    const overlay = layPopupModal();
    const iconEl = overlay.querySelector('#popupIcon');
    const tieuDeEl = overlay.querySelector('#popupTieuDe');
    const noiDungEl = overlay.querySelector('#popupNoiDung');
    const nutHuy = overlay.querySelector('#popupNutHuy');
    const nutOk = overlay.querySelector('#popupNutXacNhan');

    // Tách câu hỏi chính và ghi chú chi tiết nếu có
    let cauHoiChinh = noiDung;
    let ghiChu = '';
    const idxDauHoi = noiDung.indexOf('?');
    if (idxDauHoi !== -1 && idxDauHoi < noiDung.length - 1) {
      cauHoiChinh = noiDung.slice(0, idxDauHoi + 1).trim();
      ghiChu = noiDung.slice(idxDauHoi + 1).trim();
    }

    iconEl.className = `popup-icon-khung icon-${kieu}`;
    iconEl.innerHTML = `<i class="bi ${iconClass}"></i>`;

    tieuDeEl.textContent = tieuDe;
    if (ghiChu) {
      noiDungEl.innerHTML = `<div class="popup-cau-hoi">${cauHoiChinh}</div><div class="popup-ghi-chu">${ghiChu}</div>`;
    } else {
      noiDungEl.innerHTML = `<div class="popup-cau-hoi">${cauHoiChinh}</div>`;
    }

    nutHuy.textContent = opts.nutHuy || 'Hủy';
    nutHuy.style.display = opts.chiThongBao ? 'none' : 'inline-flex';

    nutOk.textContent = nutOkText;
    nutOk.className = `btn btn-${kieu === 'cam' ? 'cam' : kieu}`;

    let daXong = false;
    const bodyOverflowCu = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    function dong(dongY) {
      if (daXong) return;
      daXong = true;
      document.body.style.overflow = bodyOverflowCu;
      overlay.classList.remove('active');
      document.removeEventListener('keydown', batPhim);
      setTimeout(() => resolve(dongY), 200);
    }

    function batPhim(ev) {
      if (ev.key === 'Escape') {
        ev.preventDefault();
        dong(false);
      } else if (ev.key === 'Enter' && !ev.shiftKey) {
        ev.preventDefault();
        dong(true);
      }
    }

    nutHuy.onclick = () => dong(false);
    nutOk.onclick = () => dong(true);
    overlay.onclick = e => {
      if (e.target === overlay) dong(false);
    };

    document.addEventListener('keydown', batPhim);
    overlay.classList.add('active');
    setTimeout(() => nutOk.focus(), 50);
  });
}

function hienPopupThongBao(options) {
  const opts = typeof options === 'string' ? { noiDung: options } : (options || {});
  return hienPopupXacNhan({
    ...opts,
    chiThongBao: true,
    tieuDe: opts.tieuDe || 'Thông báo',
    nutXacNhan: opts.nutXacNhan || 'Đã hiểu',
    kieu: opts.kieu || 'cam',
    icon: opts.icon || 'bi-info-circle-fill'
  });
}

window.xacNhan = hienPopupXacNhan;
window.thongBao = hienPopupThongBao;
window.alert = function(msg) {
  return hienPopupThongBao(String(msg));
};

// Form có data-xac-nhan="..." → hỏi lại bằng popup nổi trước khi gửi (hủy đơn, xóa, đăng xuất...)
document.addEventListener('submit', async e => {
  const form = e.target;
  const cauHoi = form.dataset?.xacNhan || e.submitter?.dataset?.xacNhan;
  if (!cauHoi) return;

  if (form._daXacNhan) {
    delete form._daXacNhan;
    return;
  }

  e.preventDefault();
  e.stopImmediatePropagation();

  const submitter = e.submitter;
  const dongY = await hienPopupXacNhan({
    noiDung: cauHoi,
    tieuDe: form.dataset?.tieuDe || submitter?.dataset?.tieuDe,
    nutXacNhan: form.dataset?.nutXacNhan || submitter?.dataset?.nutXacNhan,
    kieu: form.dataset?.kieu || submitter?.dataset?.kieu,
    icon: form.dataset?.icon || submitter?.dataset?.icon
  });
  if (dongY) {
    form._daXacNhan = true;
    if (typeof form.requestSubmit === 'function') {
      try {
        if (submitter && submitter.form === form) {
          form.requestSubmit(submitter);
        } else {
          form.requestSubmit();
        }
      } catch {
        form.submit();
      }
    } else {
      form.submit();
    }
  }
});

// Thẻ link hoặc nút độc lập có data-xac-nhan="..."
document.addEventListener('click', async e => {
  const nut = e.target.closest('a[data-xac-nhan], button[type="button"][data-xac-nhan]');
  if (!nut) return;

  if (nut._daXacNhan) {
    delete nut._daXacNhan;
    return;
  }

  e.preventDefault();
  e.stopImmediatePropagation();

  const cauHoi = nut.dataset.xacNhan;
  const dongY = await hienPopupXacNhan({
    noiDung: cauHoi,
    tieuDe: nut.dataset?.tieuDe,
    nutXacNhan: nut.dataset?.nutXacNhan,
    kieu: nut.dataset?.kieu,
    icon: nut.dataset?.icon
  });
  if (dongY) {
    nut._daXacNhan = true;
    if (nut.tagName === 'A' && nut.href && !nut.getAttribute('href').startsWith('#') && !nut.getAttribute('href').startsWith('javascript:')) {
      if (nut.target === '_blank') {
        window.open(nut.href, '_blank');
      } else {
        window.location.href = nut.href;
      }
    } else {
      nut.click();
    }
  }
});


// Ô chọn ảnh (.o-tai-anh): hiện ảnh xem trước ngay khi chọn tệp
document.querySelectorAll('.o-tai-anh input[type="file"]').forEach(o => o.addEventListener('change', () => {
  const tep = o.files[0], khung = o.closest('.o-tai-anh');
  if (!tep || !khung) return;
  khung.querySelector('img').src = URL.createObjectURL(tep);
  khung.classList.add('co-anh');
}));

// Chống bấm gửi hai lần: form POST đã gửi đi (không bị hủy bởi hộp xác nhận / kiểm tra dữ liệu) thì khóa nút gửi
document.addEventListener('submit', e => {
  const form = e.target;
  if ((form.method || '').toLowerCase() !== 'post') return;
  setTimeout(() => {
    if (e.defaultPrevented) return;
    form.querySelectorAll('button:not([type="button"]), input[type="submit"]').forEach(b => {
      b.disabled = true;
      if (b.tagName === 'BUTTON' && !b.querySelector('.spinner-border')) b.insertAdjacentHTML('afterbegin', '<span class="spinner-border spinner-border-sm me-1"></span>');
    });
  }, 0);
});
// Quay lại trang bằng nút Back của trình duyệt: mở khóa các nút đã khóa
window.addEventListener('pageshow', e => {
  if (!e.persisted) return;
  document.querySelectorAll('button:disabled .spinner-border').forEach(s => { s.parentElement.disabled = false; s.remove(); });
});

// Nút ẩn / hiện mật khẩu trong ô nhập (.o-nhap .nut-hien-mk)
document.querySelectorAll('.nut-hien-mk').forEach(b => b.addEventListener('click', () => {
  const o = b.parentElement.querySelector('input'), hien = o.type === 'password';
  o.type = hien ? 'text' : 'password';
  b.innerHTML = `<i class="bi ${hien ? 'bi-eye-slash' : 'bi-eye'}"></i>`;
  b.setAttribute('aria-label', hien ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
}));

// Nút sao chép ([data-chep]): mã đơn, số tài khoản, nội dung chuyển khoản…
document.addEventListener('click', async e => {
  const b = e.target.closest('[data-chep]');
  if (!b) return;
  try {
    await navigator.clipboard.writeText(b.dataset.chep);
    const cu = b.innerHTML; b.innerHTML = '<i class="bi bi-check2"></i>'; setTimeout(() => b.innerHTML = cu, 1200);
  } catch { }
});

// Chuông thông báo: cập nhật số chưa đọc mỗi phút (thông báo mới phát sinh khi người khác thao tác trên đơn / ví)
(() => {
    const chuong = document.querySelector('.chuong[data-url-dem]');
    if (!chuong) return;
    const dem = chuong.querySelector('.chuong-dem');
    const capNhat = async () => {
        if (document.hidden) return;
        try {
            const r = await fetch(chuong.dataset.urlDem, { headers: { Accept: 'application/json' } });
            if (!r.ok) return;
            const { so } = await r.json();
            const truoc = dem.hidden ? 0 : parseInt(dem.textContent) || 0;
            dem.hidden = so === 0;
            dem.textContent = so > 99 ? '99+' : so;
            if (so > truoc) chuong.classList.add('rung'), setTimeout(() => chuong.classList.remove('rung'), 1200);
            // D7 – số đổi thì nạp lại danh sách trong menu chuông (không chỉ con số)
            if (so !== truoc && chuong.dataset.urlBang) {
                const h = await fetch(chuong.dataset.urlBang);
                if (h.ok) {
                    const moi = new DOMParser().parseFromString(await h.text(), 'text/html').querySelector('.bang-chuong');
                    const cu = chuong.querySelector('.bang-chuong');
                    if (moi && cu) cu.innerHTML = moi.innerHTML;
                }
            }
        } catch { /* mất mạng: thử lại lần sau */ }
    };
    setInterval(capNhat, 60000);
    document.addEventListener('visibilitychange', capNhat);
})();

// ==================== PWA SERVICE WORKER ====================
if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch(() => {});
  });
}

// ==================== WEB AUDIO SYNTHESIZER ====================
// Tạo âm thanh native không cần file MP3/WAV, hoạt động hoàn hảo 100% offline
let _audioCtx = null;
function layAudioContext() {
  if (!_audioCtx) {
    const AudioCtx = window.AudioContext || window.webkitAudioContext;
    if (AudioCtx) _audioCtx = new AudioCtx();
  }
  if (_audioCtx && _audioCtx.state === 'suspended') {
    _audioCtx.resume();
  }
  return _audioCtx;
}

// Tiếng "Bíp" khi quét mã thành công kèm rung phản hồi (haptic)
function phatAmBip() {
  try {
    if (navigator.vibrate) navigator.vibrate([60]);
    const ctx = layAudioContext();
    if (!ctx) return;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = 'sine';
    osc.frequency.setValueAtTime(1046.5, ctx.currentTime); // C6
    gain.gain.setValueAtTime(0.18, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.09);
    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.start();
    osc.stop(ctx.currentTime + 0.09);
  } catch (e) {}
}

// Tiếng "Ting" hai âm khi có thông báo mới hoặc hoàn thành tác vụ
function phatAmTing() {
  try {
    const ctx = layAudioContext();
    if (!ctx) return;
    const t0 = ctx.currentTime;
    
    // Nốt 1: D5 (587Hz)
    const osc1 = ctx.createOscillator();
    const gain1 = ctx.createGain();
    osc1.frequency.setValueAtTime(587.33, t0);
    gain1.gain.setValueAtTime(0.15, t0);
    gain1.gain.exponentialRampToValueAtTime(0.001, t0 + 0.18);
    osc1.connect(gain1);
    gain1.connect(ctx.destination);
    osc1.start(t0);
    osc1.stop(t0 + 0.18);
    
    // Nốt 2: A5 (880Hz)
    const osc2 = ctx.createOscillator();
    const gain2 = ctx.createGain();
    osc2.frequency.setValueAtTime(880, t0 + 0.09);
    gain2.gain.setValueAtTime(0.2, t0 + 0.09);
    gain2.gain.exponentialRampToValueAtTime(0.001, t0 + 0.35);
    osc2.connect(gain2);
    gain2.connect(ctx.destination);
    osc2.start(t0 + 0.09);
    osc2.stop(t0 + 0.35);
  } catch (e) {}
}

// ==================== FLOATING TOAST NOTIFICATIONS ====================
function hienToast(tieuDe, noiDung, kieu = 'thanh-cong', coAm = true) {
  let hopContainer = document.getElementById('hop-toast-container');
  if (!hopContainer) {
    hopContainer = document.createElement('div');
    hopContainer.id = 'hop-toast-container';
    document.body.appendChild(hopContainer);
  }

  const iconClass = {
    'thanh-cong': 'bi-check-lg',
    'canh-bao': 'bi-exclamation-triangle',
    'that-bai': 'bi-x-lg',
    'thong-tin': 'bi-info-lg'
  }[kieu] || 'bi-info-lg';

  const item = document.createElement('div');
  item.className = `toast-item toast-${kieu}`;
  item.innerHTML = `
    <div class="toast-icon"><i class="bi ${iconClass}"></i></div>
    <div class="toast-noi-dung">
      <div class="toast-tieu-de">${tieuDe}</div>
      ${noiDung ? `<div class="toast-chi-tiet">${noiDung}</div>` : ''}
    </div>
    <button type="button" class="toast-nut-dong" aria-label="Đóng"><i class="bi bi-x"></i></button>
  `;

  hopContainer.appendChild(item);
  if (coAm) phatAmTing();

  const dongToast = () => {
    item.classList.add('ra');
    setTimeout(() => item.remove(), 260);
  };

  item.querySelector('.toast-nut-dong').addEventListener('click', dongToast);
  setTimeout(dongToast, 2000);
}

// ==================== GIAO DIỆN SÁNG / TỐI (DARK MODE) ====================
function doiTheme() {
  const hienTai = document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
  const moi = hienTai === 'dark' ? 'light' : 'dark';
  document.documentElement.setAttribute('data-theme', moi);
  try {
    localStorage.setItem('gn_theme', moi);
  } catch (e) {}
  dongBoIconTheme();
  hienToast(
    moi === 'dark' ? 'Chế độ tối' : 'Chế độ sáng',
    moi === 'dark' ? 'Đã bật giao diện tối êm mắt' : 'Đã chuyển về giao diện sáng tiêu chuẩn',
    'thong-tin',
    false
  );
}

function dongBoIconTheme() {
  const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
  document.querySelectorAll('.nut-theme').forEach(nut => {
    nut.innerHTML = `<i class="bi ${isDark ? 'bi-sun-fill' : 'bi-moon-stars'}"></i>`;
    nut.setAttribute('title', isDark ? 'Chuyển sang chế độ Sáng' : 'Chuyển sang chế độ Tối');
  });
}

document.addEventListener('DOMContentLoaded', dongBoIconTheme);

// ==================== CHỌN NHIỀU DÒNG & BULK ACTIONS ====================
function khoiTaoBangNhieuThaoTac(bangId = '#bang-don-hang', thanhBarId = '#thanh-bulk-action') {
  const bang = document.querySelector(bangId);
  const thanhBar = document.querySelector(thanhBarId);
  if (!bang || !thanhBar) return;

  const chkAll = bang.querySelector('.chk-tat-ca');
  const chkRows = bang.querySelectorAll('.chk-dong');
  const txtSoLuong = thanhBar.querySelector('.so-luong-chon');

  const capNhatThanhBar = () => {
    const daChon = bang.querySelectorAll('.chk-dong:checked');
    const dem = daChon.length;
    if (txtSoLuong) txtSoLuong.textContent = dem;
    if (dem > 0) {
      thanhBar.style.display = 'flex';
      thanhBar.offsetHeight; // trigger layout reflow
      thanhBar.classList.add('hien');
    } else {
      thanhBar.classList.remove('hien');
      thanhBar.style.display = 'none';
      if (chkAll) chkAll.checked = false;
    }
  };

  // Luôn chạy ngay lúc khởi tạo: chưa chọn mục nào thì ẩn hoàn toàn
  capNhatThanhBar();

  if (chkAll) {
    chkAll.addEventListener('change', () => {
      chkRows.forEach(c => {
        if (!c.disabled) c.checked = chkAll.checked;
      });
      capNhatThanhBar();
    });
  }

  chkRows.forEach(c => {
    c.addEventListener('change', () => {
      if (!c.checked && chkAll) chkAll.checked = false;
      if (chkAll && chkRows.length > 0 && Array.from(chkRows).every(item => item.checked)) chkAll.checked = true;
      capNhatThanhBar();
    });
  });

  const nutBoChon = thanhBar.querySelector('.nut-bo-chon');
  if (nutBoChon) {
    nutBoChon.addEventListener('click', () => {
      chkRows.forEach(c => c.checked = false);
      if (chkAll) chkAll.checked = false;
      capNhatThanhBar();
    });
  }
}

// Tự động tìm và khởi tạo các bảng có bulk actions nếu trang có chứa
function tuDongKhoiTaoBang() {
  if (document.querySelector('#bang-don-hang') && document.querySelector('#thanh-bulk-action')) {
    khoiTaoBangNhieuThaoTac('#bang-don-hang', '#thanh-bulk-action');
  }
  if (document.querySelector('#bang-buu-cuc') && document.querySelector('#thanh-bulk-action-bc')) {
    khoiTaoBangNhieuThaoTac('#bang-buu-cuc', '#thanh-bulk-action-bc');
  }
}
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', tuDongKhoiTaoBang);
} else {
  tuDongKhoiTaoBang();
}

// Xuất file CSV hỗ trợ tiếng Việt chuẩn Unicode có BOM (\uFEFF) cho Excel
function xuatCsvBang(bangSelector, tenFile = 'danh-sach.csv', chiXuatDongChon = false) {
  const bang = document.querySelector(bangSelector);
  if (!bang) return;

  const dongDau = bang.querySelectorAll('thead tr th');
  let cacCot = [];
  let indexCot = [];
  dongDau.forEach((th, i) => {
    if (th.classList.contains('khong-xuat') || th.querySelector('.chk-tat-ca')) return;
    cacCot.push(`"${(th.textContent || '').trim().replace(/"/g, '""')}"`);
    indexCot.push(i);
  });

  let cacDong = [cacCot.join(',')];
  let trSelector = chiXuatDongChon ? 'tbody tr:has(.chk-dong:checked)' : 'tbody tr';
  const trElements = bang.querySelectorAll(trSelector);

  if (trElements.length === 0) {
    hienToast('Chưa chọn dữ liệu', 'Vui lòng chọn ít nhất một dòng để xuất Excel/CSV.', 'canh-bao');
    return;
  }

  trElements.forEach(tr => {
    const tds = tr.querySelectorAll('td');
    let hang = [];
    indexCot.forEach(idx => {
      const td = tds[idx];
      const noiDung = td ? (td.innerText || td.textContent || '').trim().replace(/\s+/g, ' ').replace(/"/g, '""') : '';
      hang.push(`"${noiDung}"`);
    });
    cacDong.push(hang.join(','));
  });

  const blob = new Blob(["\uFEFF" + cacDong.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = tenFile;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
  hienToast('Xuất tệp thành công', `Đã xuất ${cacDong.length - 1} dòng dữ liệu sang ${tenFile}`, 'thanh-cong');
}

// In nhãn vận đơn hàng loạt
function inDonHangDaChon(bangSelector = '#bang-don-hang') {
  const bang = document.querySelector(bangSelector);
  if (!bang) return;
  const daChon = Array.from(bang.querySelectorAll('.chk-dong:checked')).map(c => c.value);
  if (daChon.length === 0) {
    hienToast('Chưa chọn đơn', 'Vui lòng tích chọn các đơn hàng cần in nhãn.', 'canh-bao');
    return;
  }

  const w = window.open('', '_blank', 'width=800,height=600');
  let html = `<!DOCTYPE html><html><head><meta charset="utf-8"><title>In nhãn vận đơn hàng loạt</title>
  <style>
    body { font-family: "Segoe UI", Arial, sans-serif; padding: 20px; color: #111; }
    .nhan-don { border: 2px dashed #333; border-radius: 8px; padding: 16px; margin-bottom: 20px; page-break-inside: avoid; }
    .nhan-tieu-de { display: flex; justify-content: space-between; border-bottom: 2px solid #F26522; padding-bottom: 8px; margin-bottom: 12px; }
    .nhan-logo { font-size: 20px; font-weight: 800; color: #F26522; }
    .ma-don { font-size: 20px; font-weight: 800; }
    .khung-thong-tin { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-bottom: 10px; font-size: 14px; }
    @media print { .nut-in { display: none; } body { padding: 0; } }
  </style></head><body>
  <div style="text-align:right; margin-bottom: 16px;"><button class="nut-in" onclick="window.print()" style="padding: 8px 18px; background: #F26522; color: #fff; border: none; border-radius: 6px; font-weight: bold; cursor: pointer;">In tất cả (${daChon.length} nhãn)</button></div>
  `;

  daChon.forEach(ma => {
    html += `
      <div class="nhan-don">
        <div class="nhan-tieu-de">
          <div class="nhan-logo">GIAONHANH EXPRESS</div>
          <div class="ma-don">${ma}</div>
        </div>
        <div class="khung-thong-tin">
          <div><strong>Đơn hàng:</strong> #${ma}</div>
          <div><strong>Tiêu chuẩn:</strong> Chuyển phát tiêu chuẩn</div>
          <div><strong>Dịch vụ:</strong> UNETI Fast Express</div>
          <div><strong>Bưu cục phụ trách:</strong> Bưu cục tiếp nhận</div>
        </div>
        <div style="text-align: center; margin-top: 10px; font-family: monospace; font-size: 16px; letter-spacing: 4px; border-top: 1px solid #ccc; padding-top: 8px;">*${ma}*</div>
      </div>
    `;
  });

  html += `<script>window.onload = function() { setTimeout(function(){ window.print(); }, 400); };<\/script></body></html>`;
  w.document.write(html);
  w.document.close();
}




// ==================== POPUP CHÀO MỪNG / ĐĂNG XUẤT GÓC MÀN HÌNH ====================
function dongPopupChaoMung(btn) {
  const popup = (btn && btn.closest) ? btn.closest('.popup-chao-mung') : (btn || document.getElementById('popupChaoMungDangNhap') || document.getElementById('popupDangXuat'));
  if (!popup || popup.classList.contains('popup-bien-mat')) return;
  popup.classList.add('popup-bien-mat');
  setTimeout(() => {
    if (popup && popup.parentNode) {
      popup.parentNode.removeChild(popup);
    }
  }, 380);
}

function khoiTaoPopupChaoMung() {
  const popups = document.querySelectorAll('.popup-chao-mung');
  popups.forEach(popup => {
    // Đảm bảo popup luôn là con trực tiếp của document.body để không bị các thẻ cha có CSS transform làm lệch vị trí fixed
    if (popup.parentElement && popup.parentElement !== document.body) {
      document.body.appendChild(popup);
    }

    let timer = null;
    const thoiGian = 2000; // 2 giây tự ẩn theo yêu cầu

    const batDauDemNguoc = () => {
      timer = setTimeout(() => {
        dongPopupChaoMung(popup);
      }, thoiGian);
    };

    const dungDemNguoc = () => {
      if (timer) {
        clearTimeout(timer);
        timer = null;
      }
    };

    popup.addEventListener('mouseenter', dungDemNguoc);
    popup.addEventListener('mouseleave', batDauDemNguoc);

    // Tự động kích hoạt đếm ngược đóng
    batDauDemNguoc();
  });
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    khoiTaoPopupChaoMung();
    khoiTaoThongBaoBanner();
  });
} else {
  khoiTaoPopupChaoMung();
  khoiTaoThongBaoBanner();
}

// ==================== THÔNG BÁO FLASH HỆ THỐNG TỰ ĐỘNG ĐÓNG ====================
function dongThongBaoBanner(el) {
  if (!el || el.classList.contains('tb-bien-mat')) return;
  el.classList.add('tb-bien-mat');
  // Chờ hiệu ứng thu gọn chiều cao (0.4s) kết thúc mượt mà trước khi gỡ khỏi DOM (tránh bị giật giao diện)
  setTimeout(() => {
    try {
      if (typeof bootstrap !== 'undefined' && bootstrap.Alert) {
        const bsAlert = bootstrap.Alert.getInstance(el) || bootstrap.Alert.getOrCreateInstance(el);
        bsAlert.close();
      } else {
        el.remove();
      }
    } catch {
      el.remove();
    }
  }, 420);
}

function khoiTaoThongBaoBanner() {
  const banners = document.querySelectorAll('.thong-bao-banner');
  banners.forEach(banner => {
    let timer = null;
    const thoiGian = 2000; // 2 giây tự ẩn theo yêu cầu

    const batDau = () => {
      const bar = banner.querySelector('.tb-thanh-chay');
      if (bar) bar.style.animationPlayState = 'running';
      timer = setTimeout(() => {
        dongThongBaoBanner(banner);
      }, thoiGian);
    };

    const dung = () => {
      if (timer) {
        clearTimeout(timer);
        timer = null;
      }
      const bar = banner.querySelector('.tb-thanh-chay');
      if (bar) bar.style.animationPlayState = 'paused';
    };

    banner.addEventListener('mouseenter', dung);
    banner.addEventListener('mouseleave', () => {
      dung();
      const bar = banner.querySelector('.tb-thanh-chay');
      if (bar) bar.style.animationPlayState = 'running';
      timer = setTimeout(() => {
        dongThongBaoBanner(banner);
      }, 1000);
    });

    const nutDong = banner.querySelector('.tb-nut-dong');
    if (nutDong) {
      nutDong.addEventListener('click', (e) => {
        e.preventDefault();
        dung();
        dongThongBaoBanner(banner);
      });
    }

    batDau();
  });
}

