using System;

namespace WebBanHang.ViewModels
{
    public class ChatHistoryViewModel
    {
        public int Id { get; set; }
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }
}
