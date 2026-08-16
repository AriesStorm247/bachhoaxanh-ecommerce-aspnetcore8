using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.ViewModels;
using System.Linq;

namespace WebBanHang.Controllers
{
    public class AdminController
    : Controller
    {
        private readonly
        ApplicationDbContext
        _context;

        public AdminController(
            ApplicationDbContext context)
        {
            _context =
            context;
        }

        public async Task<IActionResult>
        ChatHistory()
        {
            var query = from ch in _context.ChatHistories
                        join u in _context.Users on ch.UserId equals u.Id into userGroup
                        from u in userGroup.DefaultIfEmpty()
                        join p in _context.CustomerProfiles on ch.UserId equals p.UserId into profileGroup
                        from p in profileGroup.DefaultIfEmpty()
                        orderby ch.CreatedAt descending
                        select new ChatHistoryViewModel
                        {
                            Id = ch.Id,
                            Question = ch.Question,
                            Answer = ch.Answer,
                            CreatedAt = ch.CreatedAt,
                            UserId = ch.UserId,
                            FullName = p != null ? p.FullName : "",
                            Email = u != null ? u.Email : ""
                        };

            var data = await query.ToListAsync();

            return View(data);
        }
    }
}