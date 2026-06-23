<?php
require_once 'config/database.php';
require_once 'mailer.php';
require_once 'include/header.php';

// Hàm helper tính toán doanh thu và voucher (Gom logic về một mối)
function tinhTongTien($conn, $row) {
    $ci = new DateTime($row['NgayCheckIn']);
    $co = new DateTime($row['NgayCheckOut']);
    $songay = max(1, $co->diff($ci)->days);
    $tong_tam = $songay * $row['GiaPhong'];
    
    // Giảm 10% nếu ở từ 2 đêm trở lên
    $giam_ngay = ($songay >= 2) ? round($tong_tam * 0.1) : 0;
    
    // Xử lý Voucher
    $giam_vc = 0; $ten_vc = '';
    $mavc = $row['MaVC'] ?? null;
    if (!empty($mavc)) {
        $vc_r = $conn->query("SELECT TenVoucher, LoaiGiam, GiaTriGiam, GiaTriToiThieu FROM Voucher WHERE MaVC = " . (int)$mavc);
        if ($vc_r && $vc_r->num_rows > 0) {
            $vc = $vc_r->fetch_assoc();
            if ($tong_tam >= (float)($vc['GiaTriToiThieu'] ?? 0)) {
                $giam_vc = ($vc['LoaiGiam'] == 'phantram') ? round($tong_tam * $vc['GiaTriGiam'] / 100) : (float)$vc['GiaTriGiam'];
                $ten_vc = $vc['TenVoucher'];
            }
        }
    }
    
    $tong_sau = max(0, $tong_tam - ($giam_ngay + $giam_vc));
    
    // Nếu đã có hóa đơn Online, ưu tiên lấy giá từ hóa đơn
    if (isset($row['MaDP'])) {
        $hd_res = $conn->query("SELECT TongTien FROM HoaDon WHERE MaDP = " . (int)$row['MaDP']);
        if ($hd_res && $hd_res->num_rows > 0) {
            $tong_sau = $hd_res->fetch_assoc()['TongTien'];
        }
    }

    return [$songay, $tong_tam, $giam_ngay, $giam_vc, $ten_vc, $tong_sau];
}

// Xử lý các Action (Duyệt / Hủy)
if (isset($_GET['action'], $_GET['id'])) {
    $id = (int)$_GET['id'];
    $act = $_GET['action'];

    if ($act == 'duyet') {
        $bk = $conn->query("SELECT dp.*, kh.HoTen, kh.Email, lp.TenLoai, lp.GiaPhong FROM DatPhong dp 
                            JOIN KhachHang kh ON dp.MaKH = kh.MaKH 
                            JOIN Phong p ON dp.MaPhong = p.MaPhong
                            JOIN LoaiPhong lp ON p.MaLoai = lp.MaLoai WHERE dp.MaDP = $id")->fetch_assoc();
        
        $conn->query("UPDATE DatPhong SET TrangThai = 'Đã xác nhận' WHERE MaDP = $id");
        
        if (!empty($bk['Email'])) {
            _logMail("Yêu cầu gửi mail Duyệt đơn từ xacnhan.php cho " . $bk['Email']);
            
            // Gọi hàm helper để lấy dữ liệu tính toán gọn gàng
            list($songay, $tong_tam, $giam_ngay, $giam_vc, $ten_vc, $tong_sau) = tinhTongTien($conn, $bk);

            $booking_data = [
                'ma_dat' => $bk['MaDP'],
                'ten_phong' => $bk['TenLoai'] . " (" . $bk['MaPhong'] . ")",
                'ngay_checkin' => $bk['NgayCheckIn'],
                'ngay_checkout' => $bk['NgayCheckOut'],
                'tong_tien' => $tong_sau,
                'gia_goc' => $tong_tam,
                'giam_gia' => ($giam_ngay + $giam_vc),
                'ten_voucher' => $ten_vc,
                'so_ngay' => $songay,
                'phuong_thuc' => ($bk['TrangThai'] == 'Đã thanh toán (Online)' ? 'Trực tuyến (Đã thanh toán)' : 'Trực tuyến (Chờ thanh toán tại quầy)')
            ];
            
            $success = sendBookingConfirmation($bk['Email'], $bk['HoTen'], $booking_data) 
                ? "Đã XÁC NHẬN đơn và ĐÃ gửi email thông báo cho khách hàng." 
                : "Đã XÁC NHẬN đơn nhưng KHÔNG gửi được email. Vui lòng kiểm tra cấu hình.";
        } else {
            $success = "Đã XÁC NHẬN đơn (Khách không có email).";
        }
    } elseif ($act == 'huy') {
        $bk = $conn->query("SELECT dp.MaDP, kh.HoTen, kh.Email FROM DatPhong dp JOIN KhachHang kh ON dp.MaKH = kh.MaKH WHERE dp.MaDP = $id")->fetch_assoc();
        $conn->query("UPDATE DatPhong SET TrangThai = 'Đã huỷ' WHERE MaDP = $id");
        
        if (!empty($bk['Email'])) {
            _logMail("Yêu cầu gửi mail Hủy đơn từ xacnhan.php cho " . $bk['Email']);
            sendCancellationEmail($bk['Email'], $bk['HoTen'], $bk['MaDP'], "Yêu cầu từ quản trị viên hoặc phòng không khả dụng.");
        }
        $success = "Đã HỦY đơn đặt phòng online.";
    }
}

// Lấy danh sách hiển thị
$result = $conn->query("SELECT dp.*, kh.HoTen, kh.SDT, kh.Email, p.MaPhong, lp.TenLoai, lp.GiaPhong,
        (SELECT COUNT(*) FROM DatPhong WHERE MaKH=dp.MaKH AND TrangThai IN ('Đã thanh toán','Đã thanh toán (Online)')) as SoLanTP
        FROM DatPhong dp 
        JOIN KhachHang kh ON dp.MaKH = kh.MaKH 
        JOIN Phong p ON dp.MaPhong = p.MaPhong 
        JOIN LoaiPhong lp ON p.MaLoai = lp.MaLoai 
        WHERE dp.NguonDat = 'BanOnline' AND dp.TrangThai IN ('Chờ xác nhận', 'Đã thanh toán (Online)') 
        ORDER BY dp.NgayCheckIn ASC");
?>

<div class="row mb-4">
    <div class="col-12">
        <h2 class="fw-bold m-0"><i class="fa-solid fa-globe text-primary me-2"></i> Duyệt Đơn Đặt Phòng Trực Tuyến</h2>
        <p class="text-muted">Các đơn khách hàng tự đặt từ Landing Page sẽ hiển thị tại đây chờ Lễ tân xác nhận.</p>
    </div>
</div>

<?php if(isset($success)): ?>
    <div class="alert alert-success fw-bold shadow-sm"><i class="fa-solid fa-check w-20px"></i> <?= $success ?></div>
<?php endif; ?>

<div class="card p-4 border-0 shadow-sm">
    <div class="table-responsive">
        <table class="table table-hover align-middle">
            <thead class="table-light">
                <tr>
                    <th>Mã Đơn</th><th>Khách Hàng</th><th>Liên Hệ</th><th>Phòng Đặt</th><th>Check-in / Check-out</th><th>Số Đêm</th><th class="text-end text-success">Dự Ước</th><th>Ghi Chú</th><th class="text-end">Thao Tác</th>
                </tr>
            </thead>
            <tbody>
                <?php while ($row = $result->fetch_assoc()): 
                    // Tái sử dụng hàm helper
                    list($songay, $tong_tam, $giam_ngay, $giam_vc, $ten_vc, $tong_sau) = tinhTongTien($conn, $row);
                    $dotColor = ($row['TrangThai'] == 'Chờ xác nhận') ? '#f59e0b' : (($row['TrangThai'] == 'Đã thanh toán (Online)') ? '#22c55e' : '#94a3b8');
                ?>
                <tr>
                    <td class="fw-bold text-dark">#ONL<?= $row['MaDP'] ?>
                        <br><span class="badge" style="font-size:.65rem;background:<?= $dotColor ?>;"><?= $row['TrangThai'] ?></span>
                    </td>
                    <td class="fw-bold text-primary"><?= htmlspecialchars($row['HoTen']) ?>
                        <?php if($row['SoLanTP'] >= 2): ?><br><span class="badge bg-warning text-dark" style="font-size:.65rem;"><i class="fa-solid fa-crown me-1"></i>VIP</span><?php endif; ?>
                    </td>
                    <td>
                        <small class="d-block"><i class="fa-solid fa-phone text-secondary w-15px"></i> <?= $row['SDT'] ?></small>
                        <small class="d-block"><i class="fa-solid fa-envelope text-secondary w-15px"></i> <?= $row['Email'] ?? 'N/A' ?></small>
                    </td>
                    <td>
                        <span class="fw-bold"><?= $row['TenLoai'] ?> (<?= $row['MaPhong'] ?>)</span>
                        <small class="d-block text-success"><?= number_format($row['GiaPhong']) ?>đ/đêm</small>
                    </td>
                    <td>
                        <span class="badge bg-primary"><?= date('d/m/Y', strtotime($row['NgayCheckIn'])) ?></span>
                        <i class="fa-solid fa-arrow-right small text-muted mx-1"></i>
                        <span class="badge bg-secondary"><?= date('d/m/Y', strtotime($row['NgayCheckOut'])) ?></span>
                    </td>
                    <td class="text-center fw-bold"><?= $songay ?> đêm</td>
                    <td class="text-end">
                        <div class="fw-bold text-danger"><?= number_format($tong_sau) ?>đ</div>
                        <?php if ($giam_ngay > 0): ?>
                            <small class="text-muted text-decoration-line-through"><?= number_format($tong_tam) ?>đ</small><br>
                            <small class="text-success"><i class="fa-solid fa-moon me-1"></i><?= $songay ?> đêm: -<?= number_format($giam_ngay) ?>đ</small><br>
                        <?php endif; ?>
                        <?php if ($giam_vc > 0): ?>
                            <small class="text-primary d-block"><i class="fa-solid fa-tag me-1"></i><?= htmlspecialchars($ten_vc) ?>: -<?= number_format($giam_vc) ?>đ</small>
                        <?php endif; ?>
                    </td>
                    <td><small class="text-muted"><?= $row['GhiChu'] ? htmlspecialchars($row['GhiChu']) : 'Không' ?></small></td>
                    <td class="text-end">
                        <a href="?action=duyet&id=<?= $row['MaDP'] ?>" class="btn btn-sm btn-success fw-bold shadow-sm" onclick="return confirm('Duyệt đơn và giữ phòng cho khách này?')"><i class="fa-solid fa-check"></i> Chấp nhận</a>
                        <a href="?action=huy&id=<?= $row['MaDP'] ?>" class="btn btn-sm btn-outline-danger shadow-sm ms-1" onclick="return confirm('Từ chối đơn này?')"><i class="fa-solid fa-xmark"></i></a>
                    </td>
                </tr>
                <?php endwhile; ?>
                <?php if($result->num_rows == 0): ?>
                    <tr><td colspan="9" class="text-center text-muted py-5"><i class="fa-solid fa-mug-hot fs-3 mb-2"></i><br>Không có đơn trực tuyến nào đang chờ duyệt.</td></tr>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<?php require_once 'include/footer.php'; ?>
