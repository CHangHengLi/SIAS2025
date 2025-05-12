using Prism.Events;

namespace SIASGraduate.Event
{
    /// <summary>
    /// 添加投票记录事件
    /// </summary>
    public class VoteRecordAddedEvent : PubSubEvent<int>
    {
    }
} 