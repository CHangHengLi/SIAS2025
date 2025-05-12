using System;
using System.Collections.Generic;
using System.Linq;

namespace SIASGraduate.Models
{
    /// <summary>
    /// 投票记录汇总类，用于显示同一用户对同一提名的所有投票
    /// </summary>
    public class VoteRecordSummary
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
        /// 员工ID
        /// </summary>
        public int? VoterEmployeeId { get; set; }

        /// <summary>
        /// 员工对象
        /// </summary>
        public Employee VoterEmployee { get; set; }

        /// <summary>
        /// 管理员ID
        /// </summary>
        public int? VoterAdminId { get; set; }

        /// <summary>
        /// 管理员对象
        /// </summary>
        public Admin VoterAdmin { get; set; }

        /// <summary>
        /// 最后投票时间
        /// </summary>
        public DateTime LastVoteTime { get; set; }

        /// <summary>
        /// 投票总数
        /// </summary>
        public int VoteCount { get; set; }

        /// <summary>
        /// 原始投票记录列表
        /// </summary>
        public List<VoteRecord> OriginalVoteRecords { get; set; } = new List<VoteRecord>();

        /// <summary>
        /// 从投票记录列表创建汇总
        /// </summary>
        /// <param name="voteRecords">投票记录列表</param>
        /// <returns>汇总后的列表</returns>
        public static List<VoteRecordSummary> CreateFromVoteRecords(IEnumerable<VoteRecord> voteRecords)
        {
            if (voteRecords == null) return new List<VoteRecordSummary>();

            var result = new List<VoteRecordSummary>();

            // 按用户分组
            var employeeGroups = voteRecords.Where(v => v.VoterEmployeeId.HasValue)
                .GroupBy(v => v.VoterEmployeeId);

            foreach (var group in employeeGroups)
            {
                var votesList = group.OrderByDescending(v => v.VoteTime).ToList();
                if (votesList.Count > 0)
                {
                    var firstVote = votesList.First();
                    var summary = new VoteRecordSummary
                    {
                        NominationId = firstVote.NominationId,
                        AwardId = firstVote.AwardId,
                        VoterEmployeeId = firstVote.VoterEmployeeId,
                        VoterEmployee = firstVote.VoterEmployee,
                        LastVoteTime = firstVote.VoteTime,
                        VoteCount = votesList.Count,
                        OriginalVoteRecords = votesList
                    };
                    result.Add(summary);
                }
            }

            // 按管理员分组
            var adminGroups = voteRecords.Where(v => v.VoterAdminId.HasValue)
                .GroupBy(v => v.VoterAdminId);

            foreach (var group in adminGroups)
            {
                var votesList = group.OrderByDescending(v => v.VoteTime).ToList();
                if (votesList.Count > 0)
                {
                    var firstVote = votesList.First();
                    var summary = new VoteRecordSummary
                    {
                        NominationId = firstVote.NominationId,
                        AwardId = firstVote.AwardId,
                        VoterAdminId = firstVote.VoterAdminId,
                        VoterAdmin = firstVote.VoterAdmin,
                        LastVoteTime = firstVote.VoteTime,
                        VoteCount = votesList.Count,
                        OriginalVoteRecords = votesList
                    };
                    result.Add(summary);
                }
            }

            return result.OrderByDescending(s => s.LastVoteTime).ToList();
        }
    }
} 