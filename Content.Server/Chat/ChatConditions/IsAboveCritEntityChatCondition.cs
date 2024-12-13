using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Radio.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.ChatConditions;

/// <summary>
/// Checks if the consumer is alive and above crit; does not check for consciousness e.g. sleeping.
/// </summary>
[DataDefinition]
public sealed partial class IsAboveCritEntityChatCondition : EntityChatCondition
{

    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

    public override HashSet<EntityUid> FilterConsumers(HashSet<EntityUid> consumers, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        if (_entitySystem.TryGetEntitySystem<MobStateSystem>(out var mobStateSystem))
        {
            return consumers.Where(x => !mobStateSystem.IsIncapacitated(x)).ToHashSet();
        }

        return new HashSet<EntityUid>();
    }
}
