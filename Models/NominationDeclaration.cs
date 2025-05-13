using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace SIASGraduate.Models
{
    /// <summary>
    /// 申报提名表
    /// </summary>
    public class NominationDeclaration : INotifyPropertyChanged
    {
        /// <summary>
        /// 实现INotifyPropertyChanged接口
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 申报编号
        /// </summary>
        [Key]
        public int DeclarationId { get; set; }

        /// <summary>
        /// 申报奖项Id
        /// </summary>
        public int AwardId { get; set; }

        /// <summary>
        /// 申报奖项
        /// </summary>
        [ForeignKey("AwardId")]
        public virtual Award? Award { get; set; }

        /// <summary>
        /// 申报对象-员工
        /// </summary>
        public int? NominatedEmployeeId { get; set; }
        [ForeignKey("NominatedEmployeeId")]
        public virtual Employee? NominatedEmployee { get; set; }

        /// <summary>
        /// 申报对象-管理员
        /// </summary>
        public int? NominatedAdminId { get; set; }
        [ForeignKey("NominatedAdminId")]
        public virtual Admin? NominatedAdmin { get; set; }

        /// <summary>
        /// 所属部门Id
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// 所属部门
        /// </summary>
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        /// <summary>
        /// 一句话介绍
        /// </summary>
        public string? Introduction { get; set; } = string.Empty;

        /// <summary>
        /// 奖项申报图片
        /// </summary>
        public byte[]? CoverImage { get; set; }

        /// <summary>
        /// 申报理由
        /// </summary>
        public string? DeclarationReason { get; set; } = string.Empty;

        /// <summary>
        /// 申报人-员工ID
        /// </summary>
        public int? DeclarerEmployeeId { get; set; }
        
        /// <summary>
        /// 申报人-员工
        /// </summary>
        [ForeignKey("DeclarerEmployeeId")]
        public virtual Employee? DeclarerEmployee { get; set; }

        /// <summary>
        /// 申报人-管理员ID
        /// </summary>
        public int? DeclarerAdminId { get; set; }
        
        /// <summary>
        /// 申报人-管理员
        /// </summary>
        [ForeignKey("DeclarerAdminId")]
        public virtual Admin? DeclarerAdmin { get; set; }

        /// <summary>
        /// 申报人-超级管理员ID
        /// </summary>
        public int? DeclarerSupAdminId { get; set; }
        
        /// <summary>
        /// 申报人-超级管理员
        /// </summary>
        [ForeignKey("DeclarerSupAdminId")]
        public virtual SupAdmin? DeclarerSupAdmin { get; set; }

        /// <summary>
        /// 申报时间
        /// </summary>
        public DateTime DeclarationTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 审核状态：0-待审核，1-已通过，2-已拒绝
        /// </summary>
        public int Status { get; set; } = 0;

        /// <summary>
        /// 审核人-员工ID
        /// </summary>
        public int? ReviewerEmployeeId { get; set; }
        
        /// <summary>
        /// 审核人-员工
        /// </summary>
        [ForeignKey("ReviewerEmployeeId")]
        public virtual Employee? ReviewerEmployee { get; set; }
        
        /// <summary>
        /// 审核人-管理员ID
        /// </summary>
        public int? ReviewerAdminId { get; set; }
        
        /// <summary>
        /// 审核人-管理员
        /// </summary>
        [ForeignKey("ReviewerAdminId")]
        public virtual Admin? ReviewerAdmin { get; set; }
        
        /// <summary>
        /// 审核人-超级管理员ID
        /// </summary>
        public int? ReviewerSupAdminId { get; set; }
        
        /// <summary>
        /// 审核人-超级管理员
        /// </summary>
        [ForeignKey("ReviewerSupAdminId")]
        public virtual SupAdmin? ReviewerSupAdmin { get; set; }
        
        /// <summary>
        /// 审核时间
        /// </summary>
        public DateTime? ReviewTime { get; set; }
        
        /// <summary>
        /// 审核意见
        /// </summary>
        public string? ReviewComment { get; set; }
        
        /// <summary>
        /// 是否已转为正式提名
        /// </summary>
        public bool IsPromoted { get; set; } = false;
        
        /// <summary>
        /// 转为正式提名ID
        /// </summary>
        public int? PromotedNominationId { get; set; }
        
        /// <summary>
        /// 是否有效（UI属性，不映射到数据库）
        /// </summary>
        [NotMapped]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 状态文本（UI属性，不映射到数据库）
        /// </summary>
        [NotMapped]
        public string StatusText
        {
            get
            {
                return Status switch
                {
                    0 => "待审核",
                    1 => "已通过",
                    2 => "已拒绝",
                    _ => "未知状态"
                };
            }
        }
        
        /// <summary>
        /// 审核人名称（UI属性，不映射到数据库）
        /// </summary>
        [NotMapped]
        public string ReviewerName
        {
            get
            {
                // 1. 状态为0表示待审核，直接返回"未审核"
                if (Status == 0) return "未审核";
                
                // 2. 如果有明确设置审核人ID，但未加载关联实体，尝试获取更简洁的信息
                if ((ReviewerSupAdminId.HasValue || ReviewerAdminId.HasValue || ReviewerEmployeeId.HasValue) && 
                    ReviewerSupAdmin == null && ReviewerAdmin == null && ReviewerEmployee == null)
                {
                    // 只返回ID，不显示角色前缀
                    if (ReviewerSupAdminId.HasValue) return $"ID:{ReviewerSupAdminId}";
                    if (ReviewerAdminId.HasValue) return $"ID:{ReviewerAdminId}";
                    if (ReviewerEmployeeId.HasValue) return $"ID:{ReviewerEmployeeId}";
                }
                
                // 3. 状态非0但缺少审核人信息，返回更明确的信息
                if (!ReviewerSupAdminId.HasValue && !ReviewerAdminId.HasValue && !ReviewerEmployeeId.HasValue)
                {
                    return Status == 1 ? "系统自动通过" : (Status == 2 ? "系统自动拒绝" : "未知审核人");
                }
                
                // 4. 有审核人信息时直接返回名称，不加角色前缀
                if (ReviewerSupAdmin != null) return ReviewerSupAdmin.SupAdminName ?? "未知";
                if (ReviewerAdmin != null) return ReviewerAdmin.AdminName ?? "未知";
                if (ReviewerEmployee != null) return ReviewerEmployee.EmployeeName ?? "未知";
                
                // 5. 兜底情况，仍然显示未知审核人
                return "未知审核人";
            }
        }
        
        /// <summary>
        /// 申报人名称（UI属性，不映射到数据库）
        /// </summary>
        [NotMapped]
        public string DeclarerName
        {
            get
            {
                if (DeclarerSupAdmin != null) return DeclarerSupAdmin.SupAdminName ?? "未知";
                if (DeclarerAdmin != null) return DeclarerAdmin.AdminName ?? "未知";
                if (DeclarerEmployee != null) return DeclarerEmployee.EmployeeName ?? "未知";
                return "未知";
            }
        }
        
        /// <summary>
        /// 被提名人名称（UI属性，不映射到数据库）
        /// </summary>
        [NotMapped]
        public string NominatedName
        {
            get
            {
                if (NominatedEmployee != null) return NominatedEmployee.EmployeeName ?? "未知";
                if (NominatedAdmin != null) return NominatedAdmin.AdminName ?? "未知";
                return "未知";
            }
        }
    }
} 