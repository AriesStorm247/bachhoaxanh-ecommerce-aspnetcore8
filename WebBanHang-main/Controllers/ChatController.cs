using Microsoft.AspNetCore.Mvc;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;
using System.Security.Claims;

namespace WebBanHang.Controllers
{
    public class ChatController : Controller
    {
        private readonly OpenAIService _ai;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            OpenAIService ai,
            ApplicationDbContext context,
            ILogger<ChatController> logger)
        {
            _ai = ai;
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest req)
        {
            try
            {
                var answer = await _ai.AskAI(req.Message, req.ImageMimeType, req.ImageData);
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var questionText = req.Message;
                if (!string.IsNullOrEmpty(req.ImageData))
                {
                    questionText = "[Gửi kèm hình ảnh] " + questionText;
                }

                _context.ChatHistories.Add(new ChatHistory
                {
                    Question = questionText,
                    Answer = answer,
                    CreatedAt = DateTime.Now,
                    UserId = currentUserId
                });

                await _context.SaveChangesAsync();
                return Json(new { reply = answer });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Lỗi kết nối tới Gemini API: {Message}", httpEx.Message);
                return Json(new { reply = "❌ Hiện tại trợ lý AI đang bận, vui lòng thử lại sau hoặc liên hệ hotline **1900 1908** để được hỗ trợ trực tiếp." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý câu hỏi AI: {Message}", ex.Message);
                return Json(new { reply = "❌ Xảy ra lỗi khi xử lý câu hỏi của bạn. Vui lòng thử lại sau hoặc liên hệ hỗ trợ: **Võ Văn Phú - 0779753643**." });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public string? ImageMimeType { get; set; }
        public string? ImageData { get; set; }
    }
}