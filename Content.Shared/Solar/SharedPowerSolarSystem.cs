using Content.Shared.Solar.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Solar;

public abstract class SharedPowerSolarSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolarPanelComponent, EntityUnpausedEvent>(OnUnpause);
        SubscribeLocalEvent<SolarPanelComponent, ComponentGetState>(GetSolarPanelState);
        SubscribeLocalEvent<SolarPanelComponent, ComponentHandleState>(HandleSolarPanelState);
    }

    private void OnUnpause(EntityUid uid, SolarPanelComponent component, ref EntityUnpausedEvent args)
    {
        component.LastUpdate += args.PausedTime;
        Dirty(component);
    }

    private void GetSolarPanelState(EntityUid uid, SolarPanelComponent component, ref ComponentGetState args)
    {
        args.State = new SolarPanelComponentState
        {
            Angle = component.StartAngle,
            AngularVelocity = component.AngularVelocity,
            LastUpdate = component.LastUpdate
        };
    }

    private void HandleSolarPanelState(EntityUid uid, SolarPanelComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SolarPanelComponentState state)
            return;

        component.StartAngle = state.Angle;
        component.AngularVelocity = state.AngularVelocity;
        component.LastUpdate = state.LastUpdate;
    }
}
