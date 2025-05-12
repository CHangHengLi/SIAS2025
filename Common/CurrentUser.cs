namespace _2025毕业设计.Common
{
    public static class CurrentUser
    {
        public static string UserName { get; set; } //当前登录用户名
        public static string Password { get; set; } //当前登录用户密码
        public static byte[]? Image { get; set; } //当前登录用户头像
        public static int RoleId { get; set; } //当前登录用户角色Id
        public static int? AdminId { get; set; } //当前登录管理员Id
        public static int? EmployeeId { get; set; } //当前登录员工Id
        public static string Account { get; set; } //当前登录用户账号
    }
}
