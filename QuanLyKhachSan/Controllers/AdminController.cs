using System;
using System.Collections.Generic;
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
    [Authorize(Roles = "admin,nhanvien")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AdminController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ==========================================
        // 1. SƠ ĐỒ PHÒNG (ROOM MAP)
        // ==========================================
        public async Task<IActionResult> RoomMap()
        {
            var now = DateTime.Now;

            // Lấy tất cả các phòng và thông tin loại phòng
            var rooms = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .OrderBy(p => p.LoaiPhong.KhuVuc)
                .ThenBy(p => p.MaPhong)
                .ToListAsync();

            // Lấy các đơn đặt phòng active (đang chờ nhận, đã xác nhận, đang ở, đã thanh toán online) để phục vụ hiển thị
            var activeBookings = await _context.DatPhongs
                .Include(dp => dp.KhachHang)
                .Where(dp => dp.TrangThai == "Chờ xác nhận" || dp.TrangThai == "Đã xác nhận" || dp.TrangThai == "Đang ở" || dp.TrangThai == "Đã thanh toán (Online)")
                .ToListAsync();

            var roomMapList = new List<RoomMapItemViewModel>();

            foreach (var r in rooms)
            {
                // Kiểm tra xem phòng có booking nào chờ checkin ngay lúc này không (so sánh phần ngày)
                var pendingBooking = activeBookings
                    .Where(dp => dp.MaPhong == r.MaPhong && 
                                 (dp.TrangThai == "Đã xác nhận" || dp.TrangThai == "Đã thanh toán (Online)" || dp.TrangThai == "Chờ xác nhận") &&
                                 dp.NgayCheckIn.Date <= now.Date)
                    .OrderBy(dp => dp.NgayCheckIn)
                    .FirstOrDefault();

                // Lấy đơn đặt phòng hiện tại đang ở (nếu có)
                var currentBooking = activeBookings
                    .FirstOrDefault(dp => dp.MaPhong == r.MaPhong && dp.TrangThai == "Đang ở");

                roomMapList.Add(new RoomMapItemViewModel
                {
                    Room = r,
                    PendingCheckInBooking = pendingBooking,
                    CurrentBooking = currentBooking,
                    // Checkout date to display
                    NgayCheckOut = currentBooking?.NgayCheckOut ?? pendingBooking?.NgayCheckOut
                });
            }

            // Phân nhóm phòng theo khu vực
            var roomsByRegion = roomMapList
                .GroupBy(x => x.Room.LoaiPhong.KhuVuc)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Thống kê số lượng
            ViewBag.TotalCount = rooms.Count;
            ViewBag.VacantCount = rooms.Count(r => r.TrangThai == "Trống");
            ViewBag.OccupiedCount = rooms.Count(r => r.TrangThai == "Đang ở");
            ViewBag.CleaningCount = rooms.Count(r => r.TrangThai == "Đang dọn dẹp");

            return View(roomsByRegion);
        }

        // Bấm nhận phòng nhanh (Check-in)
        public async Task<IActionResult> CheckinRoom(string id)
        {
            var now = DateTime.Now;
            // Tìm đơn đặt phòng hợp lệ (chờ nhận/đã xác nhận/đang chờ) (so sánh phần ngày)
            var booking = await _context.DatPhongs
                .Where(dp => dp.MaPhong == id && 
                             (dp.TrangThai == "Đã xác nhận" || dp.TrangThai == "Đã thanh toán (Online)" || dp.TrangThai == "Chờ xác nhận") &&
                             dp.NgayCheckIn.Date <= now.Date)
                .OrderBy(dp => dp.NgayCheckIn)
                .FirstOrDefaultAsync();

            if (booking != null)
            {
                booking.TrangThai = "Đang ở";
                _context.DatPhongs.Update(booking);

                // Cập nhật trạng thái phòng sang 'Đang ở'
                var room = await _context.Phongs.FindAsync(id);
                if (room != null)
                {
                    room.TrangThai = "Đang ở";
                    _context.Phongs.Update(room);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã Check-in phòng {id} thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Không tìm thấy đơn đặt phòng chờ nhận cho phòng {id}!";
            }

            return RedirectToAction("RoomMap");
        }

        // Cập nhật trạng thái dọn dẹp hoặc huỷ phòng khẩn cấp
        public async Task<IActionResult> UpdateRoomStatus(string id, string status)
        {
            var room = await _context.Phongs.FindAsync(id);
            if (room != null)
            {
                if (status == "Trống")
                {
                    // Huỷ khẩn cấp: cập nhật đơn đặt phòng đang active và trả phòng về Trống
                    var activeBooking = await _context.DatPhongs
                        .Where(dp => dp.MaPhong == id && (dp.TrangThai == "Đang ở" || dp.TrangThai == "Đã xác nhận" || dp.TrangThai == "Chờ xác nhận"))
                        .OrderByDescending(dp => dp.MaDP)
                        .FirstOrDefaultAsync();

                    if (activeBooking != null)
                    {
                        activeBooking.TrangThai = "Đã huỷ";
                        _context.DatPhongs.Update(activeBooking);
                    }
                    room.TrangThai = "Trống";
                }
                else if (status == "Đang dọn dẹp" || status == "Trống")
                {
                    room.TrangThai = status;
                }
                
                _context.Phongs.Update(room);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật trạng thái phòng {id} sang {status} thành công!";
            }

            return RedirectToAction("RoomMap");
        }

        // ==========================================
        // 2. QUẢN LÝ SỬ DỤNG DỊCH VỤ (ADD SERVICE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> AddService(int bookingId)
        {
            var booking = await _context.DatPhongs
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.Phong)
                .FirstOrDefaultAsync(dp => dp.MaDP == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            ViewBag.Services = await _context.DichVus.OrderBy(s => s.TenDV).ToListAsync();
            
            // Các dịch vụ đã sử dụng trong phòng
            ViewBag.UsedServices = await _context.SuDungDichVus
                .Include(sd => sd.DichVu)
                .Where(sd => sd.MaDP == bookingId)
                .OrderByDescending(sd => sd.ThoiGian)
                .ToListAsync();

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService(int bookingId, int serviceId, int quantity)
        {
            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "Số lượng phải lớn hơn 0!";
                return RedirectToAction("AddService", new { bookingId });
            }

            var booking = await _context.DatPhongs.FindAsync(bookingId);
            var service = await _context.DichVus.FindAsync(serviceId);

            if (booking == null || service == null)
            {
                return NotFound();
            }

            var suDungDV = new SuDungDichVu
            {
                MaDP = bookingId,
                MaDV = serviceId,
                SoLuong = quantity,
                ThanhTien = quantity * service.GiaDV,
                ThoiGian = DateTime.Now
            };

            _context.SuDungDichVus.Add(suDungDV);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm dịch vụ {service.TenDV} thành công!";
            return RedirectToAction("AddService", new { bookingId });
        }

        // Xoá dịch vụ đã thêm nhầm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveService(int id)
        {
            var sd = await _context.SuDungDichVus.FindAsync(id);
            if (sd != null)
            {
                int bookingId = sd.MaDP;
                _context.SuDungDichVus.Remove(sd);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xoá dịch vụ đã chọn!";
                return RedirectToAction("AddService", new { bookingId });
            }
            return RedirectToAction("RoomMap");
        }

        // ==========================================
        // 3. CHECK-OUT & XUẤT HÓA ĐƠN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Checkout(int bookingId)
        {
            var booking = await _context.DatPhongs
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.Phong)
                .ThenInclude(p => p.LoaiPhong)
                .FirstOrDefaultAsync(dp => dp.MaDP == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.TrangThai == "Đã thanh toán")
            {
                var inv = await _context.HoaDons.FirstOrDefaultAsync(h => h.MaDP == bookingId);
                if (inv != null)
                {
                    TempData["SuccessMessage"] = "Đơn đặt phòng này đã được thanh toán!";
                    return RedirectToAction("InvoiceDetail", new { id = inv.MaHD });
                }
                TempData["ErrorMessage"] = "Đơn đặt phòng đã thanh toán nhưng không tìm thấy hóa đơn!";
                return RedirectToAction("RoomMap");
            }

            if (booking.TrangThai == "Đã huỷ")
            {
                TempData["ErrorMessage"] = "Đơn đặt phòng đã bị huỷ!";
                return RedirectToAction("RoomMap");
            }

            // Tính toán tạm tính
            var now = DateTime.Now;
            var checkin = booking.NgayCheckIn;
            // Dùng ngày hiện tại nếu khách ở quá hạn, hoặc ngày checkout đã book nếu trả sớm
            var checkout = now > booking.NgayCheckOut ? now : (booking.NgayCheckOut ?? now);
            int days = Math.Max(1, (checkout - checkin).Days);

            decimal roomCharge = days * booking.Phong.LoaiPhong.GiaPhong;

            // Tính tiền dịch vụ
            decimal serviceCharge = await _context.SuDungDichVus
                .Where(sd => sd.MaDP == bookingId)
                .SumAsync(sd => sd.ThanhTien);

            decimal subTotal = roomCharge + serviceCharge;

            // Giảm giá VIP (10% khi ở từ 2 ngày trở lên)
            decimal vipDiscount = (days >= 2) ? Math.Round(subTotal * 0.10m) : 0;

            // Dịch vụ đã dùng
            ViewBag.UsedServices = await _context.SuDungDichVus
                .Include(sd => sd.DichVu)
                .Where(sd => sd.MaDP == bookingId)
                .ToListAsync();

            ViewBag.Days = days;
            ViewBag.RoomCharge = roomCharge;
            ViewBag.ServiceCharge = serviceCharge;
            ViewBag.SubTotal = subTotal;
            ViewBag.VipDiscount = vipDiscount;
            ViewBag.CheckoutTime = checkout;

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(int bookingId, string? voucherCode)
        {
            var booking = await _context.DatPhongs
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.Phong)
                .ThenInclude(p => p.LoaiPhong)
                .FirstOrDefaultAsync(dp => dp.MaDP == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn đặt phòng!";
                return RedirectToAction("RoomMap");
            }

            if (booking.TrangThai == "Đã thanh toán")
            {
                var inv = await _context.HoaDons.FirstOrDefaultAsync(h => h.MaDP == bookingId);
                if (inv != null)
                {
                    return RedirectToAction("InvoiceDetail", new { id = inv.MaHD });
                }
                TempData["ErrorMessage"] = "Đơn đặt phòng đã được thanh toán!";
                return RedirectToAction("RoomMap");
            }

            var now = DateTime.Now;
            var checkin = booking.NgayCheckIn;
            var checkout = now > booking.NgayCheckOut ? now : (booking.NgayCheckOut ?? now);
            int days = Math.Max(1, (checkout - checkin).Days);

            decimal roomCharge = days * booking.Phong.LoaiPhong.GiaPhong;
            decimal serviceCharge = await _context.SuDungDichVus
                .Where(sd => sd.MaDP == bookingId)
                .SumAsync(sd => sd.ThanhTien);

            decimal subTotal = roomCharge + serviceCharge;

            // 1. Tính giảm giá VIP (ở >= 2 ngày được giảm 10%)
            decimal vipDiscount = (days >= 2) ? Math.Round(subTotal * 0.10m) : 0;

            // 2. Tính giảm giá từ Voucher
            decimal voucherDiscount = 0;
            int? voucherId = null;
            string voucherName = "";

            if (!string.IsNullOrEmpty(voucherCode))
            {
                var today = DateTime.Today;
                var vc = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == voucherCode.Trim().ToUpper() && 
                                              v.TrangThai == "active" && 
                                              v.NgayHetHan >= today && 
                                              (v.MaKH == null || v.MaKH == booking.MaKH) && 
                                              v.SoLanDaDung < v.GioiHanDung);

                if (vc != null && subTotal >= vc.GiaTriToiThieu)
                {
                    if (vc.LoaiGiam == "phantram")
                    {
                        voucherDiscount = Math.Round(subTotal * (vc.GiaTriGiam / 100m));
                    }
                    else
                    {
                        voucherDiscount = vc.GiaTriGiam;
                    }
                    voucherDiscount = Math.Min(voucherDiscount, subTotal);
                    voucherId = vc.MaVC;
                    voucherName = vc.TenVoucher;

                    // Cập nhật số lần dùng voucher
                    vc.SoLanDaDung++;
                    if (vc.SoLanDaDung >= vc.GioiHanDung)
                    {
                        vc.TrangThai = "inactive";
                    }
                    _context.Vouchers.Update(vc);
                }
            }

            decimal totalDiscount = vipDiscount + voucherDiscount;
            decimal finalAmount = Math.Max(0, subTotal - totalDiscount);

            // Cập nhật đơn đặt phòng sang Đã thanh toán
            booking.NgayCheckOut = checkout;
            booking.TrangThai = "Đã thanh toán";
            _context.DatPhongs.Update(booking);

            // Cập nhật phòng về Trống
            var room = await _context.Phongs.FindAsync(booking.MaPhong);
            if (room != null)
            {
                room.TrangThai = "Trống";
                _context.Phongs.Update(room);
            }

            // Tạo Hóa Đơn
            var creatorIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? creatorId = int.TryParse(creatorIdStr, out int cid) ? cid : null;
            // Tìm MaNV tương ứng với MaTK
            int? employeeId = null;
            if (creatorId.HasValue)
            {
                var emp = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaTK == creatorId.Value);
                if (emp != null) employeeId = emp.MaNV;
            }

            var invoice = new HoaDon
            {
                MaDP = bookingId,
                MaNV = employeeId,
                TienPhong = roomCharge,
                TienDichVu = serviceCharge,
                GiamGiaThanhVien = totalDiscount, // Lưu tổng tiền giảm giá
                TongTien = finalAmount,
                NgayLanhToan = now
            };
            _context.HoaDons.Add(invoice);

            // Tự động tặng voucher "Khách Hàng Thân Thiết 10%" mỗi 2 lần thanh toán thành công
            var completedBookingsCount = await _context.DatPhongs
                .CountAsync(dp => dp.MaKH == booking.MaKH && 
                                 (dp.TrangThai == "Đã thanh toán" || dp.TrangThai == "Đã thanh toán (Online)"));
            
            // Số lần đã thanh toán thực tế bao gồm cả đơn này
            int expectedLoyalVouchers = completedBookingsCount / 2;
            int existingLoyalVouchers = await _context.Vouchers
                .CountAsync(v => v.MaKH == booking.MaKH && v.TenVoucher == "Khách Hàng Thân Thiết 10%");

            if (expectedLoyalVouchers > existingLoyalVouchers)
            {
                var rng = new Random();
                var newCode = "LOYAL-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                var loyalVoucher = new Voucher
                {
                    MaKH = booking.MaKH,
                    Code = newCode,
                    TenVoucher = "Khách Hàng Thân Thiết 10%",
                    LoaiGiam = "phantram",
                    GiaTriGiam = 10,
                    GiaTriToiThieu = 0,
                    NgayHetHan = DateTime.Today.AddYears(1),
                    GioiHanDung = 1,
                    TrangThai = "active",
                    GhiChu = "Tặng tự động sau mỗi 2 lần đặt phòng thành công",
                    NgayTao = DateTime.Now
                };
                _context.Vouchers.Add(loyalVoucher);
            }

            // Cập nhật điểm tích lũy và hạng thành viên cho khách hàng
            int pointsEarned = 0;
            var customer = await _context.KhachHangs.FindAsync(booking.MaKH);
            if (customer != null)
            {
                pointsEarned = (int)(finalAmount / 1000000m);
                if (pointsEarned > 0)
                {
                    customer.DiemTichLuy += pointsEarned;
                    
                    // Cập nhật hạng thành viên
                    if (customer.DiemTichLuy >= 100)
                        customer.HangThanhVien = "Legend";
                    else if (customer.DiemTichLuy >= 50)
                        customer.HangThanhVien = "VIP";
                    else if (customer.DiemTichLuy >= 20)
                        customer.HangThanhVien = "Kim Cương";
                    else if (customer.DiemTichLuy >= 10)
                        customer.HangThanhVien = "Bạch Kim";
                    else if (customer.DiemTichLuy >= 5)
                        customer.HangThanhVien = "Vàng";
                    else
                        customer.HangThanhVien = "Đồng";
                        
                    _context.KhachHangs.Update(customer);
                }
            }

            await _context.SaveChangesAsync();

            // Gửi email hóa đơn cho khách hàng
            if (booking.KhachHang != null && !string.IsNullOrEmpty(booking.KhachHang.Email))
            {
                _ = Task.Run(() => _emailService.SendInvoiceEmailAsync(
                    booking.KhachHang.Email, booking.KhachHang.HoTen, days, roomCharge, serviceCharge, totalDiscount, voucherName, finalAmount));
            }

            TempData["SuccessMessage"] = $"Thanh toán & xuất hóa đơn cho phòng {booking.MaPhong} thành công! Khách hàng đã được tích lũy {pointsEarned} điểm.";
            return RedirectToAction("InvoiceDetail", new { id = invoice.MaHD });
        }

        // ==========================================
        // 4. QUẢN LÝ NHÂN VIÊN & CA LÀM / CHẤM CÔNG
        // ==========================================
        public async Task<IActionResult> Employees()
        {
            var employees = await _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .OrderBy(nv => nv.ChucVu)
                .ToListAsync();

            return View(employees);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee(string username, string password, string hoten, string role, decimal salary, string shift, string? sdt, string? email)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hoten) || string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(shift))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ các trường thông tin bắt buộc!";
                return RedirectToAction("Employees");
            }

            var isUserExists = await _context.TaiKhoans.AnyAsync(tk => tk.TenDangNhap == username.Trim());
            if (isUserExists)
            {
                TempData["ErrorMessage"] = $"Tên đăng nhập '{username}' đã được sử dụng!";
                return RedirectToAction("Employees");
            }

            // Tạo TaiKhoan
            var tk = new TaiKhoan
            {
                TenDangNhap = username.Trim(),
                MatKhau = BCrypt.Net.BCrypt.HashPassword(password),
                HoTen = hoten.Trim(),
                VaiTro = role
            };

            _context.TaiKhoans.Add(tk);
            await _context.SaveChangesAsync();

            // Tạo NhanVien
            var nv = new NhanVien
            {
                MaTK = tk.MaTK,
                ChucVu = role,
                Luong = salary,
                CaLamViec = shift,
                SDT = string.IsNullOrWhiteSpace(sdt) ? null : sdt.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                NgayVaoLam = DateTime.Today
            };

            _context.NhanViens.Add(nv);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm nhân viên {hoten} thành công!";
            return RedirectToAction("Employees");
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(int id, string hoten, string role, decimal salary, string shift, string? sdt, string? email, string? password)
        {
            var nv = await _context.NhanViens.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNV == id);
            if (nv == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên!";
                return RedirectToAction("Employees");
            }

            if (string.IsNullOrWhiteSpace(hoten) || string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(shift))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ các trường thông tin bắt buộc!";
                return RedirectToAction("Employees");
            }

            if (nv.TaiKhoan != null)
            {
                nv.TaiKhoan.HoTen = hoten.Trim();
                nv.TaiKhoan.VaiTro = role;
                if (!string.IsNullOrWhiteSpace(password))
                {
                    nv.TaiKhoan.MatKhau = BCrypt.Net.BCrypt.HashPassword(password);
                }
                _context.TaiKhoans.Update(nv.TaiKhoan);
            }

            nv.ChucVu = role;
            nv.Luong = salary;
            nv.CaLamViec = shift;
            nv.SDT = string.IsNullOrWhiteSpace(sdt) ? null : sdt.Trim();
            nv.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

            _context.NhanViens.Update(nv);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật thông tin nhân viên {hoten} thành công!";
            return RedirectToAction("Employees");
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var nv = await _context.NhanViens.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNV == id);
            if (nv == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên!";
                return RedirectToAction("Employees");
            }

            try
            {
                var tk = nv.TaiKhoan;
                _context.NhanViens.Remove(nv);
                if (tk != null)
                {
                    _context.TaiKhoans.Remove(tk);
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa nhân viên {nv.TaiKhoan?.HoTen} thành công!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = $"Không thể xóa nhân viên {nv.TaiKhoan?.HoTen} vì đã có ca làm việc hoặc chấm công liên quan!";
            }

            return RedirectToAction("Employees");
        }

        public async Task<IActionResult> Customers()
        {
            var customers = await _context.KhachHangs
                .OrderByDescending(c => c.MaKH)
                .ToListAsync();

            return View(customers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCustomer(string hoten, string cccd, string sdt, string? email, string rank, int points)
        {
            if (string.IsNullOrWhiteSpace(hoten) || string.IsNullOrWhiteSpace(cccd) || string.IsNullOrWhiteSpace(sdt))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Họ tên, CCCD và Số điện thoại!";
                return RedirectToAction("Customers");
            }

            var isCccdExists = await _context.KhachHangs.AnyAsync(k => k.CCCD == cccd.Trim());
            if (isCccdExists)
            {
                TempData["ErrorMessage"] = "Số CCCD này đã tồn tại trong hệ thống!";
                return RedirectToAction("Customers");
            }

            var kh = new KhachHang
            {
                HoTen = hoten.Trim(),
                CCCD = cccd.Trim(),
                SDT = sdt.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                HangThanhVien = rank,
                DiemTichLuy = points
            };

            _context.KhachHangs.Add(kh);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm khách hàng {hoten} thành công!";
            return RedirectToAction("Customers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(int id, string hoten, string cccd, string sdt, string? email, string rank, int points)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng!";
                return RedirectToAction("Customers");
            }

            if (string.IsNullOrWhiteSpace(hoten) || string.IsNullOrWhiteSpace(cccd) || string.IsNullOrWhiteSpace(sdt))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Họ tên, CCCD và Số điện thoại!";
                return RedirectToAction("Customers");
            }

            var isCccdExists = await _context.KhachHangs.AnyAsync(k => k.CCCD == cccd.Trim() && k.MaKH != id);
            if (isCccdExists)
            {
                TempData["ErrorMessage"] = "Số CCCD này đã trùng với một khách hàng khác!";
                return RedirectToAction("Customers");
            }

            kh.HoTen = hoten.Trim();
            kh.CCCD = cccd.Trim();
            kh.SDT = sdt.Trim();
            kh.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            kh.HangThanhVien = rank;
            kh.DiemTichLuy = points;

            _context.KhachHangs.Update(kh);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật thông tin khách hàng {hoten} thành công!";
            return RedirectToAction("Customers");
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var kh = await _context.KhachHangs.Include(k => k.TaiKhoan).FirstOrDefaultAsync(k => k.MaKH == id);
            if (kh == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng!";
                return RedirectToAction("Customers");
            }

            try
            {
                var tk = kh.TaiKhoan;
                _context.KhachHangs.Remove(kh);
                if (tk != null)
                {
                    _context.TaiKhoans.Remove(tk);
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa khách hàng {kh.HoTen} thành công!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = $"Không thể xóa khách hàng {kh.HoTen} vì đã có lịch sử đặt phòng liên quan!";
            }

            return RedirectToAction("Customers");
        }

        public async Task<IActionResult> Vouchers()
        {
            var vouchers = await _context.Vouchers
                .Include(v => v.KhachHang)
                .OrderByDescending(v => v.NgayTao)
                .ToListAsync();

            ViewBag.Customers = await _context.KhachHangs.OrderBy(c => c.HoTen).ToListAsync();
            return View(vouchers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVoucher(string code, string name, string type, decimal value, decimal minAmount, DateTime expiry, int limit, int? makh, string? khuvuc, string? note)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name) || value <= 0)
            {
                TempData["ErrorMessage"] = "Thông tin voucher không hợp lệ!";
                return RedirectToAction("Vouchers");
            }

            var isExists = await _context.Vouchers.AnyAsync(v => v.Code == code.ToUpper());
            if (isExists)
            {
                TempData["ErrorMessage"] = $"Mã Voucher {code} đã tồn tại!";
                return RedirectToAction("Vouchers");
            }

            var vc = new Voucher
            {
                Code = code.Trim().ToUpper(),
                TenVoucher = name.Trim(),
                LoaiGiam = type,
                GiaTriGiam = value,
                GiaTriToiThieu = minAmount,
                NgayBatDau = DateTime.Today,
                NgayHetHan = expiry,
                GioiHanDung = limit,
                SoLanDaDung = 0,
                TrangThai = "active",
                MaKH = makh,
                KhuVucApDung = string.IsNullOrEmpty(khuvuc) ? null : khuvuc.Trim(),
                GhiChu = note,
                NgayTao = DateTime.Now
            };

            _context.Vouchers.Add(vc);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Thêm mới Voucher {code} thành công!";
            return RedirectToAction("Vouchers");
        }

        public async Task<IActionResult> Shifts()
        {
            // Ca làm việc
            var ca = await _context.CaLamViecs.ToListAsync();
            // Lịch làm việc trong tuần này
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1); // Thứ 2
            var endOfWeek = startOfWeek.AddDays(6); // Chủ nhật

            var schedules = await _context.LichLamViecs
                .Include(l => l.NhanVien)
                    .ThenInclude(nv => nv.TaiKhoan)
                .Include(l => l.CaLamViec)
                .Where(l => l.NgayLam >= startOfWeek && l.NgayLam <= endOfWeek)
                .OrderBy(l => l.NgayLam)
                .ThenBy(l => l.CaLamViec.GioBatDau)
                .ToListAsync();

            // Chấm công hôm nay
            var today = DateTime.Today;
            var attendance = await _context.ChamCongs
                .Include(cc => cc.NhanVien)
                    .ThenInclude(nv => nv.TaiKhoan)
                .Where(cc => cc.NgayCC == today)
                .ToListAsync();

            ViewBag.Shifts = ca;
            ViewBag.Schedules = schedules;
            ViewBag.Attendance = attendance;
            ViewBag.Employees = await _context.NhanViens.Include(nv => nv.TaiKhoan).OrderBy(nv => nv.ChucVu).ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(int employeeId, int shiftId, DateTime workDate, string? note)
        {
            if (employeeId <= 0 || shiftId <= 0 || workDate == default)
            {
                TempData["ErrorMessage"] = "Thông tin phân ca không hợp lệ!";
                return RedirectToAction("Shifts");
            }

            var isExists = await _context.LichLamViecs
                .AnyAsync(l => l.MaNV == employeeId && l.NgayLam.Date == workDate.Date);

            if (isExists)
            {
                TempData["ErrorMessage"] = "Nhân viên này đã được phân ca trong ngày đã chọn!";
                return RedirectToAction("Shifts");
            }

            var sched = new LichLamViec
            {
                MaNV = employeeId,
                MaCa = shiftId,
                NgayLam = workDate,
                GhiChu = note
            };

            _context.LichLamViecs.Add(sched);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Phân lịch làm việc thành công!";
            return RedirectToAction("Shifts");
        }

        // ==========================================
        // 5. QUẢN LÝ ĐẶT PHÒNG (BOOKINGS MANAGEMENT)
        // ==========================================
        public async Task<IActionResult> Bookings(string status = "all")
        {
            var query = _context.DatPhongs
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.Phong)
                .ThenInclude(p => p.LoaiPhong)
                .AsQueryable();

            if (status != "all")
            {
                query = query.Where(dp => dp.TrangThai == status);
            }

            var bookings = await query.OrderByDescending(dp => dp.MaDP).ToListAsync();
            ViewBag.CurrentStatus = status;

            return View(bookings);
        }

        // Duyệt đặt phòng (Xác nhận đặt phòng)
        public async Task<IActionResult> ApproveBooking(int id)
        {
            var booking = await _context.DatPhongs
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.Phong)
                .ThenInclude(p => p.LoaiPhong)
                .FirstOrDefaultAsync(dp => dp.MaDP == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.TrangThai == "Chờ xác nhận")
            {
                booking.TrangThai = "Đã xác nhận";
                _context.DatPhongs.Update(booking);
                await _context.SaveChangesAsync();

                // Gửi email thông báo xác nhận cho khách hàng
                if (booking.KhachHang != null && !string.IsNullOrEmpty(booking.KhachHang.Email))
                {
                    int days = Math.Max(1, ((booking.NgayCheckOut ?? booking.NgayCheckIn.AddDays(1)) - booking.NgayCheckIn).Days);
                    decimal originalPrice = days * booking.Phong.LoaiPhong.GiaPhong;
                    
                    // Xác định giảm giá nếu có lưu trong Ghi chú hoặc mặc định là 0
                    decimal discount = 0; 
                    string paymentMethod = booking.GhiChu?.Contains("Online") == true ? "Online" : "Tiền mặt";

                    _ = Task.Run(() => _emailService.SendBookingConfirmationAsync(
                        booking.KhachHang.Email, 
                        booking.KhachHang.HoTen, 
                        booking.MaDP, 
                        booking.Phong.LoaiPhong.TenLoai + $" ({booking.Phong.MaPhong})",
                        booking.NgayCheckIn, 
                        booking.NgayCheckOut.Value, 
                        days, 
                        originalPrice, 
                        discount, 
                        "", 
                        paymentMethod, 
                        originalPrice));
                }

                TempData["SuccessMessage"] = $"Đã duyệt đơn đặt phòng #{id} thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Đơn đặt phòng #{id} không ở trạng thái chờ duyệt!";
            }

            return RedirectToAction("Bookings");
        }

        // Huỷ đặt phòng
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _context.DatPhongs.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            if (booking.TrangThai == "Chờ xác nhận" || booking.TrangThai == "Đã xác nhận" || booking.TrangThai == "Đã thanh toán (Online)")
            {
                booking.TrangThai = "Đã huỷ";
                _context.DatPhongs.Update(booking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã huỷ đơn đặt phòng #{id} thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Không thể huỷ đơn đặt phòng #{id} ở trạng thái hiện tại!";
            }

            return RedirectToAction("Bookings");
        }

        // ==========================================
        // 6. QUẢN LÝ HÓA ĐƠN (INVOICES MANAGEMENT)
        // ==========================================
        public async Task<IActionResult> Invoices(string search, string date)
        {
            var query = _context.HoaDons
                .Include(hd => hd.DatPhong)
                    .ThenInclude(dp => dp.KhachHang)
                .Include(hd => hd.DatPhong)
                    .ThenInclude(dp => dp.Phong)
                        .ThenInclude(p => p.LoaiPhong)
                .Include(hd => hd.NhanVien)
                    .ThenInclude(nv => nv.TaiKhoan)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(hd => hd.DatPhong!.KhachHang!.HoTen.Contains(search) || 
                                          hd.DatPhong!.KhachHang!.SDT.Contains(search) || 
                                          hd.MaHD.ToString() == search || 
                                          hd.DatPhong!.MaPhong.Contains(search));
            }

            if (!string.IsNullOrEmpty(date))
            {
                if (DateTime.TryParse(date, out DateTime parsedDate))
                {
                    query = query.Where(hd => hd.NgayLanhToan.Date == parsedDate.Date);
                }
            }

            var invoices = await query.OrderByDescending(hd => hd.MaHD).ToListAsync();
            ViewBag.Search = search;
            ViewBag.Date = date;

            return View(invoices);
        }

        public async Task<IActionResult> InvoiceDetail(int id)
        {
            var invoice = await _context.HoaDons
                .Include(hd => hd.DatPhong)
                    .ThenInclude(dp => dp.KhachHang)
                .Include(hd => hd.DatPhong)
                    .ThenInclude(dp => dp.Phong)
                        .ThenInclude(p => p.LoaiPhong)
                .Include(hd => hd.NhanVien)
                    .ThenInclude(nv => nv.TaiKhoan)
                .FirstOrDefaultAsync(hd => hd.MaHD == id);

            if (invoice == null)
            {
                return NotFound();
            }

            // Lấy các dịch vụ đã sử dụng trong đơn đặt phòng này
            ViewBag.UsedServices = await _context.SuDungDichVus
                .Include(sd => sd.DichVu)
                .Where(sd => sd.MaDP == invoice.MaDP)
                .ToListAsync();

            // Tính số ngày lưu trú
            int days = Math.Max(1, ((invoice.DatPhong!.NgayCheckOut ?? invoice.DatPhong.NgayCheckIn.AddDays(1)) - invoice.DatPhong.NgayCheckIn).Days);
            ViewBag.Days = days;

            return View(invoice);
        }

        public async Task<IActionResult> SendInvoiceEmail(int id)
        {
            var invoice = await _context.HoaDons
                .Include(hd => hd.DatPhong)
                    .ThenInclude(dp => dp.KhachHang)
                .Include(hd => hd.DatPhong)
                    .ThenInclude(dp => dp.Phong)
                        .ThenInclude(p => p.LoaiPhong)
                .FirstOrDefaultAsync(hd => hd.MaHD == id);

            if (invoice == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn cần gửi email!";
                return RedirectToAction("Invoices");
            }

            var customer = invoice.DatPhong?.KhachHang;
            if (customer == null || string.IsNullOrEmpty(customer.Email))
            {
                TempData["ErrorMessage"] = "Khách hàng này chưa có thông tin địa chỉ email để nhận hóa đơn!";
                return RedirectToAction("InvoiceDetail", new { id = id });
            }

            int days = Math.Max(1, ((invoice.DatPhong.NgayCheckOut ?? invoice.DatPhong.NgayCheckIn.AddDays(1)) - invoice.DatPhong.NgayCheckIn).Days);
            decimal roomCharge = invoice.TienPhong;
            decimal serviceCharge = invoice.TienDichVu;
            decimal totalDiscount = invoice.GiamGiaThanhVien;
            
            // Lấy tên voucher giảm giá nếu có
            string voucherName = "";
            
            try
            {
                await _emailService.SendInvoiceEmailAsync(
                    customer.Email, 
                    customer.HoTen, 
                    days, 
                    roomCharge, 
                    serviceCharge, 
                    totalDiscount, 
                    voucherName, 
                    invoice.TongTien
                );

                TempData["SuccessMessage"] = $"Đã gửi email hóa đơn thành công đến {customer.Email}!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi gửi email: {ex.Message}";
            }

            return RedirectToAction("InvoiceDetail", new { id = id });
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            var invoice = await _context.HoaDons.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            _context.HoaDons.Remove(invoice);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa hóa đơn #{id} thành công!";
            return RedirectToAction("Invoices");
        }

        // ==========================================
        // 7. QUẢN LÝ ĐÁNH GIÁ (REVIEWS MANAGEMENT)
        // ==========================================
        public async Task<IActionResult> Reviews(string search, int? stars)
        {
            var query = _context.DanhGias
                .Include(dg => dg.KhachHang)
                .Include(dg => dg.LoaiPhong)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(dg => dg.KhachHang.HoTen.Contains(search) || 
                                          dg.KhachHang.SDT.Contains(search) || 
                                          dg.LoaiPhong.TenLoai.Contains(search) || 
                                          dg.NhanXet.Contains(search));
            }

            if (stars.HasValue && stars.Value >= 1 && stars.Value <= 5)
            {
                query = query.Where(dg => dg.SoSao == stars.Value);
            }

            var reviews = await query.OrderByDescending(dg => dg.NgayDanhGia).ToListAsync();
            ViewBag.Search = search;
            ViewBag.Stars = stars;

            return View(reviews);
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.DanhGias.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            _context.DanhGias.Remove(review);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa đánh giá #{id} thành công!";
            return RedirectToAction("Reviews");
        }
    }

    public class RoomMapItemViewModel
    {
        public Phong Room { get; set; } = null!;
        public DatPhong? PendingCheckInBooking { get; set; }
        public DatPhong? CurrentBooking { get; set; }
        public DateTime? NgayCheckOut { get; set; }
    }
}
