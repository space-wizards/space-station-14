using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class AdminOOCEnabledChatCondition : ChatCondition
{

    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext channelParameters)
    {
        IoCManager.InjectDependencies(this);
        return _configurationManager.GetCVar(CCVars.AdminOocEnabled);
    }

    protected override bool Check(ICommonSession subjectSession, ChatMessageContext channelParameters)
    {
        IoCManager.InjectDependencies(this);
        return _configurationManager.GetCVar(CCVars.AdminOocEnabled);
    }
}
