using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class ChatHistory
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public string Question
        {
            get;
            set;
        }
        =
        "";

        public string Answer
        {
            get;
            set;
        }
        =
        "";

        public DateTime CreatedAt
        {
            get;
            set;
        }

        public string? UserId
        {
            get;
            set;
        }
    }
}