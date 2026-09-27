using Content.Shared.Botany.Components;
using Content.Shared.Interaction;

namespace Content.Shared.Botany.Systems;

public sealed partial class PlantHolderSystem
{
    [Dependency] private PlantSystem _plantSystem = default!;

    private void InitializeRelay()
    {
        SubscribeLocalEvent<PlantHolderComponent, InteractUsingEvent>(RelayPlantHolderEvent);
    }

    private void RefRelayPlantHolderEvent<T>(EntityUid uid, PlantHolderComponent component, ref T args) where T : struct
    {
        RelayEvent((uid, component), ref args);
    }

    private void RelayPlantHolderEvent<T>(EntityUid uid, PlantHolderComponent component, T args) where T : class
    {
        RelayEvent((uid, component), args);
    }

    /// <summary>
    /// Relays the given event to the tray holding the plant.
    /// </summary>
    /// <param name="ent">The plant which is receiving the event</param>
    /// <param name="args">The event to relay</param>
    /// <typeparam name="T">The type of the event</typeparam>
    public void RelayEvent<T>(Entity<PlantHolderComponent> ent, ref T args) where T : struct
    {
        if (!_plantSystem.TryGetTray(ent.Owner, out var tray))
            return;

        var ev = new PlantHolderRelayedEvent<T>(args, ent);
        RaiseLocalEvent(tray, ref ev);
        args = ev.Args;
    }

    /// <summary>
    /// Relays the given event to the tray holding the plant.
    /// </summary>
    /// <param name="ent">The plant which is receiving the event</param>
    /// <param name="args">The event to relay</param>
    /// <typeparam name="T">The type of the event</typeparam>
    public void RelayEvent<T>(Entity<PlantHolderComponent> ent, T args) where T : class
    {
        if (!_plantSystem.TryGetTray(ent.Owner, out var tray))
            return;

        var ev = new PlantHolderRelayedEvent<T>(args, ent);
        RaiseLocalEvent(tray, ref ev);
    }
}

/// <summary>
/// Event wrapper for events being relayed to plant trays holding a plant.
/// </summary>
[ByRefEvent]
public record struct PlantHolderRelayedEvent<TEvent>(TEvent Args, EntityUid AppliedTo);
