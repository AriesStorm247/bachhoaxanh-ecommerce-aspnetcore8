namespace WebBanHang.Services
{
    public static class UserRoleGroups
    {
        // Các role thực tế trong SQL: Admin, Manager, Staff, User
        public static readonly string[] StaffRoles = { "Manager", "Staff" };
        public static readonly string[] CustomerRoles = { "Customer" };

        public static bool IsAdminRole(string? roleName)
            => string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase);

        public static bool IsManagerRole(string? roleName)
            => string.Equals(roleName, "Manager", StringComparison.OrdinalIgnoreCase);

        public static bool IsStaffOnlyRole(string? roleName)
            => string.Equals(roleName, "Staff", StringComparison.OrdinalIgnoreCase);

        // Admin + Manager + Staff đều là nhân sự (dùng cho HRM filter)
        public static bool IsAnyStaffRole(string? roleName)
            => IsAdminRole(roleName) || IsManagerRole(roleName) || IsStaffOnlyRole(roleName);

        // Staff hiển thị trong dropdown gán quyền (không gán được Admin)
        public static bool IsStaffRole(string? roleName)
            => IsManagerRole(roleName) || IsStaffOnlyRole(roleName);

        public static bool IsCustomerRole(string? roleName)
            => CustomerRoles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
    }
}
