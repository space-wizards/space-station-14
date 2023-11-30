using Content.Server.Body.Systems;
using Content.Shared.Alert;
using JetBrains.Annotations;

namespace Content.Server.Alert.Click;

/// <summary>
/// Attempts to toggle the internals for a particular entity
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class ToggleInternals : IAlertClick
{
    public void AlertClicked(EntityUid player)
    {
        var internalsSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InternalsSystem>();
        internalsSystem.ToggleInternals(player, player, false);
    }
}
