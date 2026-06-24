using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;

namespace QuanLyKhachSan.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public BookingController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ==========================================
        // 1. ĐẶT PHÒNG TẠI QUẦY (LỄ TÂN / ADMIN)
        // ==========================================
        [Authorize(Roles = "admin,nhanvien")]
        [HttpGet]
        public async Task<IActionResult> Create(string? room)
        {
            ViewBag.SelectedRoom = room;
            
            // Danh sách khách hàng và phòng trống để chọn
            ViewBag.Customers = await _context.KhachHangs
                .OrderByDescending(k => k.MaKH)
                .ToListAsync();

            ViewBag.Rooms = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai == "Trống")
                .ToListAsync();

            return View();
        }

        [Authorize(Roles = "admin,nhanvien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int makh, string maphong, DateTime ngayvao, DateTime ngayra)
        {
            if (makh <= 0 || string.IsNullOrEmpty(maphong) || ngayvao == default || ngayra == default)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ các trường dữ liệu bắt buộc!";
                return RedirectToAction("Create", new { room = maphong });
            }

            if (ngayra <= ngayvao)
            {
                TempData["ErrorMessage"] = "Thời gian check-out phải sau thời gian check-in!";
                return RedirectToAction("Create", new { room = maphong });
            }

            // Kiểm tra trùng lịch đặt phòng
            var isConflict = await _context.DatPhongs
                .AnyAsync(dp => dp.MaPhong == maphong && 
                                 dp.TrangThai != "Đã huỷ" && dp.TrangThai != "Đã thanh toán" && dp.TrangThai != "Đã thanh toán (Online)" &&
                                 dp.NgayCheckIn < ngayra && dp.NgayCheckOut > ngayvao);

            if (isConflict)
            {
                TempData["ErrorMessage"] = "Phòng này đã có lịch đặt trong khoảng thời gian đã chọn!";
                return RedirectToAction("Create", new { room = maphong });
            }

            var datPhong = new DatPhong
            {
                MaKH = makh,
                MaPhong = maphong,
                NgayCheckIn = ngayvao,
                NgayCheckOut = ngayra,
                TrangThai = "Đã xác nhận", // Đặt trực tiếp tại quầy sẽ được tự động xác nhận
                NguonDat = "TaiQuay"
            };

            _context.DatPhongs.Add(datPhong);
            await _context.SaveChangesAsync();

            // Gửi email xác nhận đặt phòng (nếu khách hàng có email)
            var kh = await _context.KhachHangs.FindAsync(makh);
            var phong = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.MaPhong == maphong);
            
            if (kh != null && phong != null && !string.IsNullOrEmpty(kh.Email))
            {
                int days = Math.Max(1, (ngayra - ngayvao).Days);
                decimal originalPrice = days * phong.LoaiPhong.GiaPhong;
                decimal total = originalPrice;

                // Gửi email background
                _ = Task.Run(() => _emailService.SendBookingConfirmationAsync(
                    kh.Email, kh.HoTen, datPhong.MaDP, phong.LoaiPhong.TenLoai + $" ({phong.MaPhong})",
                    ngayvao, ngayra, days, originalPrice, 0, "", "Tại quầy", total));
            }

            TempData["SuccessMessage"] = "Tạo phiếu đặt phòng thành công!";
            return RedirectToAction("RoomMap", "Admin");
        }

        // Tạo nhanh khách hàng mới tại quầy
        [Authorize(Roles = "admin,nhanvien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCustomerQuick(string hoten, string cccd, string sdt, string email)
        {
            if (string.IsNullOrWhiteSpace(hoten) || string.IsNullOrWhiteSpace(cccd) || string.IsNullOrWhiteSpace(sdt))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin khách hàng!";
                return RedirectToAction("Create");
            }

            // Kiểm tra trùng
            var isCccdExists = await _context.KhachHangs.AnyAsync(k => k.CCCD == cccd);
            if (isCccdExists)
            {
                TempData["ErrorMessage"] = "Số CCCD này đã tồn tại trong hệ thống!";
                return RedirectToAction("Create");
            }

            var kh = new KhachHang
            {
                HoTen = hoten.Trim(),
                CCCD = cccd.Trim(),
                SDT = sdt.Trim(),
                Email = email?.Trim(),
                HangThanhVien = "Đồng",
                DiemTichLuy = 0
            };

            _context.KhachHangs.Add(kh);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã tạo nhanh khách hàng {kh.HoTen}. Bạn có thể chọn ở ô Khách Hàng bên dưới.";
            return RedirectToAction("Create", new { room = Request.Form["maphong"].ToString() });
        }

        // ==========================================
        // 2. KHÁCH ĐẶT PHÒNG TRỰC TUYẾN (CUSTOMER ONLINE)
        // ==========================================
        [Authorize(Roles = "khach")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookOnline(string maphong, DateTime ngayvao, DateTime ngayra, string ghichu, string voucher_code, string payment, int so_nguoi)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var room = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(p => p.MaPhong == maphong);

            if (room == null)
            {
                return NotFound();
            }

            if (ngayra <= ngayvao)
            {
                TempData["ErrorMessage"] = "Thời gian check-out phải sau thời gian check-in!";
                return RedirectToAction("Detail", "Home", new { id = room.MaLoai });
            }

            if (so_nguoi > room.LoaiPhong.SoNguoiToiDa)
            {
                TempData["ErrorMessage"] = $"Phòng {room.MaPhong} chỉ chứa tối đa {room.LoaiPhong.SoNguoiToiDa} người!";
                return RedirectToAction("Detail", "Home", new { id = room.MaLoai });
            }

            // Lấy Khách Hàng từ tài khoản
            var kh = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == userId);
            if (kh == null)
            {
                // Nếu chưa có khách hàng, tạo một bản ghi tạm thời
                kh = new KhachHang
                {
                    MaTK = userId,
                    HoTen = User.Identity?.Name ?? "Khách Hàng Mới",
                    CCCD = "TEMP_" + DateTime.Now.Ticks.ToString().Substring(10),
                    SDT = "Chưa cập nhật",
                    Email = User.FindFirst("Username")?.Value + "@gmail.com" // Tạm thời
                };
                _context.KhachHangs.Add(kh);
                await _context.SaveChangesAsync();
            }

            // Kiểm tra xem khách hàng có bị khóa buộc thanh toán online hay không
            // Giả sử có thuộc tính hoặc logic tương ứng (BuocThanhToanTruoc). 
            // Chúng ta có thể kiểm tra xem khách có cột BuocThanhToanTruoc không.
            // Để tương thích với schema cơ sở dữ liệu hiện tại, ta có thể dùng EF hoặc check giá trị trong DB.
            // Cột này được thêm vào bảng KhachHang qua script khác. Hãy viết câu query kiểm tra.
            bool buocThanhToanTruoc = false;
            try
            {
                // Thực hiện raw SQL check cột BuocThanhToanTruoc nếu cần
                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT BuocThanhToanTruoc FROM KhachHang WHERE MaKH = {kh.MaKH}";
                var val = await cmd.ExecuteScalarAsync();
                if (val != DBNull.Value && val != null)
                {
                    buocThanhToanTruoc = Convert.ToBoolean(val);
                }
            }
            catch { }

            if (buocThanhToanTruoc && payment == "Tiền mặt")
            {
                TempData["ErrorMessage"] = "Tài khoản của bạn bắt buộc phải thanh toán Online. Vui lòng chọn thanh toán Online!";
                return RedirectToAction("Detail", "Home", new { id = room.MaLoai });
            }

            // Kiểm tra trùng lịch đặt
            var isConflict = await _context.DatPhongs
                .AnyAsync(dp => dp.MaPhong == maphong && 
                                 dp.TrangThai != "Đã huỷ" && dp.TrangThai != "Đã thanh toán" && dp.TrangThai != "Đã thanh toán (Online)" &&
                                 dp.NgayCheckIn < ngayra && dp.NgayCheckOut > ngayvao);

            if (isConflict)
            {
                TempData["ErrorMessage"] = "Phòng này đã có lịch đặt trong khoảng thời gian đã chọn! Vui lòng chọn ngày khác hoặc phòng khác.";
                return RedirectToAction("Detail", "Home", new { id = room.MaLoai });
            }

            // Xử lý giảm giá từ Voucher
            decimal discount = 0;
            string voucherName = "";
            Voucher? appliedVoucher = null;

            if (!string.IsNullOrEmpty(voucher_code))
            {
                var today = DateTime.Today;
                appliedVoucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == voucher_code.ToUpper() && 
                                              v.TrangThai == "active" && 
                                              v.NgayHetHan >= today && 
                                              (v.MaKH == null || v.MaKH == kh.MaKH) && 
                                              v.SoLanDaDung < v.GioiHanDung);

                if (appliedVoucher != null)
                {
                    int days = Math.Max(1, (ngayra - ngayvao).Days);
                    decimal originalPrice = days * room.LoaiPhong.GiaPhong;

                    if (originalPrice >= appliedVoucher.GiaTriToiThieu)
                    {
                        if (appliedVoucher.LoaiGiam == "phantram")
                        {
                            discount = originalPrice * (appliedVoucher.GiaTriGiam / 100m);
                        }
                        else
                        {
                            discount = appliedVoucher.GiaTriGiam;
                        }
                        // Không cho phép giảm quá giá trị phòng
                        discount = Math.Min(discount, originalPrice);
                        voucherName = appliedVoucher.TenVoucher;
                    }
                }
            }

            // Tạo phiếu đặt phòng
            string note = $"{ghichu} [Phương thức: {payment}]";
            string status = payment == "Online" ? "Đã thanh toán (Online)" : "Chờ xác nhận";

            var datPhong = new DatPhong
            {
                MaKH = kh.MaKH,
                MaPhong = maphong,
                NgayCheckIn = ngayvao,
                NgayCheckOut = ngayra,
                GhiChu = note,
                TrangThai = status,
                NguonDat = "BanOnline"
            };

            _context.DatPhongs.Add(datPhong);

            // Cập nhật số lần dùng của voucher nếu áp dụng thành công
            if (appliedVoucher != null && discount > 0)
            {
                appliedVoucher.SoLanDaDung++;
                _context.Vouchers.Update(appliedVoucher);
            }

            await _context.SaveChangesAsync();

            // Gửi email xác nhận đặt phòng
            if (!string.IsNullOrEmpty(kh.Email))
            {
                int days = Math.Max(1, (ngayra - ngayvao).Days);
                decimal originalPrice = days * room.LoaiPhong.GiaPhong;
                decimal total = originalPrice - discount;

                _ = Task.Run(() => _emailService.SendBookingConfirmationAsync(
                    kh.Email, kh.HoTen, datPhong.MaDP, room.LoaiPhong.TenLoai + $" ({room.MaPhong})",
                    ngayvao, ngayra, days, originalPrice, discount, voucherName, payment, total));
            }

            TempData["SuccessMessage"] = "Đặt phòng trực tuyến thành công! Mã đơn của bạn là #" + datPhong.MaDP;
            
            // Chuyển hướng tới Lịch sử đặt phòng của khách hàng
            return RedirectToAction("BookingHistory");
        }

        [Authorize(Roles = "khach")]
        [HttpGet]
        public async Task<IActionResult> BookingHistory()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var kh = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == userId);
            if (kh == null)
            {
                return View(new List<DatPhong>());
            }

            var history = await _context.DatPhongs
                .Include(dp => dp.Phong)
                .ThenInclude(p => p.LoaiPhong)
                .Where(dp => dp.MaKH == kh.MaKH)
                .OrderByDescending(dp => dp.MaDP)
                .ToListAsync();

            return View(history);
        }
    }
}
