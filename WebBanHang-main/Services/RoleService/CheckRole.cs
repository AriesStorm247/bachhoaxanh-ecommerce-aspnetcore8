using System.Security.Claims;
using WebBanHang.Data;

namespace WebBanHang.Services
{
    public class RoleService
    {
        private readonly ApplicationDbContext _context;

        public RoleService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Trả về số thứ tự role:
        /// 0 = Chưa đăng nhập / không xác định
        /// 1 = Admin
        /// 2 = Manager
        /// 3 = Staff
        /// 4 = User (Khách hàng đã đăng nhập)
        /// </summary>
        public int GetRole(ClaimsPrincipal user)
        {
            if (user == null || !user.Identity!.IsAuthenticated)
                return 0;

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return 0;

            var roleName = (from ur in _context.UserRoles
                            join r in _context.Roles on ur.RoleId equals r.Id
                            where ur.UserId == userId
                            select r.Name)
                            .FirstOrDefault();

            if (UserRoleGroups.IsAdminRole(roleName))   return 1;
            if (UserRoleGroups.IsManagerRole(roleName)) return 2;
            if (UserRoleGroups.IsStaffOnlyRole(roleName)) return 3;
            if (string.Equals(roleName, "Shipper", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(roleName, "Thu ngân", StringComparison.OrdinalIgnoreCase) || string.Equals(roleName, "Thu ngan", StringComparison.OrdinalIgnoreCase)) return 5;
            if (UserRoleGroups.IsCustomerRole(roleName)) return 0;

            return 0;
        }
    }
}
