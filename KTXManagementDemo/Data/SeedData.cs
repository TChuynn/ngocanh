using KTXManagement.Demo.Models;
using KTXManagement.Demo.Services;

namespace KTXManagement.Demo.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext db)
        {
            db.Database.EnsureCreated();

            if (db.TaiKhoans.Any()) return; // đã seed rồi thì bỏ qua

            // ===== 1. Tài khoản (UC001) =====
            var tkQuanLy = new TaiKhoan
            {
                TenDangNhap = "quanly",
                MatKhauHash = PasswordHasher.Hash("123456"),
                HoTen = "Nguyễn Văn Quản",
                Email = "quanly@ktx.edu.vn",
                VaiTro = VaiTro.QuanLyKTX,
                TrangThai = TrangThaiTaiKhoan.HoatDong
            };
            var tkKeToan = new TaiKhoan
            {
                TenDangNhap = "ketoan",
                MatKhauHash = PasswordHasher.Hash("123456"),
                HoTen = "Trần Thị Kế",
                Email = "ketoan@ktx.edu.vn",
                VaiTro = VaiTro.KeToan,
                TrangThai = TrangThaiTaiKhoan.HoatDong
            };

            var tkSV1 = new TaiKhoan { TenDangNhap = "sv001", MatKhauHash = PasswordHasher.Hash("123456"), HoTen = "Nguyễn Văn A", Email = "sv001@sv.edu.vn", VaiTro = VaiTro.SinhVien };
            var tkSV2 = new TaiKhoan { TenDangNhap = "sv002", MatKhauHash = PasswordHasher.Hash("123456"), HoTen = "Trần Thị B", Email = "sv002@sv.edu.vn", VaiTro = VaiTro.SinhVien };
            var tkSV3 = new TaiKhoan { TenDangNhap = "sv003", MatKhauHash = PasswordHasher.Hash("123456"), HoTen = "Lê Văn C", Email = "sv003@sv.edu.vn", VaiTro = VaiTro.SinhVien };
            var tkSV4 = new TaiKhoan { TenDangNhap = "sv004", MatKhauHash = PasswordHasher.Hash("123456"), HoTen = "Phạm Thị D", Email = "sv004@sv.edu.vn", VaiTro = VaiTro.SinhVien };
            var tkSV5 = new TaiKhoan { TenDangNhap = "sv005", MatKhauHash = PasswordHasher.Hash("123456"), HoTen = "Hoàng Văn E", Email = "sv005@sv.edu.vn", VaiTro = VaiTro.SinhVien, TrangThai = TrangThaiTaiKhoan.BiKhoa, SoLanDangNhapSai = TaiKhoan.SoLanSaiToiDa };

            db.TaiKhoans.AddRange(tkQuanLy, tkKeToan, tkSV1, tkSV2, tkSV3, tkSV4, tkSV5);
            db.SaveChanges();

            db.QuanLyKTXs.Add(new QuanLyKTX { TaiKhoanId = tkQuanLy.Id, ChucVu = "Trưởng ban quản lý KTX" });
            db.KeToans.Add(new KeToan { TaiKhoanId = tkKeToan.Id, ChucVu = "Kế toán KTX" });

            var sv1 = new SinhVien { TaiKhoanId = tkSV1.Id, MSSV = "SV001", Lop = "CNTT01", Khoa = "Công nghệ thông tin", GioiTinh = GioiTinh.Nam, NgaySinh = new DateTime(2005, 3, 12), QueQuan = "Thái Nguyên", TrangThaiO = TrangThaiOKTX.DangO };
            var sv2 = new SinhVien { TaiKhoanId = tkSV2.Id, MSSV = "SV002", Lop = "CNTT01", Khoa = "Công nghệ thông tin", GioiTinh = GioiTinh.Nu, NgaySinh = new DateTime(2005, 7, 20), QueQuan = "Bắc Kạn", TrangThaiO = TrangThaiOKTX.DangO };
            var sv3 = new SinhVien { TaiKhoanId = tkSV3.Id, MSSV = "SV003", Lop = "KTPM02", Khoa = "Công nghệ thông tin", GioiTinh = GioiTinh.Nam, NgaySinh = new DateTime(2006, 1, 5), QueQuan = "Tuyên Quang", TrangThaiO = TrangThaiOKTX.DaDangKy };
            var sv4 = new SinhVien { TaiKhoanId = tkSV4.Id, MSSV = "SV004", Lop = "KTPM02", Khoa = "Công nghệ thông tin", GioiTinh = GioiTinh.Nu, NgaySinh = new DateTime(2005, 11, 9), QueQuan = "Cao Bằng", TrangThaiO = TrangThaiOKTX.DangO };
            var sv5 = new SinhVien { TaiKhoanId = tkSV5.Id, MSSV = "SV005", Lop = "KTPM02", Khoa = "Công nghệ thông tin", GioiTinh = GioiTinh.Nam, NgaySinh = new DateTime(2006, 4, 2), QueQuan = "Lạng Sơn", TrangThaiO = TrangThaiOKTX.ChuaDangKy };

            db.SinhViens.AddRange(sv1, sv2, sv3, sv4, sv5);
            db.SaveChanges();

            // ===== 2. Hạ tầng (UC002): Toà A (2 tầng), Toà B (1 tầng) =====
            var toaA = new ToaNha { TenToaNha = "Tòa A", MoTa = "Khu nhà dành cho sinh viên năm nhất, năm hai" };
            var toaB = new ToaNha { TenToaNha = "Tòa B", MoTa = "Khu nhà dành cho sinh viên năm ba, năm tư" };
            db.ToaNhas.AddRange(toaA, toaB);
            db.SaveChanges();

            var tangA1 = new Tang { ToaNhaId = toaA.Id, SoThuTu = 1 };
            var tangA2 = new Tang { ToaNhaId = toaA.Id, SoThuTu = 2 };
            var tangB1 = new Tang { ToaNhaId = toaB.Id, SoThuTu = 1 };
            db.Tangs.AddRange(tangA1, tangA2, tangB1);
            db.SaveChanges();

            var phongA101 = new Phong { TangId = tangA1.Id, SoPhong = "A101", SucChua = 4, DonGiaThang = 700_000, TrangThai = TrangThaiPhong.DangSuDung };
            var phongA102 = new Phong { TangId = tangA1.Id, SoPhong = "A102", SucChua = 4, DonGiaThang = 700_000, TrangThai = TrangThaiPhong.Trong };
            var phongA201 = new Phong { TangId = tangA2.Id, SoPhong = "A201", SucChua = 4, DonGiaThang = 700_000, TrangThai = TrangThaiPhong.Trong };
            var phongB101 = new Phong { TangId = tangB1.Id, SoPhong = "B101", SucChua = 6, DonGiaThang = 900_000, TrangThai = TrangThaiPhong.DangSuDung };
            db.Phongs.AddRange(phongA101, phongA102, phongA201, phongB101);
            db.SaveChanges();

            var giuongList = new List<Giuong>
            {
                new() { PhongId = phongA101.Id, SoGiuong = "A101-01", TrangThai = TrangThaiGiuong.DaCoNguoi, SinhVienId = sv1.Id },
                new() { PhongId = phongA101.Id, SoGiuong = "A101-02", TrangThai = TrangThaiGiuong.DaCoNguoi, SinhVienId = sv2.Id },
                new() { PhongId = phongA101.Id, SoGiuong = "A101-03", TrangThai = TrangThaiGiuong.Trong },
                new() { PhongId = phongA101.Id, SoGiuong = "A101-04", TrangThai = TrangThaiGiuong.Trong },

                new() { PhongId = phongA102.Id, SoGiuong = "A102-01", TrangThai = TrangThaiGiuong.Trong },
                new() { PhongId = phongA102.Id, SoGiuong = "A102-02", TrangThai = TrangThaiGiuong.Trong },
                new() { PhongId = phongA102.Id, SoGiuong = "A102-03", TrangThai = TrangThaiGiuong.Trong },
                new() { PhongId = phongA102.Id, SoGiuong = "A102-04", TrangThai = TrangThaiGiuong.Trong },

                new() { PhongId = phongA201.Id, SoGiuong = "A201-01", TrangThai = TrangThaiGiuong.Trong },
                new() { PhongId = phongA201.Id, SoGiuong = "A201-02", TrangThai = TrangThaiGiuong.Trong },
                new() { PhongId = phongA201.Id, SoGiuong = "A201-03", TrangThai = TrangThaiGiuong.Trong },
                new() { PhongId = phongA201.Id, SoGiuong = "A201-04", TrangThai = TrangThaiGiuong.Trong },

                new() { PhongId = phongB101.Id, SoGiuong = "B101-01", TrangThai = TrangThaiGiuong.DaCoNguoi, SinhVienId = sv4.Id },
                new() { PhongId = phongB101.Id, SoGiuong = "B101-02", TrangThai = TrangThaiGiuong.Trong },
                new() { PhongId = phongB101.Id, SoGiuong = "B101-03", TrangThai = TrangThaiGiuong.Trong },
                new() { PhongId = phongB101.Id, SoGiuong = "B101-04", TrangThai = TrangThaiGiuong.Trong },
                new() { PhongId = phongB101.Id, SoGiuong = "B101-05", TrangThai = TrangThaiGiuong.Trong },
                new() { PhongId = phongB101.Id, SoGiuong = "B101-06", TrangThai = TrangThaiGiuong.Trong },
            };
            db.Giuongs.AddRange(giuongList);
            db.SaveChanges();

            var giuongA101_01 = giuongList.First(g => g.SoGiuong == "A101-01");
            var giuongA101_02 = giuongList.First(g => g.SoGiuong == "A101-02");
            var giuongB101_01 = giuongList.First(g => g.SoGiuong == "B101-01");

            // ===== 3. Đăng ký ở (UC004) =====
            var dk1 = new DangKyO { SinhVienId = sv1.Id, PhongId = phongA101.Id, GiuongId = giuongA101_01.Id, NgayDangKy = DateTime.Now.AddMonths(-6), NgayNhanPhong = DateTime.Now.AddMonths(-6).AddDays(2), TrangThai = TrangThaiDangKyO.DangO };
            var dk2 = new DangKyO { SinhVienId = sv2.Id, PhongId = phongA101.Id, GiuongId = giuongA101_02.Id, NgayDangKy = DateTime.Now.AddMonths(-6), NgayNhanPhong = DateTime.Now.AddMonths(-6).AddDays(2), TrangThai = TrangThaiDangKyO.DangO };
            var dk3 = new DangKyO { SinhVienId = sv3.Id, NgayDangKy = DateTime.Now.AddDays(-1), TrangThai = TrangThaiDangKyO.ChoDuyet, GhiChu = "Muốn ở cùng phòng với bạn cùng lớp KTPM02 nếu còn chỗ trống" };
            var dk4 = new DangKyO { SinhVienId = sv4.Id, PhongId = phongB101.Id, GiuongId = giuongB101_01.Id, NgayDangKy = DateTime.Now.AddMonths(-3), NgayNhanPhong = DateTime.Now.AddMonths(-3).AddDays(1), TrangThai = TrangThaiDangKyO.DangO };
            db.DangKyOs.AddRange(dk1, dk2, dk3, dk4);
            db.SaveChanges();

            // ===== 4. Phí phòng & thanh toán & công nợ (UC005) =====
            var thangTruoc = DateTime.Now.AddMonths(-1);
            var thangNay = DateTime.Now;

            var pp1 = new PhieuPhi { SinhVienId = sv1.Id, ThangNam = thangTruoc.ToString("MM/yyyy"), SoTienPhaiDong = 700_000, SoTienDaDong = 700_000, HanThanhToan = thangTruoc.AddDays(10), TrangThai = TrangThaiPhieuPhi.DaThanhToan };
            var pp2 = new PhieuPhi { SinhVienId = sv1.Id, ThangNam = thangNay.ToString("MM/yyyy"), SoTienPhaiDong = 700_000, SoTienDaDong = 0, HanThanhToan = thangNay.AddDays(10), TrangThai = TrangThaiPhieuPhi.ChuaThanhToan };

            var pp3 = new PhieuPhi { SinhVienId = sv2.Id, ThangNam = thangTruoc.ToString("MM/yyyy"), SoTienPhaiDong = 700_000, SoTienDaDong = 700_000, HanThanhToan = thangTruoc.AddDays(10), TrangThai = TrangThaiPhieuPhi.DaThanhToan };
            var pp4 = new PhieuPhi { SinhVienId = sv2.Id, ThangNam = thangNay.ToString("MM/yyyy"), SoTienPhaiDong = 700_000, SoTienDaDong = 0, HanThanhToan = thangNay.AddDays(-3), TrangThai = TrangThaiPhieuPhi.QuaHan };

            var pp5 = new PhieuPhi { SinhVienId = sv4.Id, ThangNam = thangNay.ToString("MM/yyyy"), SoTienPhaiDong = 900_000, SoTienDaDong = 400_000, HanThanhToan = thangNay.AddDays(10), TrangThai = TrangThaiPhieuPhi.DaThanhToanMotPhan };

            db.PhieuPhis.AddRange(pp1, pp2, pp3, pp4, pp5);
            db.SaveChanges();

            db.ThanhToans.AddRange(
                new ThanhToan { PhieuPhiId = pp1.Id, SoTien = 700_000, NgayThanhToan = thangTruoc.AddDays(5), GhiChu = "Đóng đủ tiền phòng tháng trước" },
                new ThanhToan { PhieuPhiId = pp3.Id, SoTien = 700_000, NgayThanhToan = thangTruoc.AddDays(6), GhiChu = "Đóng đủ tiền phòng tháng trước" },
                new ThanhToan { PhieuPhiId = pp5.Id, SoTien = 400_000, NgayThanhToan = thangNay.AddDays(-2), GhiChu = "Đóng một phần, còn nợ 500.000đ" }
            );

            db.CongNos.AddRange(
                new CongNo { SinhVienId = sv1.Id, TongNoHienTai = pp2.SoTienConLai },
                new CongNo { SinhVienId = sv2.Id, TongNoHienTai = pp4.SoTienConLai },
                new CongNo { SinhVienId = sv4.Id, TongNoHienTai = pp5.SoTienConLai },
                new CongNo { SinhVienId = sv3.Id, TongNoHienTai = 0 },
                new CongNo { SinhVienId = sv5.Id, TongNoHienTai = 0 }
            );
            db.SaveChanges();

            // ===== 5. Phản ánh / sự cố (UC006) =====
            db.PhanAnhSuCos.AddRange(
                new PhanAnhSuCo { SinhVienId = sv1.Id, PhongId = phongA101.Id, TieuDe = "Bóng đèn phòng A101 bị hỏng", NoiDung = "Bóng đèn khu vực học tập trong phòng đã tắt hẳn 2 ngày nay, mong ban quản lý cử người thay.", LoaiPhanAnh = LoaiPhanAnh.SuCoCoSoVatChat, NgayGui = DateTime.Now.AddDays(-2), TrangThai = TrangThaiPhanAnh.Moi },
                new PhanAnhSuCo { SinhVienId = sv2.Id, PhongId = phongA101.Id, TieuDe = "Phòng bên cạnh gây ồn ào buổi tối", NoiDung = "Phòng A102 thường xuyên mở nhạc lớn sau 23h, ảnh hưởng đến giờ nghỉ của các phòng xung quanh.", LoaiPhanAnh = LoaiPhanAnh.ViPhamNoiQuy, NgayGui = DateTime.Now.AddDays(-5), TrangThai = TrangThaiPhanAnh.DangXuLy, PhanHoi = "Ban quản lý đã nhắc nhở phòng A102, sẽ theo dõi thêm." },
                new PhanAnhSuCo { SinhVienId = sv4.Id, PhongId = phongB101.Id, TieuDe = "Vòi nước nhà vệ sinh bị rò rỉ", NoiDung = "Vòi nước khu vệ sinh chung tầng 1 toà B bị rò nước liên tục, gây trơn trượt.", LoaiPhanAnh = LoaiPhanAnh.SuCoCoSoVatChat, NgayGui = DateTime.Now.AddDays(-10), TrangThai = TrangThaiPhanAnh.DaXuLy, PhanHoi = "Đã sửa chữa xong ngày hôm sau, cảm ơn sinh viên đã phản ánh." }
            );
            db.SaveChanges();
        }
    }
}
