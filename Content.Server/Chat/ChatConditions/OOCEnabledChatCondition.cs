using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Shared.Configuration;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class OOCEnabledChatCondition : ChatCondition
{
    public override Type? ConsumerType { get; set; } = null;

    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public override HashSet<T> FilterConsumers<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        return _configurationManager.GetCVar<bool>(CCVars.OocEnabled) ? consumers : new HashSet<T>();
    }
}
