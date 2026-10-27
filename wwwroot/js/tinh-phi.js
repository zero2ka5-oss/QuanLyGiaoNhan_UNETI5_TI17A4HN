// Xem trước phí trên form tạo / sửa đơn: gọi /TrangChu/TinhPhi mỗi khi loại hàng, khu vực, bưu cục, khối lượng, kích thước thay đổi
let henGioTinhPhi;
function tinhPhiXemTruoc() {
  clearTimeout(henGioTinhPhi);
  henGioTinhPhi = setTimeout(async () => {
    const khung = document.getElementById('khungPhi');
    const loai = document.querySelector('input[name="MaLoaiHang"]:checked')?.value || document.querySelector('select#MaLoaiHang')?.value;
    const khuVuc = document.getElementById('MaKhuVuc')?.value;
    const kl = document.getElementById('KhoiLuong')?.value;
    const kt = document.getElementById('KichThuocM3')?.value || '0.005';
    const buuCucGui = document.getElementById('MaBuuCucGui')?.value || '';
    if (!khung) return;
    if (!loai || !(Number(khuVuc) > 0) || !kl || Number(kl) <= 0 || Number(kt) <= 0) {
      khung.innerHTML = '<div class="mo nho">Nhập thông tin hàng để xem phí.</div>';
      return;
    }
    // Sửa đơn: chưa đổi loại hàng / khu vực / khối lượng / kích thước / bưu cục thì server giữ nguyên phí đã báo → hiện đúng phí đó
    const luu = JSON.parse(document.querySelector('[data-phi-luu]')?.dataset.phiLuu || 'null');
    let p;
    if (luu && String(luu.loai) === String(loai) && String(luu.khuVuc) === String(khuVuc) && Number(luu.kl) === Number(kl) && (!luu.kt || Number(luu.kt) === Number(kt)) && (!luu.buuCucGui || String(luu.buuCucGui) === String(buuCucGui))) {
      p = luu;
    } else {
      // Đơn cũ đang giữ loại hàng / khu vực (có thể đã ngừng) → server vẫn cho tính phí như khi lưu
      const giu = luu ? `&giuLoai=${String(luu.loai) === String(loai)}&giuKhuVuc=${String(luu.khuVuc) === String(khuVuc)}` : '';
      const bcThamSo = buuCucGui ? `&maBuuCucGui=${buuCucGui}` : '';
      const res = await fetch(`/TrangChu/TinhPhi?maLoaiHang=${loai}&maKhuVuc=${khuVuc}&khoiLuong=${kl}&kichThuocM3=${kt}${bcThamSo}${giu}`);
      if (!res.ok) {
        const loi = await res.json().catch(() => ({}));
        khung.innerHTML = '<div class="text-danger nho"></div>';
        khung.firstChild.textContent = loi.thongBao || 'Không tính được phí với dữ liệu hiện tại.';
        return;
      }
      p = await res.json();
    }
    const thongTinCuLy = p.phanLoaiCuLy ? `<div class="dong nho text-primary mb-1 pb-1 border-bottom"><i class="bi bi-signpost-2"></i> <span>Tuyến: ${p.phanLoaiCuLy}</span></div>` : '';
    const thongTinDuKien = p.ngayGiaoDuKienHienThi ? `<div class="dong nho text-success mb-1 pb-1 border-bottom"><i class="bi bi-calendar-check"></i> <span>Dự kiến giao: <b>${p.ngayGiaoDuKienHienThi}</b> (${p.soNgayGiao} ngày)</span></div>` : '';
    const dongPhuPhiKichThuoc = (p.phuPhiKichThuoc && p.phuPhiKichThuoc > 0) ? `<div class="dong text-warning-emphasis"><span>Phụ phí cồng kềnh (${p.moTaKichThuoc || 'Kích thước lớn'})</span><span>+${dinhDangTien(p.phuPhiKichThuoc)}</span></div>` : '';
    khung.innerHTML =
      `<div class="bang-phi">
         ${thongTinCuLy}
         ${thongTinDuKien}
         <div class="dong"><span>Phí cơ bản</span><span>${dinhDangTien(p.phiCoBan)}</span></div>
         <div class="dong"><span>Phụ phí khối lượng</span><span>${dinhDangTien(p.phuPhiKhoiLuong)}</span></div>
         ${dongPhuPhiKichThuoc}
         <div class="dong"><span>Phụ phí loại hàng</span><span>${dinhDangTien(p.phuPhiLoaiHang)}</span></div>
         <div class="dong tong"><span>Phí vận chuyển</span><span>${dinhDangTien(p.tong)}</span></div>
       </div>`;

    // Thu hộ: số tiền shipper thu người nhận và số tiền người gửi nhận lại khi đối soát
    const thuHo = Math.max(0, Number(document.getElementById('TienThuHo')?.value) || 0);
    const nguoiNhanTra = document.querySelector('input[name="NguoiTraPhi"]:checked')?.value === 'NguoiNhan';
    const thuNguoiNhan = thuHo + (nguoiNhanTra ? p.tong : 0);
    const traNguoiGui = Math.max(0, thuHo - (nguoiNhanTra ? 0 : p.tong));
    const phiNguoiGui = nguoiNhanTra ? 0 : Math.max(0, p.tong - thuHo);   // phần phí không trừ được vào thu hộ
    khung.innerHTML +=
      `<div class="bang-phi mt-3 pt-3 border-top">
         <div class="dong"><span>Tiền thu hộ</span><span>${thuHo > 0 ? dinhDangTien(thuHo) : 'Không thu hộ'}</span></div>
         <div class="dong fw-bold"><span>Shipper thu người nhận</span><span class="text-cam">${dinhDangTien(thuNguoiNhan)}</span></div>
         ${thuHo > 0 ? `<div class="dong"><span>Người gửi nhận lại khi đối soát</span><span class="fw-semibold">${dinhDangTien(traNguoiGui)}</span></div>` : ''}
         ${phiNguoiGui > 0 ? `<div class="dong"><span>Người gửi thanh toán phí ship (khi đối soát)</span><span class="fw-semibold">${dinhDangTien(phiNguoiGui)}</span></div>` : ''}
       </div>`;
  }, 250);
}

// Khối ước lượng phí (_UocLuongPhi): bấm "Ước lượng phí" → gọi /TrangChu/TinhPhi → hiện chi tiết; phí thật được tính lại khi tạo đơn
(function khoiTaoUocLuong() {
  if (!document.getElementById('formUocLuong')) return;
  const ul = {
      form: document.getElementById('formUocLuong'), buuCucGui: document.getElementById('utBuuCucGui'),
      khuVuc: document.getElementById('utKhuVuc'), loai: document.getElementById('utLoaiHang'),
      kl: document.getElementById('utKhoiLuong'), kt: document.getElementById('utKichThuoc'), thuHo: document.getElementById('utThuHo'), loi: document.getElementById('utLoi'),
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
      const kt = Number(ul.kt?.value) || 0.005;
      if (!ul.khuVuc.value) return baoLoi(ul.khuVuc, 'Vui lòng chọn khu vực nhận hàng');
      if (!(kl > 0)) return baoLoi(ul.kl, 'Khối lượng phải lớn hơn 0');
      if (!(kt > 0)) return baoLoi(ul.kt, 'Kích thước phải lớn hơn 0 m³');
      if (toiDa > 0 && kl > toiDa) return baoLoi(ul.kl, `Khối lượng tối đa một đơn là ${toiDa.toLocaleString('vi-VN')} kg – vui lòng chia thành nhiều đơn`);

      ul.nut.disabled = true; ul.nut.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang tính…';
      try {
          const bcThamSo = ul.buuCucGui && ul.buuCucGui.value ? `&maBuuCucGui=${ul.buuCucGui.value}` : '';
          const res = await fetch(`/TrangChu/TinhPhi?maLoaiHang=${ul.loai.value}&maKhuVuc=${ul.khuVuc.value}&khoiLuong=${kl}&kichThuocM3=${kt}${bcThamSo}`);
          const p = await res.json();
          if (!res.ok) return baoLoi(null, p.thongBao || 'Không tính được phí với dữ liệu đã nhập');

          const thuHo = soThuHo(), nguoiNhanTra = ul.form.querySelector('[name="utNguoiTra"]:checked').value === 'NguoiNhan';
          const thuNguoiNhan = thuHo + (nguoiNhanTra ? p.tong : 0), traNguoiGui = thuHo - (nguoiNhanTra ? 0 : p.tong);
          const $ = id => document.getElementById(id);
          $('utTong').textContent = dinhDangTien(p.tong);

          const tenBcGui = ul.buuCucGui && ul.buuCucGui.value ? ul.buuCucGui.selectedOptions[0].text.split(' (')[0] : 'Bưu cục gửi';
          const tenKhuVucNhan = ul.khuVuc.selectedOptions[0].text;
          $('utTomTat').textContent = `${tenBcGui} → ${tenKhuVucNhan} · ${ul.loai.selectedOptions[0].text} · ${kl.toLocaleString('vi-VN')} kg · ${kt.toLocaleString('vi-VN')} m³`;

          if (p.khoangCachKm) {
              const dongCuLy = $('utDongCuLy');
              if (dongCuLy) {
                  dongCuLy.classList.remove('d-none');
                  $('utCuLy').textContent = `${p.phanLoaiCuLy || (p.khoangCachKm + ' km')}`;
              }
          } else {
              const dongCuLy = $('utDongCuLy');
              if (dongCuLy) dongCuLy.classList.add('d-none');
          }

          if (p.ngayGiaoDuKienHienThi) {
              const dongDuKien = $('utDongDuKien');
              if (dongDuKien) {
                  dongDuKien.classList.remove('d-none');
                  $('utDuKien').textContent = `${p.ngayGiaoDuKienHienThi} (~${p.soNgayGiao} ngày)`;
              }
          }

          $('utCoBan').textContent = dinhDangTien(p.phiCoBan);
          $('utNhanKl').textContent = p.khoiLuongVuot > 0 ? `Phụ phí khối lượng (vượt ${p.khoiLuongVuot.toLocaleString('vi-VN')} kg)` : 'Phụ phí khối lượng';
          $('utPhuKl').textContent = dinhDangTien(p.phuPhiKhoiLuong);

          const dongPhuKt = $('utDongPhuKt');
          if (dongPhuKt) {
              if (p.phuPhiKichThuoc && p.phuPhiKichThuoc > 0) {
                  dongPhuKt.classList.remove('d-none');
                  $('utNhanKt').textContent = `Phụ phí cồng kềnh (${p.moTaKichThuoc || 'Kích thước lớn'})`;
                  $('utPhuKt').textContent = dinhDangTien(p.phuPhiKichThuoc);
              } else {
                  dongPhuKt.classList.add('d-none');
              }
          }

          $('utNhanLoai').textContent = p.heSoPhuThu > 0 ? `Phụ phí loại hàng (+${Math.round(p.heSoPhuThu * 100)}%)` : 'Phụ phí loại hàng';
          $('utPhuLoai').textContent = dinhDangTien(p.phuPhiLoaiHang);
          $('utNguoiNhan').textContent = dinhDangTien(thuNguoiNhan);
          $('utNhanNguoiGui').textContent = traNguoiGui >= 0 ? 'Người gửi nhận lại khi đối soát' : 'Người gửi thanh toán phí ship';
          $('utNguoiGui').textContent = dinhDangTien(Math.abs(traNguoiGui));
          $('utKhoiThu').classList.toggle('d-none', !(thuHo > 0 || nguoiNhanTra));

          const qObj = { maKhuVuc: ul.khuVuc.value, maLoaiHang: ul.loai.value, khoiLuong: kl, kichThuocM3: kt, tienThuHo: thuHo, nguoiTraPhi: nguoiNhanTra ? 'NguoiNhan' : 'NguoiGui' };
          if (ul.buuCucGui && ul.buuCucGui.value) qObj.maBuuCucGui = ul.buuCucGui.value;
          const q = new URLSearchParams(qObj);
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

