using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class ChatRequest
    {
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public string? ImageMimeType { get; set; }

        public string? ImageData { get; set; }
    }
}
