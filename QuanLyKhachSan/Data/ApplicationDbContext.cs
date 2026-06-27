using System;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models;

namespace QuanLyKhachSan.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaiKhoan> TaiKhoans { get; set; } = null!;
        public DbSet<NhanVien> NhanViens { get; set; } = null!;
        public DbSet<KhachHang> KhachHangs { get; set; } = null!;
        public DbSet<LoaiPhong> LoaiPhongs { get; set; } = null!;
        public DbSet<Phong> Phongs { get; set; } = null!;
        public DbSet<DichVu> DichVus { get; set; } = null!;
        public DbSet<DatPhong> DatPhongs { get; set; } = null!;
        public DbSet<SuDungDichVu> SuDungDichVus { get; set; } = null!;
        public DbSet<DanhGia> DanhGias { get; set; } = null!;
        public DbSet<HoaDon> HoaDons { get; set; } = null!;
        public DbSet<Voucher> Vouchers { get; set; } = null!;
        public DbSet<CaLamViec> CaLamViecs { get; set; } = null!;
        public DbSet<LichLamViec> LichLamViecs { get; set; } = null!;
        public DbSet<ChamCong> ChamCongs { get; set; } = null!;
        public DbSet<ThuongPhat> ThuongPhats { get; set; } = null!;
        public DbSet<DanhGiaNhanVien> DanhGiaNhanViens { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure foreign key relations & cascading rules
            modelBuilder.Entity<NhanVien>()
                .HasOne(nv => nv.TaiKhoan)
                .WithOne()
                .HasForeignKey<NhanVien>(nv => nv.MaTK)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<KhachHang>()
                .HasOne(kh => kh.TaiKhoan)
                .WithOne()
                .HasForeignKey<KhachHang>(kh => kh.MaTK)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Phong>()
                .HasOne(p => p.LoaiPhong)
                .WithMany()
                .HasForeignKey(p => p.MaLoai)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DatPhong>()
                .HasOne(dp => dp.KhachHang)
                .WithMany()
                .HasForeignKey(dp => dp.MaKH)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DatPhong>()
                .HasOne(dp => dp.Phong)
                .WithMany()
                .HasForeignKey(dp => dp.MaPhong)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SuDungDichVu>()
                .HasOne(sd => sd.DatPhong)
                .WithMany(dp => dp.SuDungDichVus)
                .HasForeignKey(sd => sd.MaDP)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SuDungDichVu>()
                .HasOne(sd => sd.DichVu)
                .WithMany()
                .HasForeignKey(sd => sd.MaDV)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DanhGia>()
                .HasOne(dg => dg.KhachHang)
                .WithMany()
                .HasForeignKey(dg => dg.MaKH)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DanhGia>()
                .HasOne(dg => dg.LoaiPhong)
                .WithMany()
                .HasForeignKey(dg => dg.MaLoai)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DanhGia>()
                .HasOne(dg => dg.DatPhong)
                .WithMany()
                .HasForeignKey(dg => dg.MaDP)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HoaDon>()
                .HasOne(hd => hd.DatPhong)
                .WithMany()
                .HasForeignKey(hd => hd.MaDP)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HoaDon>()
                .HasOne(hd => hd.NhanVien)
                .WithMany()
                .HasForeignKey(hd => hd.MaNV)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Voucher>()
                .HasOne(v => v.KhachHang)
                .WithMany()
                .HasForeignKey(v => v.MaKH)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LichLamViec>()
                .HasOne(llv => llv.NhanVien)
                .WithMany()
                .HasForeignKey(llv => llv.MaNV)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LichLamViec>()
                .HasOne(llv => llv.CaLamViec)
                .WithMany()
                .HasForeignKey(llv => llv.MaCa)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChamCong>()
                .HasOne(cc => cc.NhanVien)
                .WithMany()
                .HasForeignKey(cc => cc.MaNV)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChamCong>()
                .HasOne(cc => cc.LichLamViec)
                .WithMany()
                .HasForeignKey(cc => cc.MaLich)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ThuongPhat>()
                .HasOne(tp => tp.NhanVien)
                .WithMany()
                .HasForeignKey(tp => tp.MaNV)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DanhGiaNhanVien>()
                .HasOne(dgnv => dgnv.KhachHang)
                .WithMany()
                .HasForeignKey(dgnv => dgnv.MaKH)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DanhGiaNhanVien>()
                .HasOne(dgnv => dgnv.NhanVien)
                .WithMany()
                .HasForeignKey(dgnv => dgnv.MaNV)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(t => t.TaiKhoan)
                .WithMany()
                .HasForeignKey(t => t.MaTK)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================================
            // SEEDING DATA FOR SQL SERVER
            // ==========================================

            // 1. TÀI KHOẢN
            modelBuilder.Entity<TaiKhoan>().HasData(
                new TaiKhoan { MaTK = 1, TenDangNhap = "admin", MatKhau = "admin", HoTen = "Quản trị viên", VaiTro = "admin" },
                new TaiKhoan { MaTK = 2, TenDangNhap = "letanlan", MatKhau = "lan123", HoTen = "Nguyễn Thị Lân", VaiTro = "nhanvien" },
                new TaiKhoan { MaTK = 3, TenDangNhap = "minhtrv", MatKhau = "minh123", HoTen = "Trần Văn Minh", VaiTro = "nhanvien" },
                new TaiKhoan { MaTK = 4, TenDangNhap = "hoale", MatKhau = "hoa123", HoTen = "Lê Thị Hoa", VaiTro = "nhanvien" },
                new TaiKhoan { MaTK = 5, TenDangNhap = "hungpv", MatKhau = "hung123", HoTen = "Phạm Văn Hùng", VaiTro = "nhanvien" },
                new TaiKhoan { MaTK = 6, TenDangNhap = "maivo", MatKhau = "mai123", HoTen = "Võ Thị Mai", VaiTro = "nhanvien" }
            );

            // 2. NHÂN VIÊN
            modelBuilder.Entity<NhanVien>().HasData(
                new NhanVien { MaNV = 1, MaTK = 1, ChucVu = "Quản lý", CaLamViec = "Hành chính", Luong = 20000000 },
                new NhanVien { MaNV = 2, MaTK = 2, ChucVu = "Lễ tân", CaLamViec = "Ca Sáng", Luong = 8500000, SDT = "0901111222", Email = "lan@gmail.com", NgayVaoLam = new DateTime(2024, 1, 15) },
                new NhanVien { MaNV = 3, MaTK = 3, ChucVu = "Lễ tân", CaLamViec = "Ca Chiều", Luong = 8000000, SDT = "0902222333", Email = "minh@gmail.com", NgayVaoLam = new DateTime(2024, 2, 1) },
                new NhanVien { MaNV = 4, MaTK = 4, ChucVu = "Thu ngân", CaLamViec = "Ca Sáng", Luong = 9000000, SDT = "0903333444", Email = "hoa@gmail.com", NgayVaoLam = new DateTime(2023, 11, 20) },
                new NhanVien { MaNV = 5, MaTK = 5, ChucVu = "Bảo vệ", CaLamViec = "Ca Đêm", Luong = 7500000, SDT = "0904444555", Email = "hung@gmail.com", NgayVaoLam = new DateTime(2024, 3, 10) },
                new NhanVien { MaNV = 6, MaTK = 6, ChucVu = "Dọn phòng", CaLamViec = "Ca Chiều", Luong = 7000000, SDT = "0905555666", Email = "mai@gmail.com", NgayVaoLam = new DateTime(2024, 4, 5) }
            );

            // 3. KHÁCH HÀNG MẪU
            modelBuilder.Entity<KhachHang>().HasData(
                new KhachHang { MaKH = 1, MaTK = null, HoTen = "Lê Thị D", CCCD = "045612378945", SDT = "0933456789", Email = "ltd@gmail.com", DiemTichLuy = 0, HangThanhVien = "Đồng" }
            );

            // 4. LOẠI PHÒNG
            modelBuilder.Entity<LoaiPhong>().HasData(
                // Hồ Chí Minh
                new LoaiPhong { MaLoai = 1, TenLoai = "Phòng Đơn Tiêu Chuẩn", GiaPhong = 300000, SoNguoiToiDa = 1, TienNghi = "Tivi, Máy Lạnh, Cửa Sổ Nhỏ", HinhAnh = "default_room.jpg", KhuVuc = "Hồ Chí Minh", TuKhoa = "thành phố, phố, trung tâm, sài gòn" },
                new LoaiPhong { MaLoai = 2, TenLoai = "Phòng Đôi Tiêu Chuẩn", GiaPhong = 500000, SoNguoiToiDa = 2, TienNghi = "Tivi, Tủ Lạnh, Cửa Sổ Hướng Phố", HinhAnh = "default_room.jpg", KhuVuc = "Hồ Chí Minh", TuKhoa = "thành phố, phố, trung tâm, sài gòn" },
                new LoaiPhong { MaLoai = 3, TenLoai = "Phòng VIP Hướng Biển", GiaPhong = 1200000, SoNguoiToiDa = 2, TienNghi = "Tivi OLED, Bồn tắm, Ban công siêu rộng, View Biển trực tiếp", HinhAnh = "default_room.jpg", KhuVuc = "Hồ Chí Minh", TuKhoa = "biển, resort, nghỉ dưỡng" },
                new LoaiPhong { MaLoai = 4, TenLoai = "Phòng Gia Đình", GiaPhong = 850000, SoNguoiToiDa = 4, TienNghi = "2 Giường Đôi cỡ lớn, Không gian sinh hoạt, Trò chơi điện tử", HinhAnh = "default_room.jpg", KhuVuc = "Hồ Chí Minh", TuKhoa = "gia đình, rộng" },
                
                // Phan Thiết
                new LoaiPhong { MaLoai = 5, TenLoai = "Phòng Biển Standard", GiaPhong = 600000, SoNguoiToiDa = 2, TienNghi = "View biển, Máy lạnh, Tivi, Ban công", HinhAnh = "default_room.jpg", KhuVuc = "Phan Thiết", TuKhoa = "biển, beach, phan thiết, nghỉ dưỡng" },
                new LoaiPhong { MaLoai = 6, TenLoai = "Phòng Biển Deluxe", GiaPhong = 900000, SoNguoiToiDa = 2, TienNghi = "View biển trực diện, Bồn tắm, Ban công rộng, Minibar", HinhAnh = "default_room.jpg", KhuVuc = "Phan Thiết", TuKhoa = "biển, beach, phan thiết, view" },
                new LoaiPhong { MaLoai = 7, TenLoai = "Phòng Biển Suite", GiaPhong = 1500000, SoNguoiToiDa = 3, TienNghi = "View biển panorama, Phòng khách riêng, Jacuzzi", HinhAnh = "default_room.jpg", KhuVuc = "Phan Thiết", TuKhoa = "biển, beach, phan thiết, suite" },
                new LoaiPhong { MaLoai = 8, TenLoai = "Phòng Biển Gia Đình", GiaPhong = 1200000, SoNguoiToiDa = 5, TienNghi = "2 Phòng ngủ, View biển, Bếp nhỏ, Sân vườn riêng", HinhAnh = "default_room.jpg", KhuVuc = "Phan Thiết", TuKhoa = "biển, beach, phan thiết, gia đình" },
                new LoaiPhong { MaLoai = 9, TenLoai = "Bungalow Biển VIP", GiaPhong = 2500000, SoNguoiToiDa = 4, TienNghi = "Bungalow riêng, Hồ bơi riêng, View biển 360°", HinhAnh = "default_room.jpg", KhuVuc = "Phan Thiết", TuKhoa = "biển, beach, phan thiết, vip, bungalow" },
                
                // Hà Nội
                new LoaiPhong { MaLoai = 10, TenLoai = "Phòng Phố Cổ Classic", GiaPhong = 450000, SoNguoiToiDa = 2, TienNghi = "View phố cổ, Nội thất gỗ, Máy lạnh, Tivi", HinhAnh = "default_room.jpg", KhuVuc = "Hà Nội", TuKhoa = "hà nội, phố cổ, văn hóa, hanoi" },
                new LoaiPhong { MaLoai = 11, TenLoai = "Phòng Hồ Gươm View", GiaPhong = 800000, SoNguoiToiDa = 2, TienNghi = "View Hồ Gươm, Ban công, Bồn tắm, Minibar", HinhAnh = "default_room.jpg", KhuVuc = "Hà Nội", TuKhoa = "hà nội, hồ gươm, view, hanoi" },
                new LoaiPhong { MaLoai = 12, TenLoai = "Phòng Heritage Suite", GiaPhong = 1400000, SoNguoiToiDa = 3, TienNghi = "Nội thất Đông Dương, Phòng khách rộng, Spa trong phòng", HinhAnh = "default_room.jpg", KhuVuc = "Hà Nội", TuKhoa = "hà nội, di sản, heritage, hanoi" },
                new LoaiPhong { MaLoai = 13, TenLoai = "Phòng Gia Đình Hà Nội", GiaPhong = 1000000, SoNguoiToiDa = 5, TienNghi = "2 Giường đôi, Phòng rộng, View thành phố", HinhAnh = "default_room.jpg", KhuVuc = "Hà Nội", TuKhoa = "hà nội, gia đình, hanoi" },
                new LoaiPhong { MaLoai = 14, TenLoai = "Penthouse Royal Hà Nội", GiaPhong = 3000000, SoNguoiToiDa = 4, TienNghi = "Tầng thượng, View toàn thành phố, Hồ bơi trên mái", HinhAnh = "default_room.jpg", KhuVuc = "Hà Nội", TuKhoa = "hà nội, penthouse, royal, vip, hanoi" },
                
                // Tây Ninh
                new LoaiPhong { MaLoai = 15, TenLoai = "Phòng Núi Standard", GiaPhong = 400000, SoNguoiToiDa = 2, TienNghi = "View núi Bà Đen, Máy lạnh, Tivi, Không gian thoáng", HinhAnh = "default_room.jpg", KhuVuc = "Tây Ninh", TuKhoa = "núi, mountain, tây ninh, thiên nhiên" },
                new LoaiPhong { MaLoai = 16, TenLoai = "Phòng Núi Deluxe", GiaPhong = 700000, SoNguoiToiDa = 2, TienNghi = "View núi panorama, Ban công rộng, Võng thư giãn, Trà đạo", HinhAnh = "default_room.jpg", KhuVuc = "Tây Ninh", TuKhoa = "núi, mountain, tây ninh, thư giãn" },
                new LoaiPhong { MaLoai = 17, TenLoai = "Phòng Rừng Xanh Eco", GiaPhong = 550000, SoNguoiToiDa = 2, TienNghi = "Eco-lodge, Gần rừng, Xe đạp miễn phí, BBQ", HinhAnh = "default_room.jpg", KhuVuc = "Tây Ninh", TuKhoa = "núi, mountain, tây ninh, eco, rừng" },
                new LoaiPhong { MaLoai = 18, TenLoai = "Phòng Gia Đình Núi", GiaPhong = 900000, SoNguoiToiDa = 5, TienNghi = "2 Phòng ngủ, View núi, Sân vườn rộng, Lò sưởi", HinhAnh = "default_room.jpg", KhuVuc = "Tây Ninh", TuKhoa = "núi, mountain, tây ninh, gia đình" },
                new LoaiPhong { MaLoai = 19, TenLoai = "Villa Đỉnh Núi VIP", GiaPhong = 2000000, SoNguoiToiDa = 6, TienNghi = "Villa riêng trên đồi, Hồ bơi vô cực, View 360° núi rừng", HinhAnh = "default_room.jpg", KhuVuc = "Tây Ninh", TuKhoa = "núi, mountain, tây ninh, villa, vip" },
                
                // Đà Nẵng
                new LoaiPhong { MaLoai = 20, TenLoai = "Phòng Biển Mỹ Khê", GiaPhong = 650000, SoNguoiToiDa = 2, TienNghi = "View biển Mỹ Khê, Máy lạnh, Tivi, Ban công", HinhAnh = "default_room.jpg", KhuVuc = "Đà Nẵng", TuKhoa = "đà nẵng, biển, mỹ khê, beach, miền trung, danang" },
                new LoaiPhong { MaLoai = 21, TenLoai = "Phòng Sơn Trà View", GiaPhong = 950000, SoNguoiToiDa = 2, TienNghi = "View bán đảo Sơn Trà, Bồn tắm, Ban công rộng", HinhAnh = "default_room.jpg", KhuVuc = "Đà Nẵng", TuKhoa = "đà nẵng, sơn trà, view, miền trung, danang" },
                new LoaiPhong { MaLoai = 22, TenLoai = "Phòng Suite Hàn River", GiaPhong = 1600000, SoNguoiToiDa = 3, TienNghi = "View sông Hàn, Phòng khách riêng, Minibar cao cấp", HinhAnh = "default_room.jpg", KhuVuc = "Đà Nẵng", TuKhoa = "đà nẵng, sông hàn, suite, sang trọng, danang" },
                new LoaiPhong { MaLoai = 23, TenLoai = "Phòng Gia Đình Đà Nẵng", GiaPhong = 1100000, SoNguoiToiDa = 5, TienNghi = "2 Phòng ngủ, View biển, Bếp nhỏ, Khu vui chơi", HinhAnh = "default_room.jpg", KhuVuc = "Đà Nẵng", TuKhoa = "đà nẵng, gia đình, rộng, miền trung, danang" },
                new LoaiPhong { MaLoai = 24, TenLoai = "Penthouse Ngũ Hành Sơn", GiaPhong = 2800000, SoNguoiToiDa = 4, TienNghi = "Tầng cao nhất, View Ngũ Hành Sơn, Hồ bơi riêng", HinhAnh = "default_room.jpg", KhuVuc = "Đà Nẵng", TuKhoa = "đà nẵng, ngũ hành sơn, penthouse, vip, danang" }
            );

            // 5. PHÒNG CỤ THỂ
            modelBuilder.Entity<Phong>().HasData(
                new Phong { MaPhong = "P101", MaLoai = 1, TrangThai = "Trống" },
                new Phong { MaPhong = "P102", MaLoai = 1, TrangThai = "Trống" },
                new Phong { MaPhong = "P201", MaLoai = 2, TrangThai = "Trống" },
                new Phong { MaPhong = "P202", MaLoai = 2, TrangThai = "Trống" },
                new Phong { MaPhong = "P301", MaLoai = 3, TrangThai = "Trống" },
                new Phong { MaPhong = "P302", MaLoai = 3, TrangThai = "Trống" },
                new Phong { MaPhong = "P401", MaLoai = 4, TrangThai = "Trống" },
                
                new Phong { MaPhong = "PT101", MaLoai = 5, TrangThai = "Trống" },
                new Phong { MaPhong = "PT102", MaLoai = 6, TrangThai = "Trống" },
                new Phong { MaPhong = "PT103", MaLoai = 7, TrangThai = "Trống" },
                new Phong { MaPhong = "PT104", MaLoai = 8, TrangThai = "Trống" },
                new Phong { MaPhong = "PT105", MaLoai = 9, TrangThai = "Trống" },
                
                new Phong { MaPhong = "HN101", MaLoai = 10, TrangThai = "Trống" },
                new Phong { MaPhong = "HN102", MaLoai = 11, TrangThai = "Trống" },
                new Phong { MaPhong = "HN103", MaLoai = 12, TrangThai = "Trống" },
                new Phong { MaPhong = "HN104", MaLoai = 13, TrangThai = "Trống" },
                new Phong { MaPhong = "HN105", MaLoai = 14, TrangThai = "Trống" },
                
                new Phong { MaPhong = "TN101", MaLoai = 15, TrangThai = "Trống" },
                new Phong { MaPhong = "TN102", MaLoai = 16, TrangThai = "Trống" },
                new Phong { MaPhong = "TN103", MaLoai = 17, TrangThai = "Trống" },
                new Phong { MaPhong = "TN104", MaLoai = 18, TrangThai = "Trống" },
                new Phong { MaPhong = "TN105", MaLoai = 19, TrangThai = "Trống" },
                
                new Phong { MaPhong = "DN101", MaLoai = 20, TrangThai = "Trống" },
                new Phong { MaPhong = "DN102", MaLoai = 21, TrangThai = "Trống" },
                new Phong { MaPhong = "DN103", MaLoai = 22, TrangThai = "Trống" },
                new Phong { MaPhong = "DN104", MaLoai = 23, TrangThai = "Trống" },
                new Phong { MaPhong = "DN105", MaLoai = 24, TrangThai = "Trống" }
            );

            // 6. DỊCH VỤ
            modelBuilder.Entity<DichVu>().HasData(
                new DichVu { MaDV = 1, TenDV = "Nước suối", GiaDV = 15000 },
                new DichVu { MaDV = 2, TenDV = "Bò húc", GiaDV = 25000 },
                new DichVu { MaDV = 3, TenDV = "Mì xào hải sản", GiaDV = 55000 },
                new DichVu { MaDV = 4, TenDV = "Spa Toàn Thân", GiaDV = 300000 },
                new DichVu { MaDV = 5, TenDV = "Giặt là cao cấp", GiaDV = 50000 }
            );

            // 7. CA LÀM VIỆC
            modelBuilder.Entity<CaLamViec>().HasData(
                new CaLamViec { MaCa = 1, TenCa = "Ca Sáng", GioBatDau = new TimeSpan(6, 0, 0), GioKetThuc = new TimeSpan(14, 0, 0), MoTa = "Lễ tân và phục vụ buổi sáng" },
                new CaLamViec { MaCa = 2, TenCa = "Ca Chiều", GioBatDau = new TimeSpan(14, 0, 0), GioKetThuc = new TimeSpan(22, 0, 0), MoTa = "Lễ tân và phục vụ buổi chiều tối" },
                new CaLamViec { MaCa = 3, TenCa = "Ca Đêm", GioBatDau = new TimeSpan(22, 0, 0), GioKetThuc = new TimeSpan(6, 0, 0), MoTa = "Trực đêm và bảo vệ" }
            );
        }
    }
}
