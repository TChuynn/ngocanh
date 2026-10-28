using KTXManagement.Demo.Data;
using KTXManagement.Demo.Models.ViewModels;
using KTXManagement.Demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KTXManagement.Demo.Controllers
{
    // Tính năng AI Hỏi-Đáp (Chatbot) - trả lời dựa trên đúng tri thức/dữ liệu của dự án KTX
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAIService _aiService;

        public ChatController(ApplicationDbContext db, IAIService aiService)
        {
            _db = db;
            _aiService = aiService;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            var taiKhoanId = User.GetTaiKhoanId();
            var taiKhoan = await _db.TaiKhoans.FindAsync(taiKhoanId);

            var traLoi = await _aiService.TraLoiAsync(request.Message, taiKhoan);
            return Json(new ChatResponse { Reply = traLoi });
        }
    }
}
