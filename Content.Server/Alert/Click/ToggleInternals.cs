using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Server.Shuttles.Systems;
using Content.Shared.Alert;
using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Alert.Click;

/// <summary>
/// Attempts to toggle the internals for a particular entity
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed class ToggleInternals : IAlertClick
{
    public void AlertClicked(EntityUid player)
    {
        var internalsSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InternalsSystem>();
        internalsSystem.ToggleInternals(player, player, false);
    }
}
