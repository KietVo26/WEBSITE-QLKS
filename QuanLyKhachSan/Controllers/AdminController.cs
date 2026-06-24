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

            // Lấy các đơn đặt phòng active (đang chờ nhận, đã xác nhận, đang ở) để phục vụ hiển thị
            var activeBookings = await _context.DatPhongs
                .Include(dp => dp.KhachHang)
                .Where(dp => dp.TrangThai == "Chờ xác nhận" || dp.TrangThai == "Đã xác nhận" || dp.TrangThai == "Đang ở")
                .ToListAsync();

            var roomMapList = new List<RoomMapItemViewModel>();

            foreach (var r in rooms)
            {
                // Kiểm tra xem phòng có booking nào chờ checkin ngay lúc này không
                var pendingBooking = activeBookings
                    .Where(dp => dp.MaPhong == r.MaPhong && 
                                 (dp.TrangThai == "Đã xác nhận" || dp.TrangThai == "Đã thanh toán (Online)" || dp.TrangThai == "Chờ xác nhận") &&
                                 dp.NgayCheckIn <= now)
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
            // Tìm đơn đặt phòng hợp lệ (chờ nhận/đã xác nhận/đang chờ)
            var booking = await _context.DatPhongs
                .Where(dp => dp.MaPhong == id && 
                             (dp.TrangThai == "Đã xác nhận" || dp.TrangThai == "Đã thanh toán (Online)" || dp.TrangThai == "Chờ xác nhận") &&
                             dp.NgayCheckIn <= now)
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

            await _context.SaveChangesAsync();

            // Gửi email hóa đơn cho khách hàng
            if (booking.KhachHang != null && !string.IsNullOrEmpty(booking.KhachHang.Email))
            {
                _ = Task.Run(() => _emailService.SendInvoiceEmailAsync(
                    booking.KhachHang.Email, booking.KhachHang.HoTen, days, roomCharge, serviceCharge, totalDiscount, voucherName, finalAmount));
            }

            TempData["SuccessMessage"] = $"Thanh toán & xuất hóa đơn cho phòng {booking.MaPhong} thành công!";
            return RedirectToAction("RoomMap");
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

        public async Task<IActionResult> Customers()
        {
            var customers = await _context.KhachHangs
                .OrderByDescending(c => c.MaKH)
                .ToListAsync();

            return View(customers);
        }

        public async Task<IActionResult> Vouchers()
        {
            var vouchers = await _context.Vouchers
                .Include(v => v.KhachHang)
                .OrderByDescending(v => v.NgayTao)
                .ToListAsync();

            return View(vouchers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVoucher(string code, string name, string type, decimal value, decimal minAmount, DateTime expiry, int limit, string? note)
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
                .Include(l => l.CaLamViec)
                .Where(l => l.NgayLam >= startOfWeek && l.NgayLam <= endOfWeek)
                .OrderBy(l => l.NgayLam)
                .ThenBy(l => l.CaLamViec.GioBatDau)
                .ToListAsync();

            // Chấm công hôm nay
            var today = DateTime.Today;
            var attendance = await _context.ChamCongs
                .Include(cc => cc.NhanVien)
                .Where(cc => cc.NgayCC == today)
                .ToListAsync();

            ViewBag.Shifts = ca;
            ViewBag.Schedules = schedules;
            ViewBag.Attendance = attendance;
            ViewBag.Employees = await _context.NhanViens.OrderBy(nv => nv.ChucVu).ToListAsync();

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
    }

    public class RoomMapItemViewModel
    {
        public Phong Room { get; set; } = null!;
        public DatPhong? PendingCheckInBooking { get; set; }
        public DatPhong? CurrentBooking { get; set; }
        public DateTime? NgayCheckOut { get; set; }
    }
}
