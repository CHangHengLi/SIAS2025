using System.ComponentModel.DataAnnotations;

namespace SIASGraduate.Models
{
    /// <summary>
    /// 部门表
    /// </summary>
    public class Department
    {
        /// <summary>
        /// 主键,部门编号
        /// </summary>
        [Key]
        public int DepartmentId { get; set; }

        /// <summary>
        /// 部门名称
        /// </summary>
        [StringLength(20)]
        public required string DepartmentName { get; set; }
    }
}
