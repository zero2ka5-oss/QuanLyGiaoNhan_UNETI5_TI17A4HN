// Họ và tên: Nguyễn Minh Hướng – MSSV: 23103100268
// Nội dung thực hiện: Module 5 - Tính phí phía trình duyệt: xem trước phí trên form tạo / sửa đơn (tinhPhiXemTruoc)
//                     và khối "Ước lượng phí" (_UocLuongPhi). Gọi /TrangChu/TinhPhi; phí chỉ để xem trước –
//                     khi lưu đơn, server luôn tính lại (DonHangXuLy.ApDungPhi). Cần site.js (hàm dinhDangTien) nạp trước.

// Xem trước phí trên form tạo / sửa đơn: gọi /TrangChu/TinhPhi mỗi khi loại hàng, khu vực, khối lượng thay đổi
let henGioTinhPhi;
function tinhPhiXemTruoc() {
  clearTimeout(henGioTinhPhi);
  henGioTinhPhi = setTimeout(async () => {
    const khung = document.getElementById('khungPhi');
    const loai = document.querySelector('input[name="MaLoaiHang"]:checked')?.value || document.querySelector('select#MaLoaiHang')?.value;
    const khuVuc = document.getElementById('MaKhuVuc')?.value;
    const kl = document.getElementById('KhoiLuong')?.value;
    if (!khung) return;
    if (!loai || !(Number(khuVuc) > 0) || !kl || Number(kl) <= 0) {
      khung.innerHTML = '<div class="mo nho">Nhập thông tin hàng để xem phí.</div>';
      return;
    }
    const res = await fetch(`/TrangChu/TinhPhi?maLoaiHang=${loai}&maKhuVuc=${khuVuc}&khoiLuong=${kl}`);
    if (!res.ok) {
      const loi = await res.json().catch(() => ({}));
      khung.innerHTML = '<div class="text-danger nho"></div>';
      khung.firstChild.textContent = loi.thongBao || 'Không tính được phí với dữ liệu hiện tại.';
      return;
    }
    const p = await res.json();
    khung.innerHTML =
      `<div class="bang-phi">
         <div class="dong"><span>Phí cơ bản</span><span>${dinhDangTien(p.phiCoBan)}</span></div>
         <div class="dong"><span>Phụ phí khối lượng</span><span>${dinhDangTien(p.phuPhiKhoiLuong)}</span></div>
         <div class="dong"><span>Phụ phí loại hàng</span><span>${dinhDangTien(p.phuPhiLoaiHang)}</span></div>
         <div class="dong tong"><span>Phí vận chuyển</span><span>${dinhDangTien(p.tong)}</span></div>
       </div>`;

    // Thu hộ: số tiền shipper thu người nhận và số tiền người gửi nhận lại khi đối soát
    const thuHo = Math.max(0, Number(document.getElementById('TienThuHo')?.value) || 0);
    const nguoiNhanTra = document.querySelector('input[name="NguoiTraPhi"]:checked')?.value === 'NguoiNhan';
    const thuNguoiNhan = thuHo + (nguoiNhanTra ? p.tong : 0);
    const traNguoiGui = thuHo - (nguoiNhanTra ? 0 : p.tong);
    khung.innerHTML +=
      `<div class="bang-phi mt-3 pt-3 border-top">
         <div class="dong"><span>Tiền thu hộ</span><span>${thuHo > 0 ? dinhDangTien(thuHo) : 'Không thu hộ'}</span></div>
         <div class="dong fw-bold"><span>Shipper thu người nhận</span><span class="text-cam">${dinhDangTien(thuNguoiNhan)}</span></div>
         ${thuHo > 0 || nguoiNhanTra
           ? `<div class="dong"><span>${traNguoiGui >= 0 ? 'Bạn nhận lại khi đối soát' : 'Bạn thanh toán phí ship'}</span><span class="fw-semibold">${dinhDangTien(Math.abs(traNguoiGui))}</span></div>`
           : ''}
       </div>`;
  }, 250);
}

// Khối ước lượng phí (_UocLuongPhi): bấm "Ước lượng phí" → gọi /TrangChu/TinhPhi → hiện chi tiết; phí thật được tính lại khi tạo đơn
(function khoiTaoUocLuong() {
  if (!document.getElementById('formUocLuong')) return;
  const ul = {
      form: document.getElementById('formUocLuong'), khuVuc: document.getElementById('utKhuVuc'), loai: document.getElementById('utLoaiHang'),
      kl: document.getElementById('utKhoiLuong'), thuHo: document.getElementById('utThuHo'), loi: document.getElementById('utLoi'),
      nut: document.getElementById('nutUocLuong'), cho: document.getElementById('utCho'), chiTiet: document.getElementById('utChiTiet')
  };
  const daDangNhapKhach = ul.form.dataset.khach === '1';
  const soThuHo = () => Number(ul.thuHo.value.replace(/\D/g, '')) || 0;
  const baoLoi = (oNhap, chu) => { ul.loi.textContent = chu; ul.loi.classList.add('hien'); oNhap?.classList.add('is-invalid'); oNhap?.focus(); };

  // Định dạng tiền thu hộ khi gõ: 1500000 → 1.500.000
  ul.thuHo.addEventListener('input', () => { const so = soThuHo(); ul.thuHo.value = so ? so.toLocaleString('vi-VN') : ''; });
  // Đổi dữ liệu sau khi đã ước lượng → làm mờ kết quả cũ để bấm lại
  ul.form.addEventListener('input', () => { ul.loi.classList.remove('hien'); ul.form.querySelectorAll('.is-invalid').forEach(o => o.classList.remove('is-invalid')); ul.chiTiet.classList.add('cu'); });

  ul.form.addEventListener('submit', async e => {
      e.preventDefault();
      const kl = Number(ul.kl.value), toiDa = Number(ul.kl.max);
      if (!ul.khuVuc.value) return baoLoi(ul.khuVuc, 'Vui lòng chọn khu vực giao');
      if (!(kl > 0)) return baoLoi(ul.kl, 'Khối lượng phải lớn hơn 0');
      if (toiDa > 0 && kl > toiDa) return baoLoi(ul.kl, `Khối lượng tối đa một đơn là ${toiDa.toLocaleString('vi-VN')} kg – vui lòng chia thành nhiều đơn`);

      ul.nut.disabled = true; ul.nut.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang tính…';
      try {
          const res = await fetch(`/TrangChu/TinhPhi?maLoaiHang=${ul.loai.value}&maKhuVuc=${ul.khuVuc.value}&khoiLuong=${kl}`);
          const p = await res.json();
          if (!res.ok) return baoLoi(null, p.thongBao || 'Không tính được phí với dữ liệu đã nhập');

          const thuHo = soThuHo(), nguoiNhanTra = ul.form.querySelector('[name="utNguoiTra"]:checked').value === 'NguoiNhan';
          const thuNguoiNhan = thuHo + (nguoiNhanTra ? p.tong : 0), traNguoiGui = thuHo - (nguoiNhanTra ? 0 : p.tong);
          const $ = id => document.getElementById(id);
          $('utTong').textContent = dinhDangTien(p.tong);
          $('utTomTat').textContent = `${ul.khuVuc.selectedOptions[0].text} · ${ul.loai.selectedOptions[0].text} · ${kl.toLocaleString('vi-VN')} kg`;
          $('utCoBan').textContent = dinhDangTien(p.phiCoBan);
          $('utNhanKl').textContent = p.khoiLuongVuot > 0 ? `Phụ phí khối lượng (vượt ${p.khoiLuongVuot.toLocaleString('vi-VN')} kg)` : 'Phụ phí khối lượng';
          $('utPhuKl').textContent = dinhDangTien(p.phuPhiKhoiLuong);
          $('utNhanLoai').textContent = p.heSoPhuThu > 0 ? `Phụ phí loại hàng (+${Math.round(p.heSoPhuThu * 100)}%)` : 'Phụ phí loại hàng';
          $('utPhuLoai').textContent = dinhDangTien(p.phuPhiLoaiHang);
          $('utNguoiNhan').textContent = dinhDangTien(thuNguoiNhan);
          $('utNhanNguoiGui').textContent = traNguoiGui >= 0 ? 'Người gửi nhận lại khi đối soát' : 'Người gửi thanh toán phí ship';
          $('utNguoiGui').textContent = dinhDangTien(Math.abs(traNguoiGui));
          $('utKhoiThu').classList.toggle('d-none', !(thuHo > 0 || nguoiNhanTra));

          const q = new URLSearchParams({ maKhuVuc: ul.khuVuc.value, maLoaiHang: ul.loai.value, khoiLuong: kl, tienThuHo: thuHo, nguoiTraPhi: nguoiNhanTra ? 'NguoiNhan' : 'NguoiGui' });
          $('utTaoDon').href = daDangNhapKhach ? '/KhachHangDonHang/TaoMoi?' + q : '/DangNhap/DangKy';
          $('utTaoDon').innerHTML = daDangNhapKhach ? '<i class="bi bi-plus-circle"></i> Tạo đơn với thông tin này' : '<i class="bi bi-person-plus"></i> Đăng ký để tạo đơn';

          ul.cho.classList.add('d-none');
          ul.chiTiet.classList.remove('d-none', 'cu');
          ul.chiTiet.classList.remove('vua-tinh'); void ul.chiTiet.offsetWidth; ul.chiTiet.classList.add('vua-tinh');
          if (innerWidth < 992) document.getElementById('utKetQua').scrollIntoView({ behavior: 'smooth', block: 'start' });
      } catch { baoLoi(null, 'Không kết nối được máy chủ, vui lòng thử lại'); }
      finally { ul.nut.disabled = false; ul.nut.innerHTML = '<i class="bi bi-calculator"></i> Ước lượng phí'; }
  });
})();
