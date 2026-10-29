using System.Text;
using System.Text.Json;
using KTXManagement.Demo.Data;
using KTXManagement.Demo.Models;
using KTXManagement.Demo.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KTXManagement.Demo.Services
{
    /// <summary>
    /// AIService: xử lý các nghiệp vụ AI của hệ thống KTX:
    ///  1. AI tóm tắt phản ánh/sự cố theo nhóm vấn đề cho Quản lý KTX (UC006 / UC007)
    ///  2. AI gợi ý xếp phòng dựa trên chỗ trống và tiêu chí cơ bản cho Sinh viên (UC004)
    ///  3. Hỗ trợ Quản lý KTX xem gợi ý khi duyệt xếp phòng
    /// </summary>
    public class AIService : IAIService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<AIService> _logger;

        public AIService(ApplicationDbContext db, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<AIService> logger)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        private string ApiKey => _config["AIConfig:ApiKey"] ?? string.Empty;
        private string Model => _config["AIConfig:Model"] ?? "claude-sonnet-4-6";
        private string ApiUrl => _config["AIConfig:ApiUrl"] ?? "https://api.anthropic.com/v1/messages";

        // ==================== 1. AI TÓM TẮT PHẢN ÁNH/SỰ CỐ THEO NHÓM VẤN ĐỀ ====================

        public async Task<AITomTatPhanAnhViewModel> TomTatPhanAnhTheoNhomAsync(List<PhanAnhSuCo> danhSachPhanAnh)
        {
            var vm = new AITomTatPhanAnhViewModel
            {
                TongSoPhanAnh = danhSachPhanAnh.Count,
                TongPhanAnhMoi = danhSachPhanAnh.Count(p => p.TrangThai == TrangThaiPhanAnh.Moi),
                TongDangXuLy = danhSachPhanAnh.Count(p => p.TrangThai == TrangThaiPhanAnh.DangXuLy),
                TongDaXuLy = danhSachPhanAnh.Count(p => p.TrangThai == TrangThaiPhanAnh.DaXuLy),
                ThoiGianPhanTich = DateTime.Now
            };

            if (!danhSachPhanAnh.Any())
            {
                vm.NhanXetTongQuanAI = "Hiện chưa có phản ánh hay sự cố nào được ghi nhận trong hệ thống.";
                return vm;
            }

            // Định nghĩa các nhóm vấn đề
            var nhomDien = new PhanAnhNhomVanDe
            {
                TenNhom = "Điện & Thiết bị chiếu sáng",
                Icon = "⚡",
                MauSac = "warning",
                MucDoUuTien = "Cao",
                MucDoUuTienBadge = "danger"
            };

            var nhomNuoc = new PhanAnhNhomVanDe
            {
                TenNhom = "Nước & Thiết bị vệ sinh",
                Icon = "💧",
                MauSac = "info",
                MucDoUuTien = "Khẩn cấp",
                MucDoUuTienBadge = "danger"
            };

            var nhomCoSoVatChat = new PhanAnhNhomVanDe
            {
                TenNhom = "Cơ sở vật chất & Nội thất",
                Icon = "🪑",
                MauSac = "primary",
                MucDoUuTien = "Trung bình",
                MucDoUuTienBadge = "warning"
            };

            var nhomAnNinh = new PhanAnhNhomVanDe
            {
                TenNhom = "An ninh & Vi phạm nội quy",
                Icon = "🚨",
                MauSac = "danger",
                MucDoUuTien = "Cao",
                MucDoUuTienBadge = "danger"
            };

            var nhomKhac = new PhanAnhNhomVanDe
            {
                TenNhom = "Mạng Internet & Tiện ích chung",
                Icon = "🌐",
                MauSac = "secondary",
                MucDoUuTien = "Thấp",
                MucDoUuTienBadge = "info"
            };

            // Phân loại từng phản ánh vào nhóm tương ứng bằng phân tích ngữ nghĩa từ khóa
            foreach (var pa in danhSachPhanAnh)
            {
                var text = (pa.TieuDe + " " + pa.NoiDung).ToLower();

                if (pa.LoaiPhanAnh == LoaiPhanAnh.ViPhamNoiQuy ||
                    text.Contains("ồn") || text.Contains("nhạc") || text.Contains("hút thuốc") ||
                    text.Contains("người lạ") || text.Contains("trộm") || text.Contains("mất cắp") ||
                    text.Contains("đánh bài") || text.Contains("nhậu") || text.Contains("23h") ||
                    text.Contains("nội quy") || text.Contains("an ninh"))
                {
                    nhomAnNinh.DanhSachPhanAnh.Add(pa);
                }
                else if (text.Contains("nước") || text.Contains("vòi") || text.Contains("rò rỉ") ||
                         text.Contains("tắc") || text.Contains("bồn cầu") || text.Contains("thấm") ||
                         text.Contains("ngập") || text.Contains("vệ sinh") || text.Contains("thoát nước"))
                {
                    nhomNuoc.DanhSachPhanAnh.Add(pa);
                }
                else if (text.Contains("điện") || text.Contains("bóng đèn") || text.Contains("đèn") ||
                         text.Contains("ổ cắm") || text.Contains("chập") || text.Contains("công tắc") ||
                         text.Contains("quạt") || text.Contains("nóng lạnh") || text.Contains("aptomat"))
                {
                    nhomDien.DanhSachPhanAnh.Add(pa);
                }
                else if (text.Contains("wifi") || text.Contains("mạng") || text.Contains("internet") ||
                         text.Contains("rác") || text.Contains("thang máy") || text.Contains("máy giặt"))
                {
                    nhomKhac.DanhSachPhanAnh.Add(pa);
                }
                else
                {
                    nhomCoSoVatChat.DanhSachPhanAnh.Add(pa);
                }
            }

            var tatCaNhom = new List<PhanAnhNhomVanDe> { nhomNuoc, nhomDien, nhomCoSoVatChat, nhomAnNinh, nhomKhac };

            // Tính toán số liệu và sinh nhận xét AI cho từng nhóm có dữ liệu
            foreach (var nhom in tatCaNhom)
            {
                nhom.SoLuong = nhom.DanhSachPhanAnh.Count;
                if (nhom.SoLuong == 0) continue;

                nhom.TyLePhanTram = Math.Round(nhom.SoLuong * 100.0 / vm.TongSoPhanAnh, 1);
                nhom.SoLuongMoi = nhom.DanhSachPhanAnh.Count(p => p.TrangThai == TrangThaiPhanAnh.Moi);
                nhom.SoLuongDangXuLy = nhom.DanhSachPhanAnh.Count(p => p.TrangThai == TrangThaiPhanAnh.DangXuLy);
                nhom.SoLuongDaXuLy = nhom.DanhSachPhanAnh.Count(p => p.TrangThai == TrangThaiPhanAnh.DaXuLy);

                nhom.DanhSachPhongLienQuan = nhom.DanhSachPhanAnh
                    .Where(p => p.Phong != null)
                    .Select(p => p.Phong!.SoPhong)
                    .Distinct()
                    .ToList();

                SinhNhanXetVaDeXuatChoNhom(nhom);
                vm.CacNhomVanDe.Add(nhom);
            }

            // Sắp xếp các nhóm theo mức độ ưu tiên & số lượng phản ánh mới
            vm.CacNhomVanDe = vm.CacNhomVanDe
                .OrderByDescending(n => n.SoLuongMoi)
                .ThenByDescending(n => n.SoLuong)
                .ToList();

            // Sinh nhận xét tổng quan toàn hệ thống
            var sbTongQuan = new StringBuilder();
            sbTongQuan.AppendLine($"Hệ thống ghi nhận tổng cộng {vm.TongSoPhanAnh} phản ánh/sự cố, hiện có {vm.TongPhanAnhMoi} vụ việc mới và {vm.TongDangXuLy} vụ việc đang trong quá trình giải quyết.");

            var nhomNhieuNhat = vm.CacNhomVanDe.FirstOrDefault();
            if (nhomNhieuNhat != null)
            {
                sbTongQuan.AppendLine($"Nhóm vấn đề chiếm tỷ lệ cao nhất là \"{nhomNhieuNhat.TenNhom}\" với {nhomNhieuNhat.SoLuong} vụ ({nhomNhieuNhat.TyLePhanTram}%).");
            }

            if (vm.TongPhanAnhMoi > 0)
            {
                sbTongQuan.AppendLine("Đề nghị Ban quản lý ký túc xá phân công bộ phận kỹ thuật và giám sát xử lý dứt điểm các sự cố còn tồn đọng để đảm bảo sinh hoạt ổn định cho sinh viên.");
            }
            else
            {
                sbTongQuan.AppendLine("Tiến độ xử lý phản ánh của Ban quản lý hiện tại đạt hiệu quả tốt, không còn tồn đọng sự cố mới chưa tiếp nhận.");
            }

            vm.NhanXetTongQuanAI = sbTongQuan.ToString();

            // Sinh danh sách khuyến nghị hành động ưu tiên
            vm.KhuyenNghiUuTienAI = SinhKhuyenNghiUuTien(vm.CacNhomVanDe);

            // Nếu có API Key, thử làm phong phú thêm nhận xét bằng LLM
            if (!string.IsNullOrWhiteSpace(ApiKey))
            {
                try
                {
                    var tomTatQuick = string.Join("\n", vm.CacNhomVanDe.Select(n => $"- {n.TenNhom}: {n.SoLuong} vụ ({n.SoLuongMoi} mới, {n.SoLuongDangXuLy} đang xử lý). Phòng liên quan: {string.Join(", ", n.DanhSachPhongLienQuan)}"));
                    var prompt = $"Bạn là trợ lý Quản lý ký túc xá. Dựa vào tóm tắt sau:\n{tomTatQuick}\nHãy viết 1 đoạn nhận xét chiến lược ngắn gọn (khoảng 3-4 câu) bằng tiếng Việt đưa ra đánh giá rủi ro và chỉ đạo hành động cụ thể cho ban quản lý KTX.";
                    var llmResult = await GoiLLMAsync("Bạn là trợ lý quản lý KTX.", prompt);
                    if (!string.IsNullOrWhiteSpace(llmResult))
                    {
                        vm.NhanXetTongQuanAI += "\n\n[Đánh giá chuyên sâu từ AI]: " + llmResult;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể bổ sung LLM vào tóm tắt phản ánh.");
                }
            }

            return vm;
        }

        private static void SinhNhanXetVaDeXuatChoNhom(PhanAnhNhomVanDe nhom)
        {
            var phongStr = nhom.DanhSachPhongLienQuan.Any()
                ? "tại các phòng: " + string.Join(", ", nhom.DanhSachPhongLienQuan)
                : "tại các khu vực sinh hoạt chung";

            switch (nhom.TenNhom)
            {
                case "Nước & Thiết bị vệ sinh":
                    nhom.TomTatAI = $"Ghi nhận {nhom.SoLuong} sự cố về rò rỉ nước, thiết bị vệ sinh {phongStr}. Nguy cơ gây trơn trượt, lãng phí nước và ẩm mốc sàn nhà.";
                    nhom.DeXuatHanhDongAI = "Điều phối ngay nhân viên kỹ thuật nước kiểm tra van khóa tổng và thay gioăng/vòi rò rỉ trong vòng 12h. Kiểm tra cống thoát sàn chống ngập.";
                    break;

                case "Điện & Thiết bị chiếu sáng":
                    nhom.TomTatAI = $"Có {nhom.SoLuong} phản ánh hư hỏng đèn chiếu sáng, ổ cắm hoặc thiết bị điện {phongStr}, ảnh hưởng trực tiếp đến việc học tập của sinh viên.";
                    nhom.DeXuatHanhDongAI = "Phân công thợ điện thay bóng đèn chiếu sáng và đo an toàn điện tải tại các ổ cắm trong ngày. Kiểm tra định kỳ hộp kỹ thuật.";
                    break;

                case "Cơ sở vật chất & Nội thất":
                    nhom.TomTatAI = $"Phát sinh {nhom.SoLuong} phản ánh về khóa cửa, giường, tủ hoặc cơ sở vật chất phòng ở {phongStr}.";
                    nhom.DeXuatHanhDongAI = "Lên lịch thợ mộc/cơ khí sửa chữa ổ khóa và gia cố khung giường trong 24-48h. Nhắc nhở sinh viên giữ gìn tài sản chung.";
                    break;

                case "An ninh & Vi phạm nội quy":
                    nhom.TomTatAI = $"Có {nhom.SoLuong} vụ việc liên quan đến tiếng ồn, giờ giới nghiêm hoặc vi phạm nội quy KTX {phongStr}.";
                    nhom.DeXuatHanhDongAI = "Ban quản lý cần làm việc trực tiếp, lập biên bản nhắc nhở đối với phòng vi phạm; tăng cường tuần tra các tầng sau 23h.";
                    break;

                default:
                    nhom.TomTatAI = $"Ghi nhận {nhom.SoLuong} phản ánh liên quan đến kết nối mạng internet hoặc các tiện ích sinh hoạt chung.";
                    nhom.DeXuatHanhDongAI = "Liên hệ đơn vị cung cấp mạng kiểm tra bộ phát Access Point và nhắc nhở bộ phận vệ sinh dọn dẹp đúng ca trực.";
                    break;
            }
        }

        private static List<string> SinhKhuyenNghiUuTien(List<PhanAnhNhomVanDe> cacNhom)
        {
            var list = new List<string>();

            var nhomNuoc = cacNhom.FirstOrDefault(n => n.TenNhom.Contains("Nước"));
            if (nhomNuoc != null && (nhomNuoc.SoLuongMoi > 0 || nhomNuoc.SoLuongDangXuLy > 0))
            {
                list.Add("💧 [ƯU TIÊN 1] Khắc phục ngay sự cố rò rỉ nước để tránh lãng phí nước và nguy cơ chập điện ẩm mốc.");
            }

            var nhomDien = cacNhom.FirstOrDefault(n => n.TenNhom.Contains("Điện"));
            if (nhomDien != null && (nhomDien.SoLuongMoi > 0 || nhomDien.SoLuongDangXuLy > 0))
            {
                list.Add("⚡ [ƯU TIÊN 2] Thay thế bóng đèn và kiểm tra an toàn điện khu vực học tập của sinh viên trong ngày.");
            }

            var nhomAnNinh = cacNhom.FirstOrDefault(n => n.TenNhom.Contains("An ninh"));
            if (nhomAnNinh != null && nhomAnNinh.SoLuongMoi > 0)
            {
                list.Add("🚨 [AN NINH] Tăng cường kiểm tra phòng và nhắc nhở sinh viên giữ trật tự sau 23h theo nội quy.");
            }

            if (!list.Any())
            {
                list.Add("✅ Các sự cố trọng yếu đều đã được xử lý hoặc kiểm soát tốt. Duy trì kiểm tra bảo dưỡng định kỳ hàng tuần.");
            }

            return list;
        }

        public async Task<string> TomTatPhanAnhAsync(List<PhanAnhSuCo> danhSachPhanAnh)
        {
            var vm = await TomTatPhanAnhTheoNhomAsync(danhSachPhanAnh);
            var sb = new StringBuilder();
            sb.AppendLine(vm.NhanXetTongQuanAI);
            sb.AppendLine();
            sb.AppendLine("Phân bố theo nhóm vấn đề:");
            foreach (var n in vm.CacNhomVanDe)
            {
                sb.AppendLine($"- {n.Icon} {n.TenNhom}: {n.SoLuong} sự cố ({n.TyLePhanTram}%) - Trạng thái: {n.SoLuongMoi} mới, {n.SoLuongDangXuLy} đang xử lý, {n.SoLuongDaXuLy} đã xong.");
            }
            sb.AppendLine();
            sb.AppendLine("Hành động trọng tâm:");
            foreach (var kn in vm.KhuyenNghiUuTienAI)
            {
                sb.AppendLine(kn);
            }
            return sb.ToString();
        }

        // ==================== 2. AI GỢI Ý XẾP PHÒNG CHO SINH VIÊN (UC004) ====================

        public async Task<KetQuaGoiYPhongViewModel> GoiYPhongChoSinhVienAsync(SinhVien sv, TieuChiGoiYPhongViewModel tieuChi)
        {
            var ketQua = new KetQuaGoiYPhongViewModel
            {
                TieuChi = tieuChi
            };

            // Lấy tất cả phòng còn giường trống và không ở trạng thái bảo trì
            var danhSachPhong = await _db.Phongs
                .Include(p => p.Tang)
                    .ThenInclude(t => t!.ToaNha)
                .Include(p => p.DanhSachGiuong)
                    .ThenInclude(g => g.SinhVien)
                .Where(p => p.TrangThai != TrangThaiPhong.BaoTri)
                .ToListAsync();

            var phongConCho = danhSachPhong.Where(p => p.SoGiuongTrong > 0).ToList();
            if (!phongConCho.Any())
            {
                ketQua.NhanXetTongQuatAI = "Rất tiếc! Hiện tại ký túc xá đã hết giường trống hoặc các phòng đang trong diện bảo trì. Bạn vui lòng liên hệ Ban quản lý KTX để được hỗ trợ.";
                return ketQua;
            }

            var danhSachDanhGia = new List<PhongGoiYItem>();

            foreach (var p in phongConCho)
            {
                var sinhVienTrongPhong = p.DanhSachGiuong
                    .Where(g => g.SinhVien != null)
                    .Select(g => g.SinhVien!)
                    .ToList();

                // RÀNG BUỘC CỨNG: Kiểm tra giới tính
                // Nếu phòng đã có người, tất cả người trong phòng phải CÙNG giới tính với sinh viên
                if (sinhVienTrongPhong.Any() && sinhVienTrongPhong.Any(s => s.GioiTinh != sv.GioiTinh))
                {
                    continue; // Bỏ qua phòng khác giới tính
                }

                int diem = 60; // Điểm cơ sở khi hợp lệ giới tính và còn chỗ
                var diemNoiBat = new List<string>();

                // 1. Tiêu chí bạn cùng phòng
                bool coBanCungLop = sinhVienTrongPhong.Any(s => !string.IsNullOrEmpty(sv.Lop) && s.Lop == sv.Lop);
                bool coBanCungKhoa = sinhVienTrongPhong.Any(s => !string.IsNullOrEmpty(sv.Khoa) && s.Khoa == sv.Khoa);

                if (coBanCungLop)
                {
                    diem += 18;
                    diemNoiBat.Add($"Có bạn cùng lớp {sv.Lop} đang ở");
                }
                else if (coBanCungKhoa)
                {
                    diem += 12;
                    diemNoiBat.Add($"Có bạn cùng khoa {sv.Khoa} đang ở");
                }
                else if (!sinhVienTrongPhong.Any())
                {
                    diem += 8;
                    diemNoiBat.Add("Phòng trống 100%, bạn có thể chọn giường đầu tiên");
                }

                // 2. Tiêu chí tầng
                int soTang = p.Tang?.SoThuTu ?? 1;
                if (tieuChi.ViTriTang == "TangThap")
                {
                    if (soTang <= 2)
                    {
                        diem += 10;
                        diemNoiBat.Add($"Tầng {soTang} thuận tiện đi lại, không ngại leo cầu thang");
                    }
                }
                else if (tieuChi.ViTriTang == "TangCao")
                {
                    if (soTang >= 3)
                    {
                        diem += 10;
                        diemNoiBat.Add($"Tầng {soTang} cao ráo, thoáng mát và yên tĩnh");
                    }
                }
                else
                {
                    diem += 5;
                }

                // 3. Tiêu chí ngân sách
                if (tieuChi.MucGiaMongMuon == "TietKiem")
                {
                    if (p.DonGiaThang <= 700_000)
                    {
                        diem += 10;
                        diemNoiBat.Add($"Mức phí {p.DonGiaThang:N0}đ/tháng tiết kiệm chi phí");
                    }
                }
                else if (tieuChi.MucGiaMongMuon == "TieuChuan")
                {
                    if (p.DonGiaThang > 700_000)
                    {
                        diem += 10;
                        diemNoiBat.Add($"Phòng chất lượng cao {p.DonGiaThang:N0}đ/tháng");
                    }
                }
                else
                {
                    diem += 5;
                }

                // 4. Tiêu chí phong cách sống
                if (tieuChi.PhongCachSong == "YenTinh")
                {
                    if (p.SoGiuongTrong >= 2 || p.SucChua <= 4)
                    {
                        diem += 8;
                        diemNoiBat.Add($"Phòng {p.SucChua} người, còn {p.SoGiuongTrong} chỗ trống yên tĩnh");
                    }
                }
                else if (tieuChi.PhongCachSong == "DongVui")
                {
                    if (sinhVienTrongPhong.Count >= 2 || p.SucChua >= 6)
                    {
                        diem += 8;
                        diemNoiBat.Add($"Phòng đông vui, dễ giao lưu kết bạn");
                    }
                }

                // Chuẩn hóa điểm tối đa 98%
                diem = Math.Min(98, diem);

                string danhGia = diem >= 90 ? "Phù hợp nhất" : diem >= 80 ? "Rất phù hợp" : "Khá phù hợp";
                string mauBadge = diem >= 90 ? "success" : diem >= 80 ? "primary" : "info";

                var item = new PhongGoiYItem
                {
                    PhongId = p.Id,
                    SoPhong = p.SoPhong,
                    TenToaNha = p.Tang?.ToaNha?.TenToaNha ?? "Tòa nhà",
                    TangSo = soTang,
                    SucChua = p.SucChua,
                    SoGiuongTrong = p.SoGiuongTrong,
                    DonGiaThang = p.DonGiaThang,
                    DiemPhuHop = diem,
                    DanhGiaPhuHop = danhGia,
                    MauBadge = mauBadge,
                    DiemNoiBat = diemNoiBat,
                    GiuongTrong = p.DanhSachGiuong
                        .Where(g => g.TrangThai == TrangThaiGiuong.Trong)
                        .Select(g => new GiuongGoiYInfo { GiuongId = g.Id, SoGiuong = g.SoGiuong })
                        .ToList(),
                    BanCungPhong = sinhVienTrongPhong
                        .Select(s => $"SV {(s.GioiTinh == GioiTinh.Nam ? "Nam" : "Nữ")} (Khoa {s.Khoa ?? "Đang cập nhật"}, Lớp {s.Lop ?? "..."})")
                        .ToList(),
                    NhanXetAI = SinhNhanXetPhong(p, sv, diemNoiBat, diem)
                };

                danhSachDanhGia.Add(item);
            }

            // Lấy top các phòng phù hợp nhất
            ketQua.DanhSachGoiY = danhSachDanhGia
                .OrderByDescending(x => x.DiemPhuHop)
                .ThenByDescending(x => x.SoGiuongTrong)
                .Take(5)
                .ToList();

            if (!ketQua.DanhSachGoiY.Any())
            {
                ketQua.NhanXetTongQuatAI = $"Hiện không còn phòng nào phù hợp với giới tính ({sv.GioiTinh}) hoặc tiêu chí đã chọn. Bạn có thể nới lỏng các tiêu chí (chọn tất cả tầng / tất cả mức giá) để tìm thêm phòng.";
            }
            else
            {
                var top1 = ketQua.DanhSachGoiY.First();
                ketQua.NhanXetTongQuatAI = $"AI đã phân tích và tìm thấy {ketQua.DanhSachGoiY.Count} phòng trống phù hợp nhất cho sinh viên {sv.MSSV} ({sv.GioiTinh}, Khoa {sv.Khoa}). Lựa chọn tối ưu nhất là phòng {top1.SoPhong} ({top1.TenToaNha}) với độ phù hợp đạt {top1.DiemPhuHop}%.";
            }

            return ketQua;
        }

        private static string SinhNhanXetPhong(Phong phong, SinhVien sv, List<string> diemNoiBat, int diem)
        {
            var sb = new StringBuilder();
            sb.Append($"Phòng {phong.SoPhong} đạt {diem}% độ tương thích: ");
            if (diemNoiBat.Any())
            {
                sb.Append(string.Join(", ", diemNoiBat));
                sb.Append(". ");
            }
            sb.Append($"Đảm bảo quy định phòng ở {sv.GioiTinh}, hiện còn {phong.SoGiuongTrong} giường trống sẵn sàng xếp phòng.");
            return sb.ToString();
        }

        // ==================== 3. HỖ TRỢ QUẢN LÝ KTX XẾP PHÒNG (UC004) ====================

        public async Task<string> GoiYPhongAsync(DangKyO yeuCau)
        {
            var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.Id == yeuCau.SinhVienId);
            if (sv == null) return "Không tìm thấy thông tin sinh viên để gợi ý xếp phòng.";

            var tieuChi = new TieuChiGoiYPhongViewModel
            {
                SinhVienId = sv.Id,
                MSSV = sv.MSSV,
                HoTen = sv.TaiKhoan?.HoTen ?? sv.MSSV,
                GioiTinh = sv.GioiTinh,
                Lop = sv.Lop,
                Khoa = sv.Khoa,
                UuTienBanCungPhong = "CungKhoa",
                MucGiaMongMuon = "TatCa",
                ViTriTang = "TatCa",
                PhongCachSong = "CanBang"
            };

            var ketQua = await GoiYPhongChoSinhVienAsync(sv, tieuChi);
            if (!ketQua.DanhSachGoiY.Any())
                return "Hiện không có phòng nào còn giường trống phù hợp giới tính để xếp.";

            var sb = new StringBuilder();
            sb.AppendLine($"AI gợi ý xếp phòng cho sinh viên {sv.MSSV} ({sv.GioiTinh}, lớp {sv.Lop}, khoa {sv.Khoa}):");
            int i = 1;
            foreach (var item in ketQua.DanhSachGoiY.Take(3))
            {
                sb.AppendLine($"{i}. Phòng {item.SoPhong} ({item.TenToaNha}, Tầng {item.TangSo}) - Độ phù hợp: {item.DiemPhuHop}%");
                sb.AppendLine($"   → Còn {item.SoGiuongTrong} giường trống. {string.Join("; ", item.DiemNoiBat)}.");
                i++;
            }

            return sb.ToString();
        }

        // ==================== HÀM GỌI LLM (NẾU CÓ CẤU HÌNH API KEY) ====================

        private async Task<string?> GoiLLMAsync(string systemPrompt, string userMessage)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);

                var payload = new
                {
                    model = Model,
                    max_tokens = 600,
                    system = systemPrompt,
                    messages = new[] { new { role = "user", content = userMessage } }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
                request.Headers.Add("x-api-key", ApiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gọi AI API thất bại: {Status}", response.StatusCode);
                    return null;
                }

                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var contentArray = doc.RootElement.GetProperty("content");
                var sb = new StringBuilder();
                foreach (var block in contentArray.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "text")
                    {
                        sb.Append(block.GetProperty("text").GetString());
                    }
                }
                var ketQua = sb.ToString();
                return string.IsNullOrWhiteSpace(ketQua) ? null : ketQua;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gọi AI API, sử dụng kết quả phân tích nội bộ.");
                return null;
            }
        }
    }
}
