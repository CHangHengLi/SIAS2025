using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace _2025毕业设计.Models
{
    /// <summary>
    /// 奖项提名表
    /// </summary>
    public class Nomination : INotifyPropertyChanged
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
        /// 奖项提名编号
        /// </summary>
        [Key]
        public int NominationId { get; set; }

        /// <summary>
        /// 提报奖项Id
        /// </summary>
        public int AwardId { get; set; }

        /// <summary>
        /// 提报奖项
        /// </summary>
        [ForeignKey("AwardId")]
        public virtual Award? Award { get; set; }

        /// <summary>
        /// 提报对象-员工
        /// </summary>
        public int? NominatedEmployeeId { get; set; }
        [ForeignKey("NominatedEmployeeId")]
        public virtual Employee? NominatedEmployee { get; set; }

        /// <summary>
        /// 提报对象-管理员
        /// </summary>
        public int? NominatedAdminId { get; set; }
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
        /// 奖项提名图片
        /// </summary>
        public byte[]? CoverImage { get; set; }

        /// <summary>
        /// 提名理由
        /// </summary>
        public string? NominateReason { get; set; } = string.Empty;

        /// <summary>
        /// 提议人-员工ID
        /// </summary>
        public int? ProposerEmployeeId { get; set; }
        
        /// <summary>
        /// 提议人-员工
        /// </summary>
        [ForeignKey("ProposerEmployeeId")]
        public virtual Employee? ProposerEmployee { get; set; }

        /// <summary>
        /// 提议人-管理员ID
        /// </summary>
        public int? ProposerAdminId { get; set; }
        
        /// <summary>
        /// 提议人-管理员
        /// </summary>
        [ForeignKey("ProposerAdminId")]
        public virtual Admin? ProposerAdmin { get; set; }

        /// <summary>
        /// 提议人-超级管理员
        /// </summary>
        public int? ProposerSupAdminId { get; set; }
        public virtual SupAdmin? ProposerSupAdmin { get; set; }

        /// <summary>
        /// 提名时间
        /// </summary>
        public DateTime NominationTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 提名时间别名（向后兼容）
        /// </summary>
        [NotMapped]
        public DateTime NominateTime 
        { 
            get { return NominationTime; }
            set { NominationTime = value; }
        }

        /// <summary>
        /// 是否有效（不映射到数据库）
        /// </summary>
        [NotMapped]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 评论区是否可见
        /// </summary>
        [NotMapped]
        public bool IsCommentSectionVisible { get; set; }

        /// <summary>
        /// UI显示用评论列表 (非数据库字段)
        /// </summary>
        [NotMapped]
        public ObservableCollection<CommentRecord> UIComments { get; set; } = new ObservableCollection<CommentRecord>();

        /// <summary>
        /// 新评论内容
        /// </summary>
        [NotMapped]
        public string NewCommentText { get; set; } = string.Empty;

        /// <summary>
        /// 评论数量
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// 最新评论时间
        /// </summary>
        public DateTime? LastCommentTime { get; set; }

        /// <summary>
        /// 最新评论者ID (可能是员工、管理员或超级管理员)
        /// </summary>
        public int? LastCommenterEmployeeId { get; set; }
        public int? LastCommenterAdminId { get; set; }
        public int? LastCommenterSupAdminId { get; set; }

        /// <summary>
        /// 最新评论内容预览（存储前80个字符）
        /// </summary>
        public string? LastCommentPreview { get; set; }

        /// <summary>
        /// 当前评论页码
        /// </summary>
        [NotMapped]
        public int CurrentCommentPage { get; set; } = 1;

        /// <summary>
        /// 是否有更多评论
        /// </summary>
        [NotMapped]
        public bool HasMoreComments { get; set; }

        /// <summary>
        /// 当前用户是否已投票（不映射到数据库）
        /// </summary>
        [NotMapped]
        public bool IsUserVoted { get; set; }

        /// <summary>
        /// 关联的投票记录
        /// </summary>
        public virtual ObservableCollection<VoteRecord> VoteRecords { get; set; } = new ObservableCollection<VoteRecord>();

        /// <summary>
        /// 评论列表
        /// </summary>
        public virtual ICollection<CommentRecord>? CommentRecords { get; set; }

        /// <summary>
        /// 重写ToString方法
        /// </summary>
        public override string ToString()
        {
            string nominatedName = NominatedEmployee?.EmployeeName ?? NominatedAdmin?.AdminName ?? "未知";
            string departmentName = Department?.DepartmentName ?? "无部门";
            return $"{nominatedName} - {departmentName} - 得票数:{VoteRecords?.Count ?? 0}";
        }
    }
}
