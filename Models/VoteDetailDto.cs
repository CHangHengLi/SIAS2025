namespace _2025毕业设计.Models
{
    /// <summary>
    /// 投票详情数据传输对象
    /// </summary>
    public class VoteDetailDto
    {
        /// <summary>
        /// 提名ID
        /// </summary>
        public int NominationId { get; set; }

        /// <summary>
        /// 奖项ID
        /// </summary>
        public int AwardId { get; set; }

        /// <summary>
        /// 奖项名称
        /// </summary>
        public string AwardName { get; set; }

        /// <summary>
        /// 部门ID
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// 部门名称
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// 被提名人姓名（可能是员工或管理员）
        /// </summary>
        public string NomineeName { get; set; }

        /// <summary>
        /// 一句话介绍
        /// </summary>
        public string Introduction { get; set; }

        /// <summary>
        /// 提名理由
        /// </summary>
        public string NominateReason { get; set; }

        /// <summary>
        /// 得票数
        /// </summary>
        public int VoteCount { get; set; }

        /// <summary>
        /// 员工票数
        /// </summary>
        public int EmployeeVoteCount { get; set; }

        /// <summary>
        /// 管理员票数
        /// </summary>
        public int AdminVoteCount { get; set; }
    }
}