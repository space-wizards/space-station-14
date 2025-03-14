using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class AdminOOCEnabledChatCondition : SessionChatConditionBase
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    protected override bool Check(ICommonSession subjectSession, ChatMessageContext chatContext)
    {
        IoCManager.InjectDependencies(this);
        return _configurationManager.GetCVar(CCVars.AdminOocEnabled);
    }
}
