using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;

namespace QuanLyKhachSan.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ==========================================
        // 1. ĐĂNG NHẬP (LOGIN)
        // ==========================================
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                return RedirectToDashboard(role);
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Tên đăng nhập và mật khẩu không được để trống!";
                return View();
            }

            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(u => u.TenDangNhap == username.Trim());

            if (user == null)
            {
                ViewBag.Error = "Tên đăng nhập không tồn tại!";
                return View();
            }

            // Hỗ trợ cả BCrypt và Plain text (dành cho các tài khoản seed mặc định)
            bool isPasswordCorrect = false;
            if (user.MatKhau.StartsWith("$2") || user.MatKhau.StartsWith("$2a$") || user.MatKhau.StartsWith("$2y$") || user.MatKhau.StartsWith("$2b$"))
            {
                try
                {
                    isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, user.MatKhau);
                }
                catch
                {
                    isPasswordCorrect = (password == user.MatKhau);
                }
            }
            else
            {
                isPasswordCorrect = (password == user.MatKhau);
            }

            if (!isPasswordCorrect)
            {
                ViewBag.Error = "Mật khẩu không chính xác!";
                return View();
            }

            // Đăng nhập thành công, thiết lập Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.MaTK.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim(ClaimTypes.Role, user.VaiTro),
                new Claim("Username", user.TenDangNhap)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // Lưu thông tin session tương tự PHP
            HttpContext.Session.SetInt32("user_id", user.MaTK);
            HttpContext.Session.SetString("user_name", user.HoTen);
            HttpContext.Session.SetString("role", user.VaiTro);

            return RedirectToDashboard(user.VaiTro);
        }

        private IActionResult RedirectToDashboard(string? role)
        {
            if (role == "khach")
            {
                return RedirectToAction("Index", "Home");
            }
            // nhanvien, admin chuyển sang sơ đồ phòng
            return RedirectToAction("RoomMap", "Admin");
        }

        // ==========================================
        // 2. ĐĂNG KÝ (REGISTER) & OTP VERIFICATION
        // ==========================================
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string hoten, string username, string password, string sdt, string email, string cccd)
        {
            if (string.IsNullOrWhiteSpace(hoten) || string.IsNullOrWhiteSpace(username) || 
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(sdt) || 
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(cccd))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ các thông tin bắt buộc!";
                return View();
            }

            if (password.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự!";
                return View();
            }

            // Kiểm tra trùng lặp
            var isUsernameExists = await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == username);
            if (isUsernameExists)
            {
                ViewBag.Error = $"Tên đăng nhập <strong>{username}</strong> đã tồn tại!";
                return View();
            }

            var isCccdExists = await _context.KhachHangs.AnyAsync(k => k.CCCD == cccd);
            if (isCccdExists)
            {
                ViewBag.Error = $"Số CCCD <strong>{cccd}</strong> đã được đăng ký!";
                return View();
            }

            var isEmailExists = await _context.KhachHangs.AnyAsync(k => k.Email == email);
            if (isEmailExists)
            {
                ViewBag.Error = $"Email <strong>{email}</strong> đã được sử dụng!";
                return View();
            }

            // Tạo mã OTP ngẫu nhiên 6 chữ số
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            // Lưu thông tin đăng ký chờ duyệt vào Session
            var pendingReg = new PendingRegistrationModel
            {
                HoTen = hoten.Trim(),
                TenDangNhap = username.Trim(),
                MatKhau = BCrypt.Net.BCrypt.HashPassword(password), // Băm mật khẩu bằng BCrypt
                SDT = sdt.Trim(),
                Email = email.Trim(),
                CCCD = cccd.Trim(),
                OtpCode = otpCode,
                OtpExpiry = DateTime.Now.AddMinutes(5),
                OtpAttempts = 0
            };

            HttpContext.Session.SetString("PendingRegistration", JsonSerializer.Serialize(pendingReg));

            // Gửi OTP qua email
            var isSent = await _emailService.SendOtpEmailAsync(pendingReg.Email, pendingReg.HoTen, otpCode);
            if (isSent)
            {
                return RedirectToAction("VerifyOtp");
            }
            else
            {
                ViewBag.Error = "Không thể gửi mã OTP. Vui lòng kiểm tra lại địa chỉ email hoặc thử lại sau.";
                HttpContext.Session.Remove("PendingRegistration");
                return View();
            }
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var pendingSession = HttpContext.Session.GetString("PendingRegistration");
            if (string.IsNullOrEmpty(pendingSession))
            {
                return RedirectToAction("Register");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string otp)
        {
            var pendingSession = HttpContext.Session.GetString("PendingRegistration");
            if (string.IsNullOrEmpty(pendingSession))
            {
                return RedirectToAction("Register");
            }

            var pendingReg = JsonSerializer.Deserialize<PendingRegistrationModel>(pendingSession);
            if (pendingReg == null)
            {
                return RedirectToAction("Register");
            }

            if (DateTime.Now > pendingReg.OtpExpiry)
            {
                ViewBag.Error = "Mã OTP đã hết hạn! Vui lòng thực hiện đăng ký lại.";
                HttpContext.Session.Remove("PendingRegistration");
                return View();
            }

            if (pendingReg.OtpAttempts >= 5)
            {
                ViewBag.Error = "Bạn đã nhập sai OTP quá 5 lần. Vui lòng đăng ký lại.";
                HttpContext.Session.Remove("PendingRegistration");
                return View();
            }

            if (pendingReg.OtpCode != otp?.Trim())
            {
                pendingReg.OtpAttempts++;
                HttpContext.Session.SetString("PendingRegistration", JsonSerializer.Serialize(pendingReg));
                ViewBag.Error = $"Mã OTP không đúng! Lần thử còn lại: {5 - pendingReg.OtpAttempts}";
                return View();
            }

            // OTP đúng: thực hiện lưu dữ liệu vào Database
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tạo tài khoản
                var taiKhoan = new TaiKhoan
                {
                    TenDangNhap = pendingReg.TenDangNhap,
                    MatKhau = pendingReg.MatKhau,
                    HoTen = pendingReg.HoTen,
                    VaiTro = "khach"
                };
                _context.TaiKhoans.Add(taiKhoan);
                await _context.SaveChangesAsync();

                // 2. Tạo khách hàng liên kết với tài khoản
                var khachHang = new KhachHang
                {
                    MaTK = taiKhoan.MaTK,
                    HoTen = pendingReg.HoTen,
                    CCCD = pendingReg.CCCD,
                    SDT = pendingReg.SDT,
                    Email = pendingReg.Email,
                    DiemTichLuy = 0,
                    HangThanhVien = "Đồng"
                };
                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Xoá session tạm
                HttpContext.Session.Remove("PendingRegistration");

                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ViewBag.Error = $"Đã xảy ra lỗi trong quá trình lưu dữ liệu: {ex.Message}";
                return View();
            }
        }

        // ==========================================
        // 3. ĐĂNG XUẤT (LOGOUT)
        // ==========================================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 4. QUÊN MẬT KHẨU / ĐẶT LẠI MẬT KHẨU
        // ==========================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Vui lòng nhập email!";
                return View();
            }

            var kh = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.Email == email.Trim());

            if (kh == null || kh.TaiKhoan == null)
            {
                ViewBag.Error = "Email này không liên kết với tài khoản nào trong hệ thống!";
                return View();
            }

            // Tạo token reset password
            string token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var tokenEntity = new PasswordResetToken
            {
                MaTK = kh.TaiKhoan.MaTK,
                Token = token,
                ThoiGianHetHan = DateTime.Now.AddHours(1),
                NgayTao = DateTime.Now
            };

            _context.PasswordResetTokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            // Gửi link đặt lại mật khẩu qua email
            string scheme = Request.Scheme;
            var resetLink = $"{scheme}://{Request.Host}/Account/ResetPassword?token={token}";
            var isSent = await _emailService.SendResetPasswordEmailAsync(kh.Email!, kh.HoTen, resetLink);

            if (isSent)
            {
                ViewBag.Success = "Liên kết đặt lại mật khẩu đã được gửi qua Email của bạn. Vui lòng kiểm tra hộp thư đến (hoặc thư rác).";
            }
            else
            {
                ViewBag.Error = "Gửi email thất bại. Vui lòng thử lại sau!";
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var tokenRecord = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (tokenRecord == null || DateTime.Now > tokenRecord.ThoiGianHetHan)
            {
                TempData["ErrorMessage"] = "Đường dẫn đặt lại mật khẩu không hợp lệ hoặc đã hết hạn!";
                return RedirectToAction("Login");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var tokenRecord = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (tokenRecord == null || DateTime.Now > tokenRecord.ThoiGianHetHan)
            {
                TempData["ErrorMessage"] = "Đường dẫn đặt lại mật khẩu không hợp lệ hoặc đã hết hạn!";
                return RedirectToAction("Login");
            }

            ViewBag.Token = token;

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ViewBag.Error = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Xác nhận mật khẩu không khớp!";
                return View();
            }

            // Cập nhật mật khẩu mới
            var user = await _context.TaiKhoans.FindAsync(tokenRecord.MaTK);
            if (user != null)
            {
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(password);
                _context.TaiKhoans.Update(user);

                // Xoá tất cả token reset của tài khoản này
                var userTokens = await _context.PasswordResetTokens
                    .Where(t => t.MaTK == user.MaTK)
                    .ToListAsync();
                _context.PasswordResetTokens.RemoveRange(userTokens);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = "Đã xảy ra lỗi, không tìm thấy tài khoản!";
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    public class PendingRegistrationModel
    {
        public string HoTen { get; set; } = null!;
        public string TenDangNhap { get; set; } = null!;
        public string MatKhau { get; set; } = null!;
        public string SDT { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string CCCD { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
        public DateTime OtpExpiry { get; set; }
        public int OtpAttempts { get; set; }
    }
}
