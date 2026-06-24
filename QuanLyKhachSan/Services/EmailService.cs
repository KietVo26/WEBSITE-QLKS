using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace QuanLyKhachSan.Services
{
    public class EmailService
    {
        private readonly string _smtpHost = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _senderEmail = "kietvo.260605@gmail.com";
        private readonly string _senderPassword = "aetirvhkgbucwuyb";

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_senderEmail, _senderPassword),
                EnableSsl = true
            };
        }

        private void LogMail(string message)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mailer_log.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
            catch { }
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string toName, string otp)
        {
            LogMail($"GỌI HÀM SendOtpEmail cho: {toEmail}");
            try
            {
                using var client = CreateSmtpClient();
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "K-Hotel"),
                    Subject = "[K-Hotel] Mã xác thực OTP đăng ký tài khoản",
                    Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                        <div style='background: linear-gradient(135deg, #1e3a5f, #2563eb); padding: 32px; text-align: center; border-radius: 12px 12px 0 0;'>
                            <h1 style='color: white; margin: 0; font-size: 28px;'>🏨 K-Hotel</h1>
                            <p style='color: #bfdbfe; margin: 8px 0 0;'>Xác thực tài khoản đăng ký</p>
                        </div>
                        <div style='background: #f8fafc; padding: 32px; border-radius: 0 0 12px 12px; border: 1px solid #e2e8f0;'>
                            <p style='font-size: 16px; color: #1e293b;'>Xin chào <strong>{toName}</strong>,</p>
                            <p style='color: #475569;'>Đây là mã OTP xác thực đăng ký tài khoản K-Hotel của bạn:</p>

                            <div style='text-align: center; margin: 32px 0;'>
                                <div style='display: inline-block; background: #eff6ff; border: 3px dashed #2563eb;
                                            border-radius: 16px; padding: 20px 40px;'>
                                    <div style='font-size: 48px; font-weight: 900; letter-spacing: 12px;
                                                color: #1d4ed8; font-family: monospace;'>{otp}</div>
                                </div>
                            </div>

                            <div style='background: #fff7ed; border-left: 4px solid #f97316; padding: 14px 18px; border-radius: 8px; margin: 20px 0;'>
                                <strong style='color: #ea580c;'>⏰ Lưu ý:</strong><br>
                                <span style='color: #475569; font-size: 14px;'>
                                    Mã có hiệu lực trong <strong>5 phút</strong> và chỉ dùng được <strong>1 lần</strong>.<br>
                                    Nếu bạn không yêu cầu đăng ký, hãy bỏ qua email này.
                                </span>
                            </div>

                            <p style='color: #94a3b8; font-size: 13px; text-align: center; margin-top: 24px;'>© K-Hotel — {_senderEmail}</p>
                        </div>
                    </div>",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(toEmail, toName));

                LogMail($"ĐANG GỬI mail OTP cho: {toEmail}...");
                await client.SendMailAsync(mailMessage);
                LogMail($"THÀNH CÔNG: Đã gửi mail OTP cho {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                LogMail($"THẤT BẠI: Lỗi gửi mail OTP cho {toEmail}. Lỗi: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendResetPasswordEmailAsync(string toEmail, string toName, string resetUrl)
        {
            LogMail($"GỌI HÀM SendResetPasswordEmail cho: {toEmail}");
            try
            {
                using var client = CreateSmtpClient();
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "K-Hotel"),
                    Subject = "[K-Hotel] Yêu cầu đặt lại mật khẩu",
                    Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                        <div style='background: linear-gradient(135deg, #1e3a5f, #2563eb); padding: 32px; text-align: center; border-radius: 12px 12px 0 0;'>
                            <h1 style='color: white; margin: 0; font-size: 28px;'>🏨 K-Hotel</h1>
                            <p style='color: #bfdbfe; margin: 8px 0 0;'>Yêu cầu đặt lại mật khẩu</p>
                        </div>
                        <div style='background: #f8fafc; padding: 32px; border-radius: 0 0 12px 12px; border: 1px solid #e2e8f0;'>
                            <p style='font-size: 16px; color: #1e293b;'>Xin chào <strong>{toName}</strong>,</p>
                            <p style='color: #475569; line-height: 1.7;'>
                                Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản K-Hotel của bạn.<br>
                                Nhấn vào nút bên dưới để đặt lại mật khẩu:
                            </p>

                            <div style='text-align: center; margin: 32px 0;'>
                                <a href='{resetUrl}'
                                   style='display: inline-block; background: linear-gradient(135deg, #2563eb, #1d4ed8);
                                          color: white; padding: 16px 40px; border-radius: 12px;
                                          text-decoration: none; font-weight: bold; font-size: 16px;
                                          box-shadow: 0 4px 15px rgba(37,99,235,0.4);'>
                                    🔑 Đặt lại mật khẩu ngay
                                </a>
                            </div>

                            <div style='background: #fff7ed; border-left: 4px solid #f97316; padding: 14px 18px; border-radius: 8px; margin: 20px 0;'>
                                <strong style='color: #ea580c;'>⏰ Lưu ý quan trọng:</strong><br>
                                <span style='color: #475569; font-size: 14px;'>
                                    Link này chỉ có hiệu lực trong <strong>1 giờ</strong> và chỉ dùng được <strong>1 lần</strong>.
                                </span>
                            </div>

                            <p style='color: #64748b; font-size: 14px;'>
                                Nếu nút không hoạt động, sao chép đường dẫn này vào trình duyệt:<br>
                                <a href='{resetUrl}' style='color: #2563eb; word-break: break-all; font-size: 13px;'>{resetUrl}</a>
                            </p>

                            <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;'>
                            <p style='color: #94a3b8; font-size: 13px;'>
                                Nếu bạn <strong>không</strong> yêu cầu đổi mật khẩu, hãy bỏ qua email này —
                                tài khoản của bạn vẫn an toàn.
                            </p>
                            <p style='color: #94a3b8; font-size: 13px; text-align: center;'>© K-Hotel — {_senderEmail}</p>
                        </div>
                    </div>",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(toEmail, toName));

                LogMail($"ĐANG GỬI mail reset password cho: {toEmail}...");
                await client.SendMailAsync(mailMessage);
                LogMail($"THÀNH CÔNG: Đã gửi mail reset password cho {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                LogMail($"THẤT BẠI: Lỗi gửi mail reset password cho {toEmail}. Lỗi: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendBookingConfirmationAsync(string toEmail, string toName, int bookingId, string roomName, DateTime checkin, DateTime checkout, int days, decimal originalPrice, decimal discount, string voucherName, string paymentMethod, decimal total)
        {
            LogMail($"GỌI HÀM SendBookingConfirmation cho: {toEmail}");
            try
            {
                using var client = CreateSmtpClient();
                string discountHtml = "";
                if (discount > 0)
                {
                    discountHtml += $"<tr style='border-top: 1px solid #f1f5f9;'><td style='padding: 10px 0; color: #64748b;'>Giá gốc</td><td style='padding: 10px 0;'>{originalPrice:N0} ₫</td></tr>";
                    if (!string.IsNullOrEmpty(voucherName))
                    {
                        discountHtml += $"<tr style='border-top: 1px solid #f1f5f9;'><td style='padding: 10px 0; color: #16a34a;'>🎫 Voucher ({voucherName})</td><td style='padding: 10px 0; color: #16a34a; font-weight:bold;'>- {discount:N0} ₫</td></tr>";
                    }
                    else
                    {
                        discountHtml += $"<tr style='border-top: 1px solid #f1f5f9;'><td style='padding: 10px 0; color: #16a34a;'>👑 Giảm giá (10%)</td><td style='padding: 10px 0; color: #16a34a; font-weight:bold;'>- {discount:N0} ₫</td></tr>";
                    }
                }

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "K-Hotel"),
                    Subject = $"[K-Hotel] Xác nhận đặt phòng #{bookingId}",
                    Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                        <div style='background: #1e40af; padding: 32px; text-align: center; border-radius: 12px 12px 0 0;'>
                            <h1 style='color: white; margin: 0; font-size: 28px;'>🏨 K-Hotel</h1>
                            <p style='color: #bfdbfe; margin: 8px 0 0;'>Xác nhận đặt phòng thành công</p>
                        </div>
                        <div style='background: #f8fafc; padding: 32px; border-radius: 0 0 12px 12px; border: 1px solid #e2e8f0;'>
                            <p style='font-size: 16px; color: #1e293b;'>Xin chào <strong>{toName}</strong>,</p>
                            <p style='color: #475569;'>Cảm ơn bạn đã đặt phòng tại K-Hotel. Đây là thông tin xác nhận:</p>
                            
                            <div style='background: white; border-radius: 10px; padding: 20px; margin: 20px 0; border: 1px solid #e2e8f0;'>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr><td style='padding: 10px 0; color: #64748b; width: 140px;'>Mã đặt phòng</td><td style='padding: 10px 0; font-weight: bold; color: #1e40af;'>#{bookingId}</td></tr>
                                    <tr style='border-top: 1px solid #f1f5f9;'><td style='padding: 10px 0; color: #64748b;'>Loại phòng</td><td style='padding: 10px 0; font-weight: bold;'>{roomName}</td></tr>
                                    <tr style='border-top: 1px solid #f1f5f9;'><td style='padding: 10px 0; color: #64748b;'>Check-in</td><td style='padding: 10px 0;'>{checkin:HH:mm dd/MM/yyyy}</td></tr>
                                    <tr style='border-top: 1px solid #f1f5f9;'><td style='padding: 10px 0; color: #64748b;'>Check-out</td><td style='padding: 10px 0;'>{checkout:HH:mm dd/MM/yyyy}</td></tr>
                                    <tr style='border-top: 1px solid #f1f5f9;'><td style='padding: 10px 0; color: #64748b;'>Số ngày ở</td><td style='padding: 10px 0;'>{days} ngày</td></tr>
                                    {discountHtml}
                                    <tr style='border-top: 1px solid #f1f5f9;'><td style='padding: 10px 0; color: #64748b;'>Thanh toán</td><td style='padding: 10px 0;'>{paymentMethod}</td></tr>
                                    <tr style='border-top: 2px solid #e2e8f0;'><td style='padding: 12px 0; color: #1e293b; font-weight: bold;'>Tổng thanh toán</td><td style='padding: 12px 0; font-size: 20px; font-weight: bold; color: #dc2626;'>{total:N0} ₫</td></tr>
                                </table>
                            </div>

                            <div style='background: #eff6ff; border-left: 4px solid #1e40af; padding: 14px 18px; border-radius: 6px; margin: 16px 0;'>
                                <strong style='color: #1e40af;'>📋 Lưu ý khi nhận phòng:</strong><br>
                                <span style='color: #475569; font-size: 14px;'>Vui lòng mang theo CCCD/Hộ chiếu và mã đặt phòng khi làm thủ tục.</span>
                            </div>

                            <p style='color: #64748b; font-size: 14px;'>Nếu cần hỗ trợ, vui lòng liên hệ: <a href='mailto:{_senderEmail}' style='color: #1e40af;'>{_senderEmail}</a></p>
                            <p style='color: #94a3b8; font-size: 13px; margin-top: 24px; text-align: center;'>© K-Hotel — Trân trọng phục vụ quý khách</p>
                        </div>
                    </div>",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(toEmail, toName));

                LogMail($"ĐANG GỬI mail xác nhận cho: {toEmail}...");
                await client.SendMailAsync(mailMessage);
                LogMail($"THÀNH CÔNG: Đã gửi mail xác nhận cho {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                LogMail($"THẤT BẠI: Lỗi gửi mail xác nhận cho {toEmail}. Lỗi: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendInvoiceEmailAsync(string toEmail, string toName, int days, decimal roomPrice, decimal servicePrice, decimal discount, string voucherName, decimal total)
        {
            LogMail($"GỌI HÀM SendInvoiceEmail cho: {toEmail}");
            try
            {
                using var client = CreateSmtpClient();
                string discountHtml = "";
                if (discount > 0)
                {
                    if (!string.IsNullOrEmpty(voucherName))
                    {
                        discountHtml = $"<tr style='border-top: 1px solid #f1f5f9;'><td style='padding: 10px 0; color: #16a34a;'>🎫 Voucher ({voucherName})</td><td style='padding: 10px 0; text-align: right; color: #16a34a; font-weight: bold;'>- {discount:N0} ₫</td></tr>";
                    }
                    else
                    {
                        discountHtml = $"<tr style='border-top: 1px solid #f1f5f9;'><td style='padding: 10px 0; color: #16a34a;'>👑 Giảm giá VIP (10%)</td><td style='padding: 10px 0; text-align: right; color: #16a34a; font-weight: bold;'>- {discount:N0} ₫</td></tr>";
                    }
                }

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "K-Hotel"),
                    Subject = "[K-Hotel] Hóa đơn thanh toán - Cảm ơn quý khách!",
                    Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                        <div style='background: #065f46; padding: 32px; text-align: center; border-radius: 12px 12px 0 0;'>
                            <h1 style='color: white; margin: 0; font-size: 28px;'>🏨 K-Hotel</h1>
                            <p style='color: #a7f3d0; margin: 8px 0 0;'>Hóa đơn thanh toán</p>
                        </div>
                        <div style='background: #f8fafc; padding: 32px; border-radius: 0 0 12px 12px; border: 1px solid #e2e8f0;'>
                            <p style='font-size: 16px; color: #1e293b;'>Xin chào <strong>{toName}</strong>,</p>
                            <p style='color: #475569;'>Cảm ơn quý khách đã lưu trú tại K-Hotel. Đây là hóa đơn của bạn:</p>
                            
                            <div style='background: white; border-radius: 10px; padding: 20px; margin: 20px 0; border: 1px solid #e2e8f0;'>
                                <div style='font-size: 12px; color: #94a3b8; margin-bottom: 12px;'>Ngày lập: {DateTime.Now:HH:mm dd/MM/yyyy}</div>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr><td style='padding: 10px 0; color: #64748b;'>Tiền phòng ({days} ngày)</td><td style='padding: 10px 0; text-align: right;'>{roomPrice:N0} ₫</td></tr>
                                    <tr style='border-top: 1px solid #f1f5f9;'><td style='padding: 10px 0; color: #64748b;'>Phí dịch vụ</td><td style='padding: 10px 0; text-align: right;'>{servicePrice:N0} ₫</td></tr>
                                    {discountHtml}
                                    <tr style='border-top: 2px solid #e2e8f0;'><td style='padding: 14px 0; font-size: 18px; font-weight: bold; color: #1e293b;'>TỔNG THANH TOÁN</td><td style='padding: 14px 0; font-size: 22px; font-weight: bold; color: #dc2626; text-align: right;'>{total:N0} ₫</td></tr>
                                </table>
                            </div>

                            <div style='background: #f0fdf4; border-left: 4px solid #16a34a; padding: 14px 18px; border-radius: 6px;'>
                                <strong style='color: #16a34a;'>🙏 Cảm ơn quý khách!</strong><br>
                                <span style='color: #475569; font-size: 14px;'>Rất vui được phục vụ bạn. Hẹn gặp lại tại K-Hotel!</span>
                            </div>

                            <p style='color: #94a3b8; font-size: 13px; text-align: center; margin-top: 24px;'>© K-Hotel — {_senderEmail}</p>
                        </div>
                    </div>",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(toEmail, toName));

                LogMail($"ĐANG GỬI mail hóa đơn cho: {toEmail}...");
                await client.SendMailAsync(mailMessage);
                LogMail($"THÀNH CÔNG: Đã gửi mail hóa đơn cho {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                LogMail($"THẤT BẠI: Lỗi gửi mail hóa đơn cho {toEmail}. Lỗi: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendCancellationEmailAsync(string toEmail, string toName, string bookingId, string reason)
        {
            LogMail($"GỌI HÀM SendCancellationEmail cho: {toEmail} (Đơn #{bookingId})");
            try
            {
                using var client = CreateSmtpClient();
                string reasonHtml = !string.IsNullOrEmpty(reason) ? $"<p style='color: #475569;'><strong>Lý do:</strong> {reason}</p>" : "";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "K-Hotel"),
                    Subject = $"[K-Hotel] Thông báo hủy đặt phòng #{bookingId}",
                    Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                        <div style='background: #dc2626; padding: 32px; text-align: center; border-radius: 12px 12px 0 0;'>
                            <h1 style='color: white; margin: 0; font-size: 28px;'>🏨 K-Hotel</h1>
                            <p style='color: #fecaca; margin: 8px 0 0;'>Thông báo hủy đặt phòng</p>
                        </div>
                        <div style='background: #f8fafc; padding: 32px; border-radius: 0 0 12px 12px; border: 1px solid #e2e8f0;'>
                            <p style='font-size: 16px; color: #1e293b;'>Xin chào <strong>{toName}</strong>,</p>
                            <p style='color: #475569;'>Đơn đặt phòng <strong>#{bookingId}</strong> của bạn đã bị hủy.</p>
                            {reasonHtml}
                            <div style='background: #fff7ed; border-left: 4px solid #f97316; padding: 14px 18px; border-radius: 6px;'>
                                <strong style='color: #ea580c;'>Cần hỗ trợ?</strong><br>
                                <span style='color: #475569; font-size: 14px;'>Liên hệ ngay: <a href='mailto:{_senderEmail}'>{_senderEmail}</a></span>
                            </div>
                            <p style='color: #94a3b8; font-size: 13px; text-align: center; margin-top: 24px;'>© K-Hotel</p>
                        </div>
                    </div>",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(new MailAddress(toEmail, toName));

                LogMail($"ĐANG GỬI mail hủy đơn cho: {toEmail} (Đơn #{bookingId})");
                await client.SendMailAsync(mailMessage);
                LogMail($"THÀNH CÔNG: Đã gửi mail hủy đơn cho {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                LogMail($"THẤT BẠI: Lỗi gửi mail hủy đơn cho {toEmail}. Lỗi: {ex.Message}");
                return false;
            }
        }
    }
}
