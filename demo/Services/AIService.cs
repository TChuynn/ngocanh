using System.Text;
using System.Text.Json;
using KTXManagement.Demo.Data;
using KTXManagement.Demo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace KTXManagement.Demo.Services
{
    /// <summary>
    /// AIService: xử lý toàn bộ nghiệp vụ AI của hệ thống KTX AI:
    ///  - Chatbot hỏi-đáp giới hạn trong phạm vi dữ liệu dự án
    ///  - Gợi ý xếp phòng khi Quản lý KTX duyệt yêu cầu đăng ký ở (UC004)
    ///  - Tóm tắt xu hướng phản ánh/sự cố cho báo cáo thống kê (UC007)
    ///
    /// Nếu appsettings["AIConfig:ApiKey"] có giá trị, service sẽ gọi API Anthropic thật.
    /// Nếu không, tự động dùng bộ trả lời rule-based dựa trên dữ liệu đã seed
    /// để bản demo luôn chạy được mà không cần Internet / API key.
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

        // ==================== 1. CHATBOT HỎI-ĐÁP ====================

        public async Task<string> TraLoiAsync(string cauHoi, TaiKhoan? nguoiDung)
        {
            if (string.IsNullOrWhiteSpace(cauHoi))
                return "Bạn vui lòng nhập câu hỏi để mình hỗ trợ nhé.";

            // Bước 1: các câu hỏi "cá nhân" (công nợ của tôi, phòng của tôi...) luôn được
            // trả lời trực tiếp từ dữ liệu thật của đúng người hỏi, không giao cho AI suy đoán.
            var traLoiCaNhan = await TraLoiCauHoiCaNhanAsync(cauHoi, nguoiDung);
            if (traLoiCaNhan != null) return traLoiCaNhan;

            // Bước 2: câu hỏi chung về hệ thống -> build ngữ cảnh dữ liệu + gọi LLM (nếu có key)
            var systemPrompt = await XayDungSystemPromptAsync(nguoiDung);

            if (!string.IsNullOrWhiteSpace(ApiKey))
            {
                var ketQua = await GoiLLMAsync(systemPrompt, cauHoi);
                if (ketQua != null) return ketQua;
            }

            // Bước 3: fallback rule-based nếu không có API key hoặc gọi API lỗi
            return TraLoiRuleBased(cauHoi, nguoiDung);
        }

        private async Task<string?> TraLoiCauHoiCaNhanAsync(string cauHoi, TaiKhoan? nguoiDung)
        {
            if (nguoiDung == null || nguoiDung.VaiTro != VaiTro.SinhVien) return null;
            var sv = await _db.SinhViens.Include(s => s.CongNo)
                .FirstOrDefaultAsync(s => s.TaiKhoanId == nguoiDung.Id);
            if (sv == null) return null;

            var cauHoiLower = cauHoi.ToLower();

            if (cauHoiLower.Contains("công nợ") || cauHoiLower.Contains("no ") || cauHoiLower.Contains("còn nợ"))
            {
                var no = sv.CongNo?.TongNoHienTai ?? 0;
                return no > 0
                    ? $"Bạn ({sv.MSSV}) hiện đang còn nợ {no:N0}đ tiền phòng. Vui lòng liên hệ Kế toán KTX để thanh toán trước hạn nhé."
                    : $"Bạn ({sv.MSSV}) hiện không có công nợ nào, cảm ơn bạn đã đóng phí đầy đủ!";
            }

            if (cauHoiLower.Contains("phòng của tôi") || cauHoiLower.Contains("đang ở phòng") || cauHoiLower.Contains("giường của tôi"))
            {
                var dk = await _db.DangKyOs.Include(d => d.Phong).Include(d => d.Giuong)
                    .Where(d => d.SinhVienId == sv.Id && (d.TrangThai == TrangThaiDangKyO.DangO))
                    .FirstOrDefaultAsync();
                return dk?.Phong != null
                    ? $"Bạn hiện đang ở phòng {dk.Phong.SoPhong}, giường {dk.Giuong?.SoGiuong}."
                    : "Bạn hiện chưa được xếp phòng ở KTX. Nếu đã gửi yêu cầu đăng ký ở, vui lòng chờ Quản lý KTX xử lý.";
            }

            if (cauHoiLower.Contains("phản ánh") && (cauHoiLower.Contains("của tôi") || cauHoiLower.Contains("tôi gửi")))
            {
                var soLuong = await _db.PhanAnhSuCos.CountAsync(p => p.SinhVienId == sv.Id);
                var choXuLy = await _db.PhanAnhSuCos.CountAsync(p => p.SinhVienId == sv.Id && p.TrangThai != TrangThaiPhanAnh.DaXuLy && p.TrangThai != TrangThaiPhanAnh.TuChoi);
                return $"Bạn đã gửi {soLuong} phản ánh/sự cố, trong đó {choXuLy} phản ánh đang chờ xử lý. Bạn có thể xem chi tiết ở mục \"Phản ánh của tôi\".";
            }

            return null;
        }

        private async Task<string> XayDungSystemPromptAsync(TaiKhoan? nguoiDung)
        {
            var tongPhong = await _db.Phongs.CountAsync();
            var tongGiuong = await _db.Giuongs.CountAsync();
            var giuongDaO = await _db.Giuongs.CountAsync(g => g.TrangThai == TrangThaiGiuong.DaCoNguoi);
            var choDuyet = await _db.DangKyOs.CountAsync(d => d.TrangThai == TrangThaiDangKyO.ChoDuyet);
            var phanAnhMoi = await _db.PhanAnhSuCos.CountAsync(p => p.TrangThai == TrangThaiPhanAnh.Moi);

            var vaiTro = nguoiDung?.VaiTro.ToString() ?? "Khách";

            var sb = new StringBuilder();
            sb.AppendLine("Bạn là trợ lý AI của \"Hệ thống quản lý ký túc xá có tích hợp AI\".");
            sb.AppendLine("Bạn CHỈ được trả lời các câu hỏi liên quan tới phạm vi của hệ thống này, bao gồm:");
            sb.AppendLine("- Quản lý hạ tầng KTX (toà nhà, tầng, phòng, giường)");
            sb.AppendLine("- Hồ sơ sinh viên nội trú");
            sb.AppendLine("- Đăng ký ở, xếp phòng, chuyển phòng, trả phòng");
            sb.AppendLine("- Phí phòng và công nợ");
            sb.AppendLine("- Phản ánh/sự cố và vi phạm nội quy KTX");
            sb.AppendLine("- Thống kê công suất, công nợ, phản ánh của KTX");
            sb.AppendLine();
            sb.AppendLine("Nếu người dùng hỏi ngoài phạm vi trên, hãy lịch sự từ chối và nhắc họ hỏi đúng chủ đề KTX.");
            sb.AppendLine("Trả lời ngắn gọn, rõ ràng, bằng tiếng Việt.");
            sb.AppendLine();
            sb.AppendLine("Dữ liệu hiện tại của hệ thống (dùng để trả lời các câu hỏi tổng quan, không suy diễn thêm số liệu khác):");
            sb.AppendLine($"- Tổng số phòng: {tongPhong}; Tổng số giường: {tongGiuong}; Số giường đã có người ở: {giuongDaO}");
            sb.AppendLine($"- Số yêu cầu đăng ký ở đang chờ duyệt: {choDuyet}");
            sb.AppendLine($"- Số phản ánh/sự cố mới chưa xử lý: {phanAnhMoi}");
            sb.AppendLine($"- Vai trò người đang hỏi: {vaiTro}");

            return sb.ToString();
        }

        private async Task<string?> GoiLLMAsync(string systemPrompt, string userMessage)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);

                var payload = new
                {
                    model = Model,
                    max_tokens = 500,
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
                _logger.LogWarning(ex, "Lỗi khi gọi AI API, chuyển sang trả lời rule-based.");
                return null;
            }
        }

        private string TraLoiRuleBased(string cauHoi, TaiKhoan? nguoiDung)
        {
            var c = cauHoi.ToLower();

            if (c.Contains("phòng trống") || c.Contains("còn phòng") || c.Contains("còn giường"))
            {
                var phongConCho = _db.Phongs.Include(p => p.DanhSachGiuong)
                    .AsEnumerable()
                    .Where(p => p.SoGiuongTrong > 0)
                    .Select(p => $"{p.SoPhong} (còn {p.SoGiuongTrong} giường trống)")
                    .ToList();

                return phongConCho.Any()
                    ? "Các phòng hiện còn giường trống: " + string.Join(", ", phongConCho) + "."
                    : "Hiện tại KTX không còn giường trống, vui lòng liên hệ trực tiếp Quản lý KTX để được tư vấn.";
            }

            if (c.Contains("đăng ký ở") || c.Contains("đăng ký nội trú") || c.Contains("làm sao để ở ktx"))
            {
                return "Để đăng ký ở KTX (UC004): Sinh viên đăng nhập hệ thống, vào mục \"Đăng ký ở\" và gửi yêu cầu. " +
                       "Quản lý KTX sẽ xem xét, dùng gợi ý AI để xếp phòng/giường phù hợp rồi xác nhận cho bạn.";
            }

            if (c.Contains("phí") || c.Contains("giá phòng") || c.Contains("bao nhiêu tiền"))
            {
                var giaPhong = _db.Phongs.Select(p => p.DonGiaThang).Distinct().OrderBy(x => x).ToList();
                return giaPhong.Any()
                    ? "Giá phòng hiện tại dao động từ " + giaPhong.Min().ToString("N0") + "đ đến " + giaPhong.Max().ToString("N0") + "đ/tháng tuỳ loại phòng."
                    : "Hiện chưa có dữ liệu giá phòng trong hệ thống.";
            }

            if (c.Contains("phản ánh") || c.Contains("sự cố") || c.Contains("khiếu nại"))
            {
                return "Để gửi phản ánh/sự cố hoặc vi phạm nội quy (UC006): Sinh viên vào mục \"Phản ánh/Sự cố\", điền tiêu đề và nội dung. " +
                       "Quản lý KTX sẽ tiếp nhận, xử lý và phản hồi lại cho bạn.";
            }

            if (c.Contains("chuyển phòng"))
            {
                return "Yêu cầu chuyển phòng được Quản lý KTX xử lý trong mục \"Đăng ký ở\" (UC004): hệ thống sẽ tính lại phí phòng nếu có chênh lệch giữa phòng cũ và phòng mới.";
            }

            if (c.Contains("trả phòng"))
            {
                return "Khi trả phòng (UC004), hệ thống sẽ kiểm tra công nợ (UC005) của bạn trước. " +
                       "Nếu còn nợ tiền phòng, bạn cần thanh toán hết trước khi được xác nhận trả phòng.";
            }

            if (c.Contains("xin chào") || c.Contains("hello") || c.Contains("hi "))
            {
                return "Xin chào! Mình là trợ lý AI của hệ thống quản lý KTX. Bạn có thể hỏi mình về: đăng ký ở, tình trạng phòng/giường, phí phòng - công nợ, hoặc cách gửi phản ánh/sự cố.";
            }

            return "Mình chỉ hỗ trợ các câu hỏi liên quan tới quản lý ký túc xá (đăng ký ở, phòng/giường, phí - công nợ, phản ánh/sự cố, thống kê). " +
                   "Bạn có thể hỏi lại rõ hơn theo các chủ đề này nhé.";
        }

        // ==================== 2. AI GỢI Ý XẾP PHÒNG (UC004) ====================

        public async Task<string> GoiYPhongAsync(DangKyO yeuCau)
        {
            var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.Id == yeuCau.SinhVienId);
            if (sv == null) return "Không tìm thấy thông tin sinh viên để gợi ý.";

            // Ưu tiên rule-based (nhanh, ổn định) vì đây là gợi ý nghiệp vụ có ràng buộc rõ (giới tính, sức chứa).
            var phongPhuHop = await _db.Phongs
                .Include(p => p.DanhSachGiuong).ThenInclude(g => g.SinhVien)
                .Where(p => p.TrangThai != TrangThaiPhong.BaoTri)
                .ToListAsync();

            var ketQua = phongPhuHop
                .Where(p => p.SoGiuongTrong > 0)
                // Chỉ gợi ý phòng đang trống hoàn toàn, hoặc phòng đã có người CÙNG giới tính
                .Where(p => !p.DanhSachGiuong.Any(g => g.SinhVien != null) ||
                            p.DanhSachGiuong.Where(g => g.SinhVien != null).All(g => g.SinhVien!.GioiTinh == sv.GioiTinh))
                .Select(p => new
                {
                    Phong = p,
                    CungLop = p.DanhSachGiuong.Any(g => g.SinhVien != null && g.SinhVien.Lop == sv.Lop),
                    CungKhoa = p.DanhSachGiuong.Any(g => g.SinhVien != null && g.SinhVien.Khoa == sv.Khoa)
                })
                .OrderByDescending(x => x.CungLop)
                .ThenByDescending(x => x.CungKhoa)
                .ThenByDescending(x => x.Phong.SoGiuongTrong)
                .Take(3)
                .ToList();

            if (!ketQua.Any())
                return "Hiện không có phòng nào còn giường trống phù hợp (cùng giới tính) để gợi ý. Đề nghị xử lý thủ công hoặc chờ có phòng trống.";

            var sb = new StringBuilder();
            sb.AppendLine($"Gợi ý xếp phòng cho sinh viên {sv.MSSV} ({sv.GioiTinh}, lớp {sv.Lop}):");
            int stt = 1;
            foreach (var item in ketQua)
            {
                var lyDo = item.CungLop ? "có bạn cùng lớp đang ở" : item.CungKhoa ? "có sinh viên cùng khoa đang ở" : "còn nhiều giường trống, phù hợp giới tính";
                sb.AppendLine($"{stt}. Phòng {item.Phong.SoPhong} - còn {item.Phong.SoGiuongTrong} giường trống ({lyDo}).");
                stt++;
            }

            // Nếu có cấu hình API key, thử xin thêm một câu nhận xét ngắn từ LLM (không bắt buộc)
            if (!string.IsNullOrWhiteSpace(ApiKey))
            {
                var promptThem = await GoiLLMAsync(
                    "Bạn là trợ lý xếp phòng ký túc xá. Hãy viết 1 câu nhận xét ngắn gọn (dưới 30 từ) bằng tiếng Việt về danh sách gợi ý phòng dưới đây, không lặp lại số liệu.",
                    sb.ToString());
                if (promptThem != null) sb.AppendLine().Append("Nhận xét thêm: ").Append(promptThem);
            }

            return sb.ToString();
        }

        // ==================== 3. AI TÓM TẮT PHẢN ÁNH (UC007) ====================

        public async Task<string> TomTatPhanAnhAsync(List<PhanAnhSuCo> danhSachPhanAnh)
        {
            if (!danhSachPhanAnh.Any()) return "Chưa có phản ánh/sự cố nào được ghi nhận trong hệ thống.";

            var theoLoai = danhSachPhanAnh.GroupBy(p => p.LoaiPhanAnh)
                .Select(g => $"{ChuyenLoai(g.Key)}: {g.Count()}")
                .ToList();

            var theoTrangThai = danhSachPhanAnh.GroupBy(p => p.TrangThai)
                .Select(g => $"{ChuyenTrangThai(g.Key)}: {g.Count()}")
                .ToList();

            var tomTatCoBan = $"Tổng {danhSachPhanAnh.Count} phản ánh/sự cố. Phân loại: {string.Join(", ", theoLoai)}. " +
                               $"Trạng thái xử lý: {string.Join(", ", theoTrangThai)}.";

            if (string.IsNullOrWhiteSpace(ApiKey))
                return tomTatCoBan;

            var noiDungGopLai = string.Join("\n", danhSachPhanAnh.Select(p => $"- [{ChuyenLoai(p.LoaiPhanAnh)}] {p.TieuDe}: {p.NoiDung}"));
            var systemPrompt = "Bạn là trợ lý AI của hệ thống quản lý ký túc xá. Dựa trên danh sách phản ánh/sự cố dưới đây, " +
                                "hãy viết một đoạn tóm tắt ngắn (3-5 câu) bằng tiếng Việt nêu bật các vấn đề nổi cộm và đề xuất hướng xử lý ưu tiên. " +
                                "Chỉ dựa trên dữ liệu được cung cấp, không bịa thêm thông tin.";

            var ketQuaAI = await GoiLLMAsync(systemPrompt, noiDungGopLai);
            return ketQuaAI != null ? $"{tomTatCoBan}\n\nTóm tắt AI: {ketQuaAI}" : tomTatCoBan;
        }

        private static string ChuyenLoai(LoaiPhanAnh loai) => loai switch
        {
            LoaiPhanAnh.SuCoCoSoVatChat => "Sự cố cơ sở vật chất",
            LoaiPhanAnh.ViPhamNoiQuy => "Vi phạm nội quy",
            _ => "Khác"
        };

        private static string ChuyenTrangThai(TrangThaiPhanAnh tt) => tt switch
        {
            TrangThaiPhanAnh.Moi => "Mới",
            TrangThaiPhanAnh.DangXuLy => "Đang xử lý",
            TrangThaiPhanAnh.DaXuLy => "Đã xử lý",
            TrangThaiPhanAnh.TuChoi => "Từ chối",
            _ => tt.ToString()
        };
    }
}
