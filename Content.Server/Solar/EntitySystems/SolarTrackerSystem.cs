using Content.Server.Power.EntitySystems;
using Content.Server.Solar.Components;
using Content.Shared.Power;

namespace Content.Server.Solar.EntitySystems;

public sealed class SolarTrackerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PowerReceiverSystem _receiver = default!;

    private readonly float _trackerRange = 50; // Yes, this is an arbitrary, magic number.

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolarTrackerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SolarTrackerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnPowerChanged(Entity<SolarTrackerComponent> tracker, ref PowerChangedEvent args)
    {
        var coordinates = Transform(tracker).Coordinates;
        var panels = _lookup.GetEntitiesInRange<SolarPanelComponent>(coordinates, _trackerRange);

        foreach (var panel in panels)
        {
            panel.Comp.AssistedByTracker = args.Powered;
        }

        tracker.Comp.LastCoordinates = args.Powered ? coordinates : null;
    }

    private void OnShutdown(Entity<SolarTrackerComponent> tracker, ref ComponentShutdown args)
    {
        if (tracker.Comp.LastCoordinates is null)
            return;

        var panels = _lookup.GetEntitiesInRange<SolarPanelComponent>(tracker.Comp.LastCoordinates.Value, _trackerRange);

        foreach (var panel in panels)
        {
            panel.Comp.AssistedByTracker = false;
        }
    }

    public void CheckForTrackers(Entity<SolarPanelComponent> panel)
    {
        var coordinates = Transform(panel).Coordinates;
        var trackers = _lookup.GetEntitiesInRange<SolarTrackerComponent>(coordinates, _trackerRange);

        foreach (var tracker in trackers)
        {
            if (!_receiver.IsPowered(tracker))
                continue;

            panel.Comp.AssistedByTracker = true;
            return;
        }
    }
}
