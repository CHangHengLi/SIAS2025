using System.ComponentModel.DataAnnotations;

namespace _2025毕业设计.Models
{
    /// <summary>
    /// 奖项表
    /// </summary>
    public class Award
    {
        /// <summary>
        /// 奖项Id 主键,自增
        /// </summary>
        [Key]
        public int AwardId { get; set; }

        /// <summary>
        /// 奖项名称
        /// </summary>

        [StringLength(20)]
        public required string AwardName { get; set; }

        /// <summary>
        /// 奖项描述
        /// </summary>
        [StringLength(100)]
        public string? AwardDescription { get; set; }

        /// <summary>
        /// 奖项最大投票次数
        /// </summary>
        public int MaxVoteCount { get; set; } = 1;

        /// <summary>
        /// 奖项封面图片路径
        /// </summary>
        [StringLength(200)]
        public string? CoverImage { get; set; }

        /// <summary>
        /// 奖项封面图片数据
        /// </summary>
        public byte[]? CoverImageData { get; set; }

        /// <summary>
        /// 奖项的提名列表
        /// </summary>
        public virtual ICollection<Nomination>? Nominations { get; set; }

        public override string ToString()
        {
            return AwardName ?? $"奖项ID:{AwardId}";
        }
    }
}
