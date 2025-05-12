using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _2025毕业设计.Models
{
    /// <summary>
    /// 管理员类
    /// </summary>
    public class Admin
    {
        /// <summary>
        /// 管理员ID
        /// </summary>
        [Key]
        public int AdminId { get; set; }

        /// <summary>
        /// 管理员头像
        /// </summary>
        public byte[]? AdminImage { get; set; }

        /// <summary>
        /// 管理员账号
        /// </summary>
        [StringLength(6, MinimumLength = 6, ErrorMessage = "账号必须为6位数")]
        [Required(ErrorMessage = "账号不能为空")]
        public string Account { get; set; }

        /// <summary>
        /// 管理员账号
        /// </summary>
        // 非空唯一, 长度不超过20
        [StringLength(20, ErrorMessage = "用户名长度不能超过20")]
        public required string AdminName { get; set; }
        /// <summary>
        /// 管理员密码
        /// </summary>
        // 非空长度不超过20
        [StringLength(20, ErrorMessage = "密码长度不能超过20")]
        public required string AdminPassword { get; set; }
        /// <summary>
        /// 管理员邮箱
        /// </summary>
        [EmailAddress(ErrorMessage = "请输入有效的电子邮件地址")]
        public string? Email { get; set; }
        /// <summary>
        /// 员工部门Id
        /// </summary>
        [ForeignKey(nameof(Department))]
        public int? DepartmentId { get; set; }
        /// <summary>
        /// 导航属性：员工所属部门
        /// </summary>
        public virtual Department? Department { get; set; }
        /// <summary>
        /// 入职日期
        /// </summary>
        public DateTime? HireDate { get; set; } = DateTime.Now;
        /// <summary>
        /// 是否在职
        /// </summary>
        public bool? IsActive { get; set; } = true;
        /// <summary>
        /// 管理员角色ID
        /// </summary>
        public int? RoleId { get; set; } = 2;

        /// <summary>
        /// 用于显示的名称，结合部门名称
        /// </summary>
        [NotMapped]
        public string DisplayName => $"{AdminName}{(Department != null ? $" ({Department.DepartmentName})" : "")}";
    }
}
