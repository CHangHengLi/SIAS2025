using Prism.Events;

namespace _2025毕业设计.Event
{
    // 创建一个新事件，用于通知奖项已被删除
    public class AwardDeletedEvent : PubSubEvent<int> // 参数是被删除奖项的ID
    {
    }
} 