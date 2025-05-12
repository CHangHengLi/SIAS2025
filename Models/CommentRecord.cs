using System.ComponentModel.DataAnnotations;
using System;

namespace SIASGraduate.Models
{
    public class CommentRecord
    {
        /// <summary>
        /// 评论ID
        /// </summary>
        [Key]
        public int CommentId { get; set; }
        /// <summary>
        /// 评论人-员工
        /// </summary>
        public int? CommenterEmployeeId { get; set; }
        public virtual Employee? CommenterEmployee { get; set; }
        /// <summary>
        /// 评论人-管理员
        /// </summary>
        public int? CommenterAdminId { get; set; }
        public virtual Admin? CommenterAdmin { get; set; }
        /// <summary>
        /// 评论人-超级管理员
        /// </summary>
        public int? CommenterSupAdminId { get; set; }
        public virtual SupAdmin? CommenterSupAdmin { get; set; }
        /// <summary>
        /// 评论时间
        /// </summary>
        public DateTime CommentTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 评论内容
        /// </summary>
        public required string Content { get; set; }
        /// <summary>
        /// 奖项提名编号
        /// </summary>
        public int NominationId { get; set; }
        public virtual Nomination? Nomination { get; set; }
        /// <summary>
        /// 提报奖项ID
        /// </summary>
        public int AwardId { get; set; }
        public virtual Award? Award { get; set; }
        /// <summary>
        /// 评论是否被删除
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        /// <summary>
        /// 评论删除时间
        /// </summary>
        public DateTime? DeletedTime { get; set; }
        /// <summary>
        /// 删除评论的管理员ID
        /// </summary>
        public int? DeletedByAdminId { get; set; }
        public virtual Admin? DeletedByAdmin { get; set; }
        /// <summary>
        /// 删除评论的超级管理员ID
        /// </summary>
        public int? DeletedBySupAdminId { get; set; }
        public virtual SupAdmin? DeletedBySupAdmin { get; set; }
    }
}
