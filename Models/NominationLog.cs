using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIASGraduate.Models
{
    /// <summary>
    /// 申报审核日志
    /// </summary>
    public class NominationLog
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        [Key]
        public int LogId { get; set; }
        
        /// <summary>
        /// 关联的申报ID
        /// </summary>
        public int DeclarationId { get; set; }
        
        /// <summary>
        /// 关联的申报
        /// </summary>
        [ForeignKey("DeclarationId")]
        public virtual NominationDeclaration? Declaration { get; set; }
        
        /// <summary>
        /// 操作类型：1-提交申报，2-审核通过，3-审核拒绝，4-转为提名，5-编辑操作，6-删除操作
        /// </summary>
        public int OperationType { get; set; }
        
        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperationTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 操作人-员工ID
        /// </summary>
        public int? OperatorEmployeeId { get; set; }
        
        /// <summary>
        /// 操作人-员工
        /// </summary>
        [ForeignKey("OperatorEmployeeId")]
        public virtual Employee? OperatorEmployee { get; set; }
        
        /// <summary>
        /// 操作人-管理员ID
        /// </summary>
        public int? OperatorAdminId { get; set; }
        
        /// <summary>
        /// 操作人-管理员
        /// </summary>
        [ForeignKey("OperatorAdminId")]
        public virtual Admin? OperatorAdmin { get; set; }
        
        /// <summary>
        /// 操作人-超级管理员ID
        /// </summary>
        public int? OperatorSupAdminId { get; set; }
        
        /// <summary>
        /// 操作人-超级管理员
        /// </summary>
        [ForeignKey("OperatorSupAdminId")]
        public virtual SupAdmin? OperatorSupAdmin { get; set; }
        
        /// <summary>
        /// 操作内容/备注
        /// </summary>
        public string? Content { get; set; }
        
        /// <summary>
        /// 操作人名称（UI属性，不映射到数据库）
        /// </summary>
        [NotMapped]
        public string OperatorName
        {
            get
            {
                if (OperatorSupAdmin != null) return OperatorSupAdmin.SupAdminName ?? "未知";
                if (OperatorAdmin != null) return OperatorAdmin.AdminName ?? "未知";
                if (OperatorEmployee != null) return OperatorEmployee.EmployeeName ?? "未知";
                return "未知";
            }
        }
        
        /// <summary>
        /// 操作类型文本（UI属性，不映射到数据库）
        /// </summary>
        [NotMapped]
        public string OperationTypeText
        {
            get
            {
                return OperationType switch
                {
                    1 => "提交申报",
                    2 => "审核通过",
                    3 => "审核拒绝",
                    4 => "转为提名",
                    5 => "编辑操作",
                    6 => "删除操作",
                    _ => "未知操作"
                };
            }
        }
    }
} 