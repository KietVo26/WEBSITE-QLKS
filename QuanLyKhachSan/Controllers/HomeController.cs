using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Models;

namespace QuanLyKhachSan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. TRANG CHỦ & HỆ THỐNG GỢI Ý PHÒNG THÔNG MINH
        // ==========================================
        public async Task<IActionResult> Index()
        {
            // Lấy thông tin người dùng từ Claims
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var recommendations = new List<PhongViewModel>();
            var allVacantRooms = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai == "Trống")
                .ToListAsync();

            if (int.TryParse(userIdStr, out int userId) && role == "khach")
            {
                // Khách hàng đã đăng nhập: Lấy lịch sử 10 đơn đặt phòng gần nhất của họ
                var historyBookings = await _context.DatPhongs
                    .Include(dp => dp.Phong)
                    .ThenInclude(p => p.LoaiPhong)
                    .Where(dp => dp.KhachHang.MaTK == userId && 
                                 (dp.TrangThai == "Đang ở" || dp.TrangThai == "Đã thanh toán" || dp.TrangThai == "Đã thanh toán (Online)"))
                    .OrderByDescending(dp => dp.MaDP)
                    .Take(10)
                    .ToListAsync();

                if (historyBookings.Any())
                {
                    // Tính các chỉ số lịch sử
                    decimal avgPrice = historyBookings.Average(b => b.Phong.LoaiPhong.GiaPhong);
                    int maxGuest = historyBookings.Max(b => b.Phong.LoaiPhong.SoNguoiToiDa);
                    
                    // Thống kê các tiện nghi xuất hiện nhiều nhất
                    var amenitiesFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (var booking in historyBookings)
                    {
                        var amenities = booking.Phong.LoaiPhong.TienNghi?
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(a => a.Trim());
                        
                        if (amenities != null)
                        {
                            foreach (var a in amenities)
                            {
                                if (amenitiesFreq.ContainsKey(a))
                                    amenitiesFreq[a]++;
                                else
                                    amenitiesFreq[a] = 1;
                            }
                        }
                    }

                    var topAmenities = amenitiesFreq
                        .OrderByDescending(kv => kv.Value)
                        .Select(kv => kv.Key)
                        .Take(2)
                        .ToList();

                    // Tính điểm tương đồng (Similarity Score) cho mỗi phòng trống
                    var scoredRooms = new List<(Phong Room, double Score)>();
                    foreach (var room in allVacantRooms)
                    {
                        double score = 1.0; // Điểm cơ sở

                        // Tiêu chí 1: Khoảng giá (nằm trong khoảng 70% - 140% giá trung bình)
                        if (room.LoaiPhong.GiaPhong >= avgPrice * 0.7m && room.LoaiPhong.GiaPhong <= avgPrice * 1.4m)
                        {
                            score += 5.0;
                        }

                        // Tiêu chí 2: Số người tối đa đáp ứng được
                        if (room.LoaiPhong.SoNguoiToiDa >= maxGuest)
                        {
                            score += 3.0;
                        }

                        // Tiêu chí 3: Chứa các tiện ích ưa thích
                        if (room.LoaiPhong.TienNghi != null)
                        {
                            var roomAmenities = room.LoaiPhong.TienNghi
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(a => a.Trim())
                                .ToList();

                            foreach (var topAmenity in topAmenities)
                            {
                                if (roomAmenities.Any(ra => ra.Equals(topAmenity, StringComparison.OrdinalIgnoreCase)))
                                {
                                    score += 3.0;
                                }
                            }
                        }

                        scoredRooms.Add((room, score));
                    }

                    // Sắp xếp theo điểm tương đồng giảm dần, sau đó là giá tăng dần
                    recommendations = scoredRooms
                        .OrderByDescending(x => x.Score)
                        .ThenBy(x => x.Room.LoaiPhong.GiaPhong)
                        .Take(4)
                        .Select(x => new PhongViewModel { Room = x.Room, Score = x.Score })
                        .ToList();
                }
            }

            // Nếu chưa đủ 4 phòng (khách mới, chưa đăng nhập hoặc không đủ phòng gợi ý)
            if (recommendations.Count < 4)
            {
                int needed = 4 - recommendations.Count;
                var excludedRoomIds = recommendations.Select(r => r.Room.MaPhong).ToList();

                var fallbackRooms = allVacantRooms
                    .Where(p => !excludedRoomIds.Contains(p.MaPhong))
                    .OrderByDescending(p => p.LoaiPhong.GiaPhong) // Gợi ý phòng cao cấp nhất
                    .Take(needed)
                    .Select(p => new PhongViewModel { Room = p, Score = 0 });

                recommendations.AddRange(fallbackRooms);
            }

            // Thống kê nhanh
            ViewBag.StatRooms = allVacantRooms.Count;
            ViewBag.StatRating = 4.8; // Điểm xếp hạng mặc định hoặc tính từ DB
            ViewBag.Recommendations = recommendations;

            // Lấy danh sách tất cả loại phòng để hiển thị trên homepage
            var roomTypes = await _context.LoaiPhongs
                .OrderBy(lp => lp.KhuVuc)
                .ThenBy(lp => lp.GiaPhong)
                .ToListAsync();

            return View(roomTypes);
        }

        // ==========================================
        // 2. TÌM KIẾM PHÒNG (SEARCH)
        // ==========================================
        public async Task<IActionResult> Search(string q, decimal? price, int? guest, string amenity, string khuvuc)
        {
            string keyword = q?.Trim() ?? "";
            decimal maxPrice = price ?? 5000000;
            int guestCount = guest ?? 0;
            string selAmenity = amenity?.Trim() ?? "";
            string selKhuVuc = khuvuc?.Trim() ?? "";

            // Bản đồ từ khóa sang khu vực
            var keywordMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "biển", "Phan Thiết" }, { "beach", "Phan Thiết" }, { "phan thiết", "Phan Thiết" }, { "cát", "Phan Thiết" }, { "bãi biển", "Phan Thiết" },
                { "núi", "Tây Ninh" }, { "mountain", "Tây Ninh" }, { "tây ninh", "Tây Ninh" }, { "rừng", "Tây Ninh" }, { "thiên nhiên", "Tây Ninh" }, { "bà đen", "Tây Ninh" },
                { "phố", "Hồ Chí Minh" }, { "thành phố", "Hồ Chí Minh" }, { "sài gòn", "Hồ Chí Minh" }, { "hồ chí minh", "Hồ Chí Minh" }, { "trung tâm", "Hồ Chí Minh" }, { "city", "Hồ Chí Minh" }, { "mua sắm", "Hồ Chí Minh" },
                { "hà nội", "Hà Nội" }, { "hanoi", "Hà Nội" }, { "phố cổ", "Hà Nội" }, { "hồ gươm", "Hà Nội" }, { "thủ đô", "Hà Nội" }, { "văn hóa", "Hà Nội" }, { "di sản", "Hà Nội" },
                { "đà nẵng", "Đà Nẵng" }, { "danang", "Đà Nẵng" }, { "mỹ khê", "Đà Nẵng" }, { "sơn trà", "Đà Nẵng" }, { "miền trung", "Đà Nẵng" }, { "hàn", "Đà Nẵng" }, { "ngũ hành sơn", "Đà Nẵng" }
            };

            string detectedRegion = "";
            foreach (var kv in keywordMap)
            {
                if (keyword.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                {
                    detectedRegion = kv.Value;
                    break;
                }
            }

            // Tạo truy vấn động bằng LINQ
            var query = _context.Phongs
                .Include(p => p.LoaiPhong)
                .Where(p => p.LoaiPhong.GiaPhong <= maxPrice);

            if (!string.IsNullOrEmpty(detectedRegion))
            {
                query = query.Where(p => p.LoaiPhong.KhuVuc == detectedRegion);
            }
            else if (!string.IsNullOrEmpty(selKhuVuc))
            {
                query = query.Where(p => p.LoaiPhong.KhuVuc == selKhuVuc);
            }

            if (!string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(detectedRegion))
            {
                query = query.Where(p => p.LoaiPhong.TenLoai.Contains(keyword) || 
                                         p.MaPhong.Contains(keyword) || 
                                         (p.LoaiPhong.TuKhoa != null && p.LoaiPhong.TuKhoa.Contains(keyword)) || 
                                         p.LoaiPhong.KhuVuc.Contains(keyword));
            }

            if (guestCount > 0)
            {
                query = query.Where(p => p.LoaiPhong.SoNguoiToiDa >= guestCount);
            }

            if (!string.IsNullOrEmpty(selAmenity))
            {
                query = query.Where(p => p.LoaiPhong.TienNghi != null && p.LoaiPhong.TienNghi.Contains(selAmenity));
            }

            var matchedRooms = await query
                .OrderBy(p => p.LoaiPhong.KhuVuc)
                .ThenBy(p => p.LoaiPhong.GiaPhong)
                .ToListAsync();

            // Lấy danh sách tất cả khu vực duy nhất cho combobox tìm kiếm
            ViewBag.AllRegions = await _context.LoaiPhongs
                .Where(lp => lp.KhuVuc != null)
                .Select(lp => lp.KhuVuc)
                .Distinct()
                .OrderBy(k => k)
                .ToListAsync();

            ViewBag.Keyword = q;
            ViewBag.Price = maxPrice;
            ViewBag.Guest = guestCount;
            ViewBag.Amenity = selAmenity;
            ViewBag.KhuVuc = selKhuVuc;

            return View(matchedRooms);
        }

        // ==========================================
        // 3. CHI TIẾT LOẠI PHÒNG (DETAIL)
        // ==========================================
        public async Task<IActionResult> Detail(int id)
        {
            var roomType = await _context.LoaiPhongs
                .FirstOrDefaultAsync(lp => lp.MaLoai == id);

            if (roomType == null)
            {
                return NotFound();
            }

            // Lấy danh sách đánh giá của loại phòng này
            var reviews = await _context.DanhGias
                .Include(d => d.KhachHang)
                .Where(d => d.MaLoai == id)
                .OrderByDescending(d => d.NgayDanhGia)
                .ToListAsync();

            // Tính sao trung bình
            double avgStars = reviews.Any() ? reviews.Average(r => r.SoSao) : 5.0;

            // Tìm các phòng trống thực tế thuộc loại phòng này
            var vacantRooms = await _context.Phongs
                .Where(p => p.MaLoai == id && p.TrangThai == "Trống")
                .ToListAsync();

            ViewBag.Reviews = reviews;
            ViewBag.AverageStars = Math.Round(avgStars, 1);
            ViewBag.VacantRooms = vacantRooms;

            // Lấy danh sách voucher hợp lệ của khách hàng
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var vouchers = new List<Voucher>();
            if (int.TryParse(userIdStr, out int userId))
            {
                var kh = await _context.KhachHangs.FirstOrDefaultAsync(k => k.MaTK == userId);
                if (kh != null)
                {
                    var today = DateTime.Today;
                    vouchers = await _context.Vouchers
                        .Where(v => v.TrangThai == "active" && 
                                    v.NgayHetHan >= today && 
                                    v.SoLanDaDung < v.GioiHanDung &&
                                    (v.MaKH == null || v.MaKH == kh.MaKH) &&
                                    (v.KhuVucApDung == null || v.KhuVucApDung == roomType.KhuVuc))
                        .ToListAsync();
                }
            }
            ViewBag.AvailableVouchers = vouchers;

            return View(roomType);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class PhongViewModel
    {
        public Phong Room { get; set; } = null!;
        public double Score { get; set; }
    }
}
