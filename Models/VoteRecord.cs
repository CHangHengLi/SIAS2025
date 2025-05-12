using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIASGraduate.Models
{
    /// <summary>
    /// 投票记录实体类
    /// </summary>
    public class VoteRecord
    {
        /// <summary>
        /// 投票记录ID
        /// </summary>
        [Key]
        public int VoteRecordId { get; set; }

        /// <summary>
        /// 奖项ID
        /// </summary>
        public int AwardId { get; set; }

        /// <summary>
        /// 奖项导航属性
        /// </summary>
        [ForeignKey("AwardId")]
        public Award Award { get; set; }

        /// <summary>
        /// 提名ID
        /// </summary>
        public int NominationId { get; set; }

        /// <summary>
        /// 提名导航属性
        /// </summary>
        [ForeignKey("NominationId")]
        public Nomination Nomination { get; set; }

        /// <summary>
        /// 投票人Id - 员工
        /// </summary>
        public int? VoterEmployeeId { get; set; }

        /// <summary>
        /// 投票人导航属性 - 员工
        /// </summary>
        [ForeignKey("VoterEmployeeId")]
        public Employee VoterEmployee { get; set; }

        /// <summary>
        /// 投票人Id - 管理员
        /// </summary>
        public int? VoterAdminId { get; set; }

        /// <summary>
        /// 投票人导航属性 - 管理员
        /// </summary>
        [ForeignKey("VoterAdminId")]
        public Admin VoterAdmin { get; set; }

        /// <summary>
        /// 投票时间
        /// </summary>
        public DateTime VoteTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 投票时间别名（向后兼容）
        /// </summary>
        [NotMapped]
        public DateTime VoteDate 
        { 
            get { return VoteTime; }
            set { VoteTime = value; }
        }

        /// <summary>
        /// 投票描述
        /// </summary>
        [NotMapped]
        public string VoteDescription { get; set; }
    }
}
