using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class OOCEnabledSessionChatCondition : SessionChatCondition
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public override HashSet<ICommonSession> FilterConsumers(HashSet<ICommonSession> consumers, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        return _configurationManager.GetCVar<bool>(CCVars.OocEnabled) ? consumers : new HashSet<ICommonSession>();
    }
}
