using Content.Server.Atmos.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Content.Shared.Heretic;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Heretic.Components;

namespace Content.Server.Heretic.Abilities;
public sealed partial class BlazingDashSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlazingDashComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    public override void Update(float timeframe)
    {
        base.Update(timeframe);

        var dashEndQuery = EntityQueryEnumerator<BlazingDashComponent>();
        while (dashEndQuery.MoveNext(out var uid, out var dashComp))
        {
            RefreshBlazingDash(uid, dashComp);
        }
    }

    public void TryDoDash(Entity<HereticComponent> ent, ref EventHereticBlazingDash args)
    {
        var dashComp = EnsureComp<BlazingDashComponent>(ent);

        dashComp.BlazingDashActive = true;
        dashComp.BlazingDashEndTime = _timing.CurTime + dashComp.BlazingDashDuration;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
        _audio.PlayPvs(dashComp.DashSound, ent, AudioParams.Default
            .WithVolume(-2f)
            .WithMaxDistance(15f)
            .WithRolloffFactor(0.8f)
            );
        args.Handled = true;
    }

    private void RefreshBlazingDash(EntityUid uid, BlazingDashComponent dashComp)
    {
        if (dashComp.BlazingDashActive && _timing.CurTime >= dashComp.BlazingDashEndTime)
        {
            dashComp.BlazingDashActive = false;
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        }
        else if (dashComp.BlazingDashActive)
        {
            //refreshes over and over, so the firestacks are repeatedly transferred to people you touch
            //performs fine on my localhost but it might get laggy on a server with multiple heretics
            if (TryComp<FlammableComponent>(uid, out var flam))
            {
                _flammable.SetFireStacks(uid, dashComp.DashFireStacks, flam, true);
            }
        }
    }

    private void OnRefreshSpeed(Entity<BlazingDashComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.BlazingDashActive)
        {
            args.ModifySpeed(ent.Comp.DashWalkSpeed, ent.Comp.DashRunSpeed);
        }
        else
        {
            args.ModifySpeed(1f, 1f);
        }
    }
}
