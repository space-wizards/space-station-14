using Content.Shared.Administration.Managers;
using Robust.Shared.Player;

namespace Content.Shared.Chat.ChatConditions;

/// <summary>
/// Checks if the consumers are admins.
/// </summary>
[DataDefinition]
public sealed partial class IsAdminChatCondition : SessionChatConditionBase
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;

    /// <summary>
    /// If true, deadmined sessions will be included.
    /// </summary>
    [DataField]
    public bool IncludeDeadmin;

    protected override bool Check(ICommonSession subjectSession, ChatMessageContext chatContext)
    {
        IoCManager.InjectDependencies(this);

        return _admin.IsAdmin(subjectSession, IncludeDeadmin);
    }
}
