using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLyKhachSan.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaLamViec",
                columns: table => new
                {
                    MaCa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenCa = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GioBatDau = table.Column<TimeSpan>(type: "time", nullable: false),
                    GioKetThuc = table.Column<TimeSpan>(type: "time", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaLamViec", x => x.MaCa);
                });

            migrationBuilder.CreateTable(
                name: "DichVu",
                columns: table => new
                {
                    MaDV = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDV = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GiaDV = table.Column<decimal>(type: "decimal(15,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DichVu", x => x.MaDV);
                });

            migrationBuilder.CreateTable(
                name: "LoaiPhong",
                columns: table => new
                {
                    MaLoai = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KhuVuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GiaPhong = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    SoNguoiToiDa = table.Column<int>(type: "int", nullable: false),
                    TienNghi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HinhAnh = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TuKhoa = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiPhong", x => x.MaLoai);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    MaTK = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.MaTK);
                });

            migrationBuilder.CreateTable(
                name: "Phong",
                columns: table => new
                {
                    MaPhong = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MaLoai = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phong", x => x.MaPhong);
                    table.ForeignKey(
                        name: "FK_Phong_LoaiPhong_MaLoai",
                        column: x => x.MaLoai,
                        principalTable: "LoaiPhong",
                        principalColumn: "MaLoai",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KhachHang",
                columns: table => new
                {
                    MaKH = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTK = table.Column<int>(type: "int", nullable: true),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CCCD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SDT = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiemTichLuy = table.Column<int>(type: "int", nullable: false),
                    HangThanhVien = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHang", x => x.MaKH);
                    table.ForeignKey(
                        name: "FK_KhachHang_TaiKhoan_MaTK",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NhanVien",
                columns: table => new
                {
                    MaNV = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTK = table.Column<int>(type: "int", nullable: true),
                    ChucVu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CaLamViec = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Luong = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    SDT = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayVaoLam = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanVien", x => x.MaNV);
                    table.ForeignKey(
                        name: "FK_NhanVien_TaiKhoan_MaTK",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    MaToken = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTK = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ThoiGianHetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.MaToken);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_TaiKhoan_MaTK",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatPhong",
                columns: table => new
                {
                    MaDP = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKH = table.Column<int>(type: "int", nullable: false),
                    MaPhong = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NgayCheckIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NguonDat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatPhong", x => x.MaDP);
                    table.ForeignKey(
                        name: "FK_DatPhong_KhachHang_MaKH",
                        column: x => x.MaKH,
                        principalTable: "KhachHang",
                        principalColumn: "MaKH",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DatPhong_Phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "Phong",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Voucher",
                columns: table => new
                {
                    MaVC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKH = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenVoucher = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LoaiGiam = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GiaTriGiam = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    GiaTriToiThieu = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "date", nullable: true),
                    NgayHetHan = table.Column<DateTime>(type: "date", nullable: false),
                    GioiHanDung = table.Column<int>(type: "int", nullable: false),
                    SoLanDaDung = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voucher", x => x.MaVC);
                    table.ForeignKey(
                        name: "FK_Voucher_KhachHang_MaKH",
                        column: x => x.MaKH,
                        principalTable: "KhachHang",
                        principalColumn: "MaKH",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DanhGiaNhanVien",
                columns: table => new
                {
                    MaDGNV = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKH = table.Column<int>(type: "int", nullable: false),
                    MaNV = table.Column<int>(type: "int", nullable: false),
                    SoSao = table.Column<int>(type: "int", nullable: false),
                    NhanXet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGiaNhanVien", x => x.MaDGNV);
                    table.ForeignKey(
                        name: "FK_DanhGiaNhanVien_KhachHang_MaKH",
                        column: x => x.MaKH,
                        principalTable: "KhachHang",
                        principalColumn: "MaKH",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanhGiaNhanVien_NhanVien_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichLamViec",
                columns: table => new
                {
                    MaLich = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNV = table.Column<int>(type: "int", nullable: false),
                    MaCa = table.Column<int>(type: "int", nullable: false),
                    NgayLam = table.Column<DateTime>(type: "date", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichLamViec", x => x.MaLich);
                    table.ForeignKey(
                        name: "FK_LichLamViec_CaLamViec_MaCa",
                        column: x => x.MaCa,
                        principalTable: "CaLamViec",
                        principalColumn: "MaCa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichLamViec_NhanVien_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThuongPhat",
                columns: table => new
                {
                    MaTP = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNV = table.Column<int>(type: "int", nullable: false),
                    Loai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Ngay = table.Column<DateTime>(type: "date", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThuongPhat", x => x.MaTP);
                    table.ForeignKey(
                        name: "FK_ThuongPhat_NhanVien_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanhGia",
                columns: table => new
                {
                    MaDG = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKH = table.Column<int>(type: "int", nullable: false),
                    MaLoai = table.Column<int>(type: "int", nullable: false),
                    MaDP = table.Column<int>(type: "int", nullable: true),
                    SoSao = table.Column<int>(type: "int", nullable: false),
                    NhanXet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGia", x => x.MaDG);
                    table.ForeignKey(
                        name: "FK_DanhGia_DatPhong_MaDP",
                        column: x => x.MaDP,
                        principalTable: "DatPhong",
                        principalColumn: "MaDP",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DanhGia_KhachHang_MaKH",
                        column: x => x.MaKH,
                        principalTable: "KhachHang",
                        principalColumn: "MaKH",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanhGia_LoaiPhong_MaLoai",
                        column: x => x.MaLoai,
                        principalTable: "LoaiPhong",
                        principalColumn: "MaLoai",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoaDon",
                columns: table => new
                {
                    MaHD = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDP = table.Column<int>(type: "int", nullable: false),
                    MaNV = table.Column<int>(type: "int", nullable: true),
                    TienPhong = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    TienDichVu = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    GiamGiaThanhVien = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    NgayLanhToan = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDon", x => x.MaHD);
                    table.ForeignKey(
                        name: "FK_HoaDon_DatPhong_MaDP",
                        column: x => x.MaDP,
                        principalTable: "DatPhong",
                        principalColumn: "MaDP",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HoaDon_NhanVien_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SuDungDichVu",
                columns: table => new
                {
                    MaSD = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDP = table.Column<int>(type: "int", nullable: false),
                    MaDV = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuDungDichVu", x => x.MaSD);
                    table.ForeignKey(
                        name: "FK_SuDungDichVu_DatPhong_MaDP",
                        column: x => x.MaDP,
                        principalTable: "DatPhong",
                        principalColumn: "MaDP",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuDungDichVu_DichVu_MaDV",
                        column: x => x.MaDV,
                        principalTable: "DichVu",
                        principalColumn: "MaDV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChamCong",
                columns: table => new
                {
                    MaCC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNV = table.Column<int>(type: "int", nullable: false),
                    MaLich = table.Column<int>(type: "int", nullable: true),
                    NgayCC = table.Column<DateTime>(type: "date", nullable: false),
                    GioVao = table.Column<TimeSpan>(type: "time", nullable: true),
                    GioRa = table.Column<TimeSpan>(type: "time", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChamCong", x => x.MaCC);
                    table.ForeignKey(
                        name: "FK_ChamCong_LichLamViec_MaLich",
                        column: x => x.MaLich,
                        principalTable: "LichLamViec",
                        principalColumn: "MaLich",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChamCong_NhanVien_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CaLamViec",
                columns: new[] { "MaCa", "GioBatDau", "GioKetThuc", "MoTa", "TenCa" },
                values: new object[,]
                {
                    { 1, new TimeSpan(0, 6, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), "Lễ tân và phục vụ buổi sáng", "Ca Sáng" },
                    { 2, new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 22, 0, 0, 0), "Lễ tân và phục vụ buổi chiều tối", "Ca Chiều" },
                    { 3, new TimeSpan(0, 22, 0, 0, 0), new TimeSpan(0, 6, 0, 0, 0), "Trực đêm và bảo vệ", "Ca Đêm" }
                });

            migrationBuilder.InsertData(
                table: "DichVu",
                columns: new[] { "MaDV", "GiaDV", "TenDV" },
                values: new object[,]
                {
                    { 1, 15000m, "Nước suối" },
                    { 2, 25000m, "Bò húc" },
                    { 3, 55000m, "Mì xào hải sản" },
                    { 4, 300000m, "Spa Toàn Thân" },
                    { 5, 50000m, "Giặt là cao cấp" }
                });

            migrationBuilder.InsertData(
                table: "KhachHang",
                columns: new[] { "MaKH", "CCCD", "DiemTichLuy", "Email", "HangThanhVien", "HoTen", "MaTK", "SDT" },
                values: new object[] { 1, "045612378945", 0, "ltd@gmail.com", "Đồng", "Lê Thị D", null, "0933456789" });

            migrationBuilder.InsertData(
                table: "LoaiPhong",
                columns: new[] { "MaLoai", "GiaPhong", "HinhAnh", "KhuVuc", "SoNguoiToiDa", "TenLoai", "TienNghi", "TuKhoa" },
                values: new object[,]
                {
                    { 1, 300000m, "default_room.jpg", "Hồ Chí Minh", 1, "Phòng Đơn Tiêu Chuẩn", "Tivi, Máy Lạnh, Cửa Sổ Nhỏ", "thành phố, phố, trung tâm, sài gòn" },
                    { 2, 500000m, "default_room.jpg", "Hồ Chí Minh", 2, "Phòng Đôi Tiêu Chuẩn", "Tivi, Tủ Lạnh, Cửa Sổ Hướng Phố", "thành phố, phố, trung tâm, sài gòn" },
                    { 3, 1200000m, "default_room.jpg", "Hồ Chí Minh", 2, "Phòng VIP Hướng Biển", "Tivi OLED, Bồn tắm, Ban công siêu rộng, View Biển trực tiếp", "biển, resort, nghỉ dưỡng" },
                    { 4, 850000m, "default_room.jpg", "Hồ Chí Minh", 4, "Phòng Gia Đình", "2 Giường Đôi cỡ lớn, Không gian sinh hoạt, Trò chơi điện tử", "gia đình, rộng" },
                    { 5, 600000m, "default_room.jpg", "Phan Thiết", 2, "Phòng Biển Standard", "View biển, Máy lạnh, Tivi, Ban công", "biển, beach, phan thiết, nghỉ dưỡng" },
                    { 6, 900000m, "default_room.jpg", "Phan Thiết", 2, "Phòng Biển Deluxe", "View biển trực diện, Bồn tắm, Ban công rộng, Minibar", "biển, beach, phan thiết, view" },
                    { 7, 1500000m, "default_room.jpg", "Phan Thiết", 3, "Phòng Biển Suite", "View biển panorama, Phòng khách riêng, Jacuzzi", "biển, beach, phan thiết, suite" },
                    { 8, 1200000m, "default_room.jpg", "Phan Thiết", 5, "Phòng Biển Gia Đình", "2 Phòng ngủ, View biển, Bếp nhỏ, Sân vườn riêng", "biển, beach, phan thiết, gia đình" },
                    { 9, 2500000m, "default_room.jpg", "Phan Thiết", 4, "Bungalow Biển VIP", "Bungalow riêng, Hồ bơi riêng, View biển 360°", "biển, beach, phan thiết, vip, bungalow" },
                    { 10, 450000m, "default_room.jpg", "Hà Nội", 2, "Phòng Phố Cổ Classic", "View phố cổ, Nội thất gỗ, Máy lạnh, Tivi", "hà nội, phố cổ, văn hóa, hanoi" },
                    { 11, 800000m, "default_room.jpg", "Hà Nội", 2, "Phòng Hồ Gươm View", "View Hồ Gươm, Ban công, Bồn tắm, Minibar", "hà nội, hồ gươm, view, hanoi" },
                    { 12, 1400000m, "default_room.jpg", "Hà Nội", 3, "Phòng Heritage Suite", "Nội thất Đông Dương, Phòng khách rộng, Spa trong phòng", "hà nội, di sản, heritage, hanoi" },
                    { 13, 1000000m, "default_room.jpg", "Hà Nội", 5, "Phòng Gia Đình Hà Nội", "2 Giường đôi, Phòng rộng, View thành phố", "hà nội, gia đình, hanoi" },
                    { 14, 3000000m, "default_room.jpg", "Hà Nội", 4, "Penthouse Royal Hà Nội", "Tầng thượng, View toàn thành phố, Hồ bơi trên mái", "hà nội, penthouse, royal, vip, hanoi" },
                    { 15, 400000m, "default_room.jpg", "Tây Ninh", 2, "Phòng Núi Standard", "View núi Bà Đen, Máy lạnh, Tivi, Không gian thoáng", "núi, mountain, tây ninh, thiên nhiên" },
                    { 16, 700000m, "default_room.jpg", "Tây Ninh", 2, "Phòng Núi Deluxe", "View núi panorama, Ban công rộng, Võng thư giãn, Trà đạo", "núi, mountain, tây ninh, thư giãn" },
                    { 17, 550000m, "default_room.jpg", "Tây Ninh", 2, "Phòng Rừng Xanh Eco", "Eco-lodge, Gần rừng, Xe đạp miễn phí, BBQ", "núi, mountain, tây ninh, eco, rừng" },
                    { 18, 900000m, "default_room.jpg", "Tây Ninh", 5, "Phòng Gia Đình Núi", "2 Phòng ngủ, View núi, Sân vườn rộng, Lò sưởi", "núi, mountain, tây ninh, gia đình" },
                    { 19, 2000000m, "default_room.jpg", "Tây Ninh", 6, "Villa Đỉnh Núi VIP", "Villa riêng trên đồi, Hồ bơi vô cực, View 360° núi rừng", "núi, mountain, tây ninh, villa, vip" },
                    { 20, 650000m, "default_room.jpg", "Đà Nẵng", 2, "Phòng Biển Mỹ Khê", "View biển Mỹ Khê, Máy lạnh, Tivi, Ban công", "đà nẵng, biển, mỹ khê, beach, miền trung, danang" },
                    { 21, 950000m, "default_room.jpg", "Đà Nẵng", 2, "Phòng Sơn Trà View", "View bán đảo Sơn Trà, Bồn tắm, Ban công rộng", "đà nẵng, sơn trà, view, miền trung, danang" },
                    { 22, 1600000m, "default_room.jpg", "Đà Nẵng", 3, "Phòng Suite Hàn River", "View sông Hàn, Phòng khách riêng, Minibar cao cấp", "đà nẵng, sông hàn, suite, sang trọng, danang" },
                    { 23, 1100000m, "default_room.jpg", "Đà Nẵng", 5, "Phòng Gia Đình Đà Nẵng", "2 Phòng ngủ, View biển, Bếp nhỏ, Khu vui chơi", "đà nẵng, gia đình, rộng, miền trung, danang" },
                    { 24, 2800000m, "default_room.jpg", "Đà Nẵng", 4, "Penthouse Ngũ Hành Sơn", "Tầng cao nhất, View Ngũ Hành Sơn, Hồ bơi riêng", "đà nẵng, ngũ hành sơn, penthouse, vip, danang" }
                });

            migrationBuilder.InsertData(
                table: "TaiKhoan",
                columns: new[] { "MaTK", "HoTen", "MatKhau", "TenDangNhap", "VaiTro" },
                values: new object[,]
                {
                    { 1, "Quản trị viên", "admin", "admin", "admin" },
                    { 2, "Nguyễn Thị Lân", "lan123", "letanlan", "nhanvien" },
                    { 3, "Trần Văn Minh", "minh123", "minhtrv", "nhanvien" },
                    { 4, "Lê Thị Hoa", "hoa123", "hoale", "nhanvien" },
                    { 5, "Phạm Văn Hùng", "hung123", "hungpv", "nhanvien" },
                    { 6, "Võ Thị Mai", "mai123", "maivo", "nhanvien" }
                });

            migrationBuilder.InsertData(
                table: "NhanVien",
                columns: new[] { "MaNV", "CaLamViec", "ChucVu", "Email", "Luong", "MaTK", "NgayVaoLam", "SDT" },
                values: new object[,]
                {
                    { 1, "Hành chính", "Quản lý", null, 20000000m, 1, null, null },
                    { 2, "Ca Sáng", "Lễ tân", "lan@gmail.com", 8500000m, 2, new DateTime(2024, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "0901111222" },
                    { 3, "Ca Chiều", "Lễ tân", "minh@gmail.com", 8000000m, 3, new DateTime(2024, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "0902222333" },
                    { 4, "Ca Sáng", "Thu ngân", "hoa@gmail.com", 9000000m, 4, new DateTime(2023, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "0903333444" },
                    { 5, "Ca Đêm", "Bảo vệ", "hung@gmail.com", 7500000m, 5, new DateTime(2024, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "0904444555" },
                    { 6, "Ca Chiều", "Dọn phòng", "mai@gmail.com", 7000000m, 6, new DateTime(2024, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "0905555666" }
                });

            migrationBuilder.InsertData(
                table: "Phong",
                columns: new[] { "MaPhong", "MaLoai", "TrangThai" },
                values: new object[,]
                {
                    { "DN101", 20, "Trống" },
                    { "DN102", 21, "Trống" },
                    { "DN103", 22, "Trống" },
                    { "DN104", 23, "Trống" },
                    { "DN105", 24, "Trống" },
                    { "HN101", 10, "Trống" },
                    { "HN102", 11, "Trống" },
                    { "HN103", 12, "Trống" },
                    { "HN104", 13, "Trống" },
                    { "HN105", 14, "Trống" },
                    { "P101", 1, "Trống" },
                    { "P102", 1, "Trống" },
                    { "P201", 2, "Trống" },
                    { "P202", 2, "Trống" },
                    { "P301", 3, "Trống" },
                    { "P302", 3, "Trống" },
                    { "P401", 4, "Trống" },
                    { "PT101", 5, "Trống" },
                    { "PT102", 6, "Trống" },
                    { "PT103", 7, "Trống" },
                    { "PT104", 8, "Trống" },
                    { "PT105", 9, "Trống" },
                    { "TN101", 15, "Trống" },
                    { "TN102", 16, "Trống" },
                    { "TN103", 17, "Trống" },
                    { "TN104", 18, "Trống" },
                    { "TN105", 19, "Trống" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChamCong_MaLich",
                table: "ChamCong",
                column: "MaLich");

            migrationBuilder.CreateIndex(
                name: "IX_ChamCong_MaNV",
                table: "ChamCong",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_MaDP",
                table: "DanhGia",
                column: "MaDP");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_MaKH",
                table: "DanhGia",
                column: "MaKH");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_MaLoai",
                table: "DanhGia",
                column: "MaLoai");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaNhanVien_MaKH",
                table: "DanhGiaNhanVien",
                column: "MaKH");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaNhanVien_MaNV",
                table: "DanhGiaNhanVien",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_DatPhong_MaKH",
                table: "DatPhong",
                column: "MaKH");

            migrationBuilder.CreateIndex(
                name: "IX_DatPhong_MaPhong",
                table: "DatPhong",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_MaDP",
                table: "HoaDon",
                column: "MaDP");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_MaNV",
                table: "HoaDon",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_MaTK",
                table: "KhachHang",
                column: "MaTK",
                unique: true,
                filter: "[MaTK] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LichLamViec_MaCa",
                table: "LichLamViec",
                column: "MaCa");

            migrationBuilder.CreateIndex(
                name: "IX_LichLamViec_MaNV",
                table: "LichLamViec",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_MaTK",
                table: "NhanVien",
                column: "MaTK",
                unique: true,
                filter: "[MaTK] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_MaTK",
                table: "PasswordResetTokens",
                column: "MaTK");

            migrationBuilder.CreateIndex(
                name: "IX_Phong_MaLoai",
                table: "Phong",
                column: "MaLoai");

            migrationBuilder.CreateIndex(
                name: "IX_SuDungDichVu_MaDP",
                table: "SuDungDichVu",
                column: "MaDP");

            migrationBuilder.CreateIndex(
                name: "IX_SuDungDichVu_MaDV",
                table: "SuDungDichVu",
                column: "MaDV");

            migrationBuilder.CreateIndex(
                name: "IX_ThuongPhat_MaNV",
                table: "ThuongPhat",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_MaKH",
                table: "Voucher",
                column: "MaKH");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChamCong");

            migrationBuilder.DropTable(
                name: "DanhGia");

            migrationBuilder.DropTable(
                name: "DanhGiaNhanVien");

            migrationBuilder.DropTable(
                name: "HoaDon");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "SuDungDichVu");

            migrationBuilder.DropTable(
                name: "ThuongPhat");

            migrationBuilder.DropTable(
                name: "Voucher");

            migrationBuilder.DropTable(
                name: "LichLamViec");

            migrationBuilder.DropTable(
                name: "DatPhong");

            migrationBuilder.DropTable(
                name: "DichVu");

            migrationBuilder.DropTable(
                name: "CaLamViec");

            migrationBuilder.DropTable(
                name: "NhanVien");

            migrationBuilder.DropTable(
                name: "KhachHang");

            migrationBuilder.DropTable(
                name: "Phong");

            migrationBuilder.DropTable(
                name: "TaiKhoan");

            migrationBuilder.DropTable(
                name: "LoaiPhong");
        }
    }
}
