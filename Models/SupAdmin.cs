using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2025毕业设计.Models
{
    /// <summary>
    /// 超级管理员
    /// </summary>
    public class SupAdmin
    {
        /// <summary>
        /// 超级管理员ID
        /// </summary>
        [Key]
        public int SupAdminId { get; set; }
        /// <summary>
        /// 超级管理员头像
        /// </summary>
        public byte[]? SupAdminImage { get; set; }
        /// <summary>
        /// 超级管理员账号
        /// </summary>
        [StringLength(6, MinimumLength = 6, ErrorMessage = "账号必须为6位数")]
        [Required(ErrorMessage = "账号不能为空")]
        public string Account { get; set; }
        /// <summary>
        /// 超级管理员名称
        /// </summary>
        [StringLength(20, ErrorMessage = "超级管理员名称长度不能超过20")]
        public required string SupAdminName { get; set; }
        /// <summary>
        /// 超级管理员密码
        /// </summary>
        [StringLength(20, ErrorMessage = "密码长度不能超过20")]
        public required string SupAdminPassword { get; set; }
        /// <summary>
        /// 超级管理员权限级别
        /// </summary>
        public int? RoleId { get; set; } = 1;
        
        /// <summary>
        /// 用于显示的名称
        /// </summary>
        [NotMapped]
        public string DisplayName => $"{SupAdminName} (超级管理员)";

        [EmailAddress(ErrorMessage = "请输入有效的电子邮件地址")]
        public string? Email { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
