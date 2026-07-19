using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class RechargeBasicEntityAmmoSystem : EntitySystem
{
    private static readonly EntityTimerId RechargeTimer = new("recharge");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RechargeBasicEntityAmmoComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<RechargeBasicEntityAmmoComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<RechargeBasicEntityAmmoComponent, EntityTimerEvent>(OnTimer);
        SubscribeLocalEvent<RechargeBasicEntityAmmoComponent, ExaminedEvent>(OnExamined);
    }

    private void OnTimer(Entity<RechargeBasicEntityAmmoComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != RechargeTimer ||
            !TryComp<BasicEntityAmmoProviderComponent>(ent, out var ammo) ||
            ammo.Count is null ||
            ammo.Count == ammo.Capacity ||
            ent.Comp.NextCharge is null)
            return;

        if (_gun.UpdateBasicEntityAmmoCount((ent, ammo), ammo.Count.Value + 1))
        {
            if (_netManager.IsServer)
                _audio.PlayPvs(ent.Comp.RechargeSound, ent);
        }

        if (ammo.Count == ammo.Capacity)
        {
            ent.Comp.NextCharge = null;
            Dirty(ent);
            return;
        }

        ent.Comp.NextCharge += TimeSpan.FromSeconds(ent.Comp.RechargeCooldown);
        Dirty(ent);
        Schedule(ent);
    }

    private void OnInit(Entity<RechargeBasicEntityAmmoComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextCharge = _timing.CurTime;
        Dirty(ent);
        Schedule(ent);
    }

    private void OnHandleState(Entity<RechargeBasicEntityAmmoComponent> ent, ref ComponentHandleState args)
    {
        Schedule(ent);
    }

    private void OnExamined(Entity<RechargeBasicEntityAmmoComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.ShowExamineText)
            return;

        if (!TryComp<BasicEntityAmmoProviderComponent>(ent, out var ammo)
            || ammo.Count == ammo.Capacity ||
            ent.Comp.NextCharge == null)
        {
            args.PushMarkup(Loc.GetString("recharge-basic-entity-ammo-full"));
            return;
        }

        if (!_timers.TryGetTimer<RechargeBasicEntityAmmoComponent>(ent.Owner, RechargeTimer, out var timer))
            return;

        args.PushMarkup(Loc.GetString("recharge-basic-entity-ammo-can-recharge", ("seconds", Math.Round(timer.Remaining.TotalSeconds, 1))));
    }

    public void Reset(Entity<RechargeBasicEntityAmmoComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.NextCharge == null || ent.Comp.NextCharge < _timing.CurTime)
        {
            ent.Comp.NextCharge = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.RechargeCooldown);
            Dirty(ent);
            Schedule((ent.Owner, ent.Comp));
        }
    }

    private void Schedule(Entity<RechargeBasicEntityAmmoComponent> ent)
    {
        if (ent.Comp.NextCharge is {} deadline)
            _timers.SetTimerAt(ent, RechargeTimer, deadline);
        else
            _timers.CancelTimer<RechargeBasicEntityAmmoComponent>(ent, RechargeTimer);
    }
}
