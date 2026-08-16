using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Gửi thông báo chung cho toàn hệ thống
        public async Task<Notification> SendGlobalNotificationAsync(string title, string content, NotificationType type)
        {
            var notification = new Notification
            {
                Title = title,
                Content = content,
                Type = type,
                UserId = null,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        // Gửi thông báo riêng cho từng khách hàng cụ thể
        public async Task<Notification> SendPersonalNotificationAsync(string userId, string title, string content, NotificationType type)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        // Lấy số lượng thông báo chưa đọc của người dùng
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return 0;

            var readNotificationIds = await _context.UserNotificationStates
                .Where(s => s.UserId == userId && s.IsRead)
                .Select(s => s.NotificationId)
                .ToListAsync();

            return await _context.Notifications
                .CountAsync(n => (n.UserId == null || n.UserId == userId) 
                                 && !readNotificationIds.Contains(n.Id));
        }

        // Lấy tất cả thông báo của người dùng (gồm cả global và personal) kèm trạng thái đã đọc hay chưa
        public async Task<List<NotificationItemViewModel>> GetNotificationsAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == null || n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var readStates = await _context.UserNotificationStates
                .Where(s => s.UserId == userId)
                .ToDictionaryAsync(s => s.NotificationId, s => s.IsRead);

            return notifications.Select(n => new NotificationItemViewModel
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                Type = n.Type,
                CreatedAt = n.CreatedAt,
                IsRead = readStates.TryGetValue(n.Id, out var isRead) && isRead,
                IsGlobal = n.UserId == null
            }).ToList();
        }

        // Đánh dấu một thông báo là đã đọc
        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var state = await _context.UserNotificationStates
                .FirstOrDefaultAsync(s => s.NotificationId == notificationId && s.UserId == userId);

            if (state == null)
            {
                state = new UserNotificationState
                {
                    NotificationId = notificationId,
                    UserId = userId,
                    IsRead = true,
                    ReadAt = DateTime.Now
                };
                _context.UserNotificationStates.Add(state);
            }
            else if (!state.IsRead)
            {
                state.IsRead = true;
                state.ReadAt = DateTime.Now;
                _context.Entry(state).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }

        // Đánh dấu tất cả thông báo thuộc về khách hàng là đã đọc
        public async Task MarkAllAsReadAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            // Lấy tất cả thông báo mà người dùng có quyền xem
            var visibleNotifications = await _context.Notifications
                .Where(n => n.UserId == null || n.UserId == userId)
                .ToListAsync();

            var existingStates = await _context.UserNotificationStates
                .Where(s => s.UserId == userId)
                .ToListAsync();

            var existingStatesDict = existingStates.ToDictionary(s => s.NotificationId);

            foreach (var n in visibleNotifications)
            {
                if (!existingStatesDict.TryGetValue(n.Id, out var state))
                {
                    _context.UserNotificationStates.Add(new UserNotificationState
                    {
                        NotificationId = n.Id,
                        UserId = userId,
                        IsRead = true,
                        ReadAt = DateTime.Now
                    });
                }
                else if (!state.IsRead)
                {
                    state.IsRead = true;
                    state.ReadAt = DateTime.Now;
                    _context.Entry(state).State = EntityState.Modified;
                }
            }

            await _context.SaveChangesAsync();
        }
    }

    public class NotificationItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsGlobal { get; set; }
    }
}
