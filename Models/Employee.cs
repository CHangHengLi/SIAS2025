using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIASGraduate.Models
{
    /// <summary>
    /// 雇员表
    /// </summary>
    public class Employee

    {
        /// <summary>
        /// 员工ID
        /// </summary>
        [Key]
        public int EmployeeId { get; set; }

        /// <summary>
        /// 员工头像
        /// </summary>
        public byte[]? EmployeeImage { get; set; }

        /// <summary>
        /// 员工账号
        /// </summary>
        [StringLength(6, MinimumLength = 6, ErrorMessage = "账号必须为6位数")]
        [Required(ErrorMessage = "账号不能为空")]
        public string Account { get; set; }

        /// <summary>
        /// 员工姓名
        /// </summary>
        // 非空唯一, 长度不超过20
        [StringLength(20, ErrorMessage = "账号名称长度不能超过20")]
        public required string EmployeeName { get; set; }

        /// <summary>
        /// 员工密码
        /// </summary>
        // 非空长度不超过20
        [StringLength(20, ErrorMessage = "密码长度不能超过20")]
        public required string EmployeePassword { get; set; }

        /// <summary>
        /// 员工邮箱
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
        /// 员工角色ID
        /// </summary>
        public int? RoleId { get; set; } = 3;

        /// <summary>
        /// 用于显示的名称
        /// </summary>
        [NotMapped]
        public string DisplayName => Department != null 
            ? $"{EmployeeName} ({Department.DepartmentName})" 
            : EmployeeName;

        /// <summary>
        /// 员工文件数据
        /// </summary>
        public byte[]? EmployeeFileData { get; set; }
    }
}
