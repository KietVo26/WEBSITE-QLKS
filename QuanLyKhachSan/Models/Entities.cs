using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models
{
    [Table("TaiKhoan")]
    public class TaiKhoan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaTK { get; set; }

        [Required]
        [StringLength(50)]
        public string TenDangNhap { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string MatKhau { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; } = null!;

        [StringLength(20)]
        public string VaiTro { get; set; } = "khach"; // 'khach', 'nhanvien', 'admin'
    }

    [Table("NhanVien")]
    public class NhanVien
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaNV { get; set; }

        public int? MaTK { get; set; }

        [ForeignKey("MaTK")]
        public TaiKhoan? TaiKhoan { get; set; }

        [StringLength(50)]
        public string? ChucVu { get; set; }

        [StringLength(50)]
        public string? CaLamViec { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal Luong { get; set; }

        [StringLength(20)]
        public string? SDT { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [Column(TypeName = "date")]
        public DateTime? NgayVaoLam { get; set; }
    }

    [Table("KhachHang")]
    public class KhachHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaKH { get; set; }

        public int? MaTK { get; set; }

        [ForeignKey("MaTK")]
        public TaiKhoan? TaiKhoan { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string CCCD { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string SDT { get; set; } = null!;

        [StringLength(100)]
        public string? Email { get; set; }

        public int DiemTichLuy { get; set; } = 0;

        [StringLength(20)]
        public string HangThanhVien { get; set; } = "Đồng"; // 'Đồng', 'Bạc', 'Vàng'
    }

    [Table("LoaiPhong")]
    public class LoaiPhong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaLoai { get; set; }

        [Required]
        [StringLength(50)]
        public string TenLoai { get; set; } = null!;

        [StringLength(100)]
        public string KhuVuc { get; set; } = "Hồ Chí Minh";

        [Column(TypeName = "decimal(15,2)")]
        public decimal GiaPhong { get; set; }

        public int SoNguoiToiDa { get; set; } = 2;

        public string? TienNghi { get; set; }

        [StringLength(255)]
        public string HinhAnh { get; set; } = "default_room.jpg";

        public string? TuKhoa { get; set; }
    }

    [Table("Phong")]
    public class Phong
    {
        [Key]
        [StringLength(10)]
        public string MaPhong { get; set; } = null!;

        [Required]
        public int MaLoai { get; set; }

        [ForeignKey("MaLoai")]
        public LoaiPhong LoaiPhong { get; set; } = null!;

        [StringLength(20)]
        public string TrangThai { get; set; } = "Trống"; // 'Trống', 'Đang ở', 'Đang dọn dẹp'
    }

    [Table("DichVu")]
    public class DichVu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDV { get; set; }

        [Required]
        [StringLength(100)]
        public string TenDV { get; set; } = null!;

        [Column(TypeName = "decimal(15,2)")]
        public decimal GiaDV { get; set; }
    }

    [Table("DatPhong")]
    public class DatPhong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDP { get; set; }

        [Required]
        public int MaKH { get; set; }

        [ForeignKey("MaKH")]
        public KhachHang KhachHang { get; set; } = null!;

        [Required]
        [StringLength(10)]
        public string MaPhong { get; set; } = null!;

        [ForeignKey("MaPhong")]
        public Phong Phong { get; set; } = null!;

        [Required]
        public DateTime NgayCheckIn { get; set; }

        public DateTime? NgayCheckOut { get; set; }

        public string? GhiChu { get; set; }

        [StringLength(20)]
        public string TrangThai { get; set; } = "Chờ xác nhận"; // 'Chờ xác nhận', 'Đã xác nhận', 'Đang ở', 'Đã thanh toán', 'Đã huỷ'

        [StringLength(20)]
        public string NguonDat { get; set; } = "Trực tiếp"; // 'Trực tiếp', 'BanOnline'

        public ICollection<SuDungDichVu> SuDungDichVus { get; set; } = new List<SuDungDichVu>();
    }

    [Table("SuDungDichVu")]
    public class SuDungDichVu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaSD { get; set; }

        [Required]
        public int MaDP { get; set; }

        [ForeignKey("MaDP")]
        public DatPhong DatPhong { get; set; } = null!;

        [Required]
        public int MaDV { get; set; }

        [ForeignKey("MaDV")]
        public DichVu DichVu { get; set; } = null!;

        public int SoLuong { get; set; } = 1;

        [Column(TypeName = "decimal(15,2)")]
        public decimal ThanhTien { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }

    [Table("DanhGia")]
    public class DanhGia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDG { get; set; }

        [Required]
        public int MaKH { get; set; }

        [ForeignKey("MaKH")]
        public KhachHang KhachHang { get; set; } = null!;

        [Required]
        public int MaLoai { get; set; }

        [ForeignKey("MaLoai")]
        public LoaiPhong LoaiPhong { get; set; } = null!;

        public int? MaDP { get; set; }

        [ForeignKey("MaDP")]
        public DatPhong? DatPhong { get; set; }

        public int SoSao { get; set; } = 5;

        public string? NhanXet { get; set; }

        public DateTime NgayDanhGia { get; set; } = DateTime.Now;
    }

    [Table("HoaDon")]
    public class HoaDon
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaHD { get; set; }

        [Required]
        public int MaDP { get; set; }

        [ForeignKey("MaDP")]
        public DatPhong DatPhong { get; set; } = null!;

        public int? MaNV { get; set; }

        [ForeignKey("MaNV")]
        public NhanVien? NhanVien { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal TienPhong { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal TienDichVu { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal TongTien { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal GiamGiaThanhVien { get; set; } = 0;

        public DateTime NgayLanhToan { get; set; } = DateTime.Now;
    }

    [Table("Voucher")]
    public class Voucher
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaVC { get; set; }

        public int? MaKH { get; set; }

        [ForeignKey("MaKH")]
        public KhachHang? KhachHang { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string TenVoucher { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string LoaiGiam { get; set; } = "phantram"; // 'phantram', 'sotien'

        [Column(TypeName = "decimal(10,2)")]
        public decimal GiaTriGiam { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal GiaTriToiThieu { get; set; } = 0;

        [Column(TypeName = "date")]
        public DateTime? NgayBatDau { get; set; }

        [Column(TypeName = "date")]
        public DateTime NgayHetHan { get; set; }

        public int GioiHanDung { get; set; } = 1;

        public int SoLanDaDung { get; set; } = 0;

        [StringLength(20)]
        public string TrangThai { get; set; } = "active"; // 'active', 'inactive'

        [StringLength(100)]
        public string? KhuVucApDung { get; set; }

        public string? GhiChu { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }

    [Table("CaLamViec")]
    public class CaLamViec
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaCa { get; set; }

        [Required]
        [StringLength(50)]
        public string TenCa { get; set; } = null!;

        public TimeSpan GioBatDau { get; set; }

        public TimeSpan GioKetThuc { get; set; }

        [StringLength(255)]
        public string? MoTa { get; set; }
    }

    [Table("LichLamViec")]
    public class LichLamViec
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaLich { get; set; }

        [Required]
        public int MaNV { get; set; }

        [ForeignKey("MaNV")]
        public NhanVien NhanVien { get; set; } = null!;

        [Required]
        public int MaCa { get; set; }

        [ForeignKey("MaCa")]
        public CaLamViec CaLamViec { get; set; } = null!;

        [Column(TypeName = "date")]
        public DateTime NgayLam { get; set; }

        [StringLength(255)]
        public string? GhiChu { get; set; }
    }

    [Table("ChamCong")]
    public class ChamCong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaCC { get; set; }

        [Required]
        public int MaNV { get; set; }

        [ForeignKey("MaNV")]
        public NhanVien NhanVien { get; set; } = null!;

        public int? MaLich { get; set; }

        [ForeignKey("MaLich")]
        public LichLamViec? LichLamViec { get; set; }

        [Column(TypeName = "date")]
        public DateTime NgayCC { get; set; }

        public TimeSpan? GioVao { get; set; }

        public TimeSpan? GioRa { get; set; }

        [StringLength(20)]
        public string TrangThai { get; set; } = "dung_gio"; // 'dung_gio', 'tre', 'vang_mat', 'nghi_phep'

        public string? GhiChu { get; set; }
    }

    [Table("ThuongPhat")]
    public class ThuongPhat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaTP { get; set; }

        [Required]
        public int MaNV { get; set; }

        [ForeignKey("MaNV")]
        public NhanVien NhanVien { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string Loai { get; set; } = null!; // 'thuong', 'phat'

        [Column(TypeName = "decimal(15,2)")]
        public decimal SoTien { get; set; }

        [Required]
        [StringLength(255)]
        public string LyDo { get; set; } = null!;

        [Column(TypeName = "date")]
        public DateTime Ngay { get; set; }

        public string? GhiChu { get; set; }
    }

    [Table("DanhGiaNhanVien")]
    public class DanhGiaNhanVien
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDGNV { get; set; }

        [Required]
        public int MaKH { get; set; }

        [ForeignKey("MaKH")]
        public KhachHang KhachHang { get; set; } = null!;

        [Required]
        public int MaNV { get; set; }

        [ForeignKey("MaNV")]
        public NhanVien NhanVien { get; set; } = null!;

        public int SoSao { get; set; } = 5;

        public string? NhanXet { get; set; }

        public DateTime NgayDanhGia { get; set; } = DateTime.Now;
    }

    [Table("PasswordResetTokens")]
    public class PasswordResetToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaToken { get; set; }

        [Required]
        public int MaTK { get; set; }

        [ForeignKey("MaTK")]
        public TaiKhoan TaiKhoan { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string Token { get; set; } = null!;

        public DateTime ThoiGianHetHan { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
