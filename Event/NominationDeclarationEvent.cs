using SIASGraduate.Models;

namespace SIASGraduate.Event
{
    /// <summary>
    /// 申报提名添加事件
    /// </summary>
    public class NominationDeclarationAddEvent : PubSubEvent
    {
    }

    /// <summary>
    /// 申报提名更新事件
    /// </summary>
    public class NominationDeclarationUpdateEvent : PubSubEvent
    {
    }

    /// <summary>
    /// 申报提名删除事件
    /// </summary>
    public class NominationDeclarationDeleteEvent : PubSubEvent<int>
    {
    }

    /// <summary>
    /// 申报提名审核通过事件
    /// </summary>
    public class NominationDeclarationApproveEvent : PubSubEvent<NominationDeclaration>
    {
    }

    /// <summary>
    /// 申报提名审核拒绝事件
    /// </summary>
    public class NominationDeclarationRejectEvent : PubSubEvent<NominationDeclaration>
    {
    }

    /// <summary>
    /// 申报提名转为正式提名事件
    /// </summary>
    public class NominationDeclarationPromoteEvent : PubSubEvent<NominationDeclaration>
    {
    }
}