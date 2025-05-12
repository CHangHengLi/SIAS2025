namespace SIASGraduate.Models.Enums
{
    /// <summary>
    /// 提名状态枚举
    /// </summary>
    public enum NominationState
    {
        /// <summary>
        /// 草稿
        /// </summary>
        Draft = 0,
        
        /// <summary>
        /// 已提交
        /// </summary>
        Submitted = 1,
        
        /// <summary>
        /// 已批准
        /// </summary>
        Approved = 2,
        
        /// <summary>
        /// 已拒绝
        /// </summary>
        Rejected = 3,
        
        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 4
    }
} 