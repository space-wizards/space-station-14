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
        var entManager = IoCManager.Resolve<IEntityManager>();

        if (!entManager.TryGetComponent<InternalsComponent>(player, out var internals)) return;

        var popups = entManager.EntitySysManager.GetEntitySystem<PopupSystem>();
        var internalsSystem = entManager.EntitySysManager.GetEntitySystem<InternalsSystem>();

        // Toggle off if they're on
        if (internalsSystem.AreInternalsWorking(internals))
        {
            internalsSystem.DisconnectTank(internals);
            return;
        }

        // If they're not on then check if we have a mask to use
        if (internals.BreathToolEntity == null)
        {
            popups.PopupEntity(Loc.GetString("internals-no-breath-tool"), player, Filter.Entities(player));
            return;
        }

        var tank = internalsSystem.FindBestGasTank(internals);

        if (tank == null)
        {
            popups.PopupEntity(Loc.GetString("internals-no-tank"), player, Filter.Entities(player));
            return;
        }

        entManager.EntitySysManager.GetEntitySystem<GasTankSystem>().ConnectToInternals(tank);
    }
}
