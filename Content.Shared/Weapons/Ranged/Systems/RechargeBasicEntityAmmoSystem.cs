using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class RechargeBasicEntityAmmoSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RechargeBasicEntityAmmoComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<RechargeBasicEntityAmmoComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<RechargeBasicEntityAmmoComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUnpaused(EntityUid uid, RechargeBasicEntityAmmoComponent component, ref EntityUnpausedEvent args)
    {
        if (component.NextCharge == null)
            return;

        component.NextCharge = component.NextCharge.Value + args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<RechargeBasicEntityAmmoComponent, BasicEntityAmmoProviderComponent>();

        while (query.MoveNext(out var uid, out var recharge, out var ammo))
        {
            if (ammo.Count is null || ammo.Count == ammo.Capacity || recharge.NextCharge == null)
                continue;

            if (recharge.NextCharge > _timing.CurTime)
                continue;

            if (_gun.UpdateBasicEntityAmmoCount(uid, ammo.Count.Value + 1, ammo))
            {
                // We don't predict this because occasionally on client it may not play.
                // PlayPredicted will still be predicted on the client.
                if (_netManager.IsServer)
                    _audio.PlayPvs(recharge.RechargeSound, uid);
            }

            if (ammo.Count == ammo.Capacity)
            {
                recharge.NextCharge = null;
                Dirty(recharge);
                continue;
            }

            recharge.NextCharge = recharge.NextCharge.Value + TimeSpan.FromSeconds(recharge.RechargeCooldown);
            Dirty(recharge);
        }
    }

    private void OnInit(EntityUid uid, RechargeBasicEntityAmmoComponent component, MapInitEvent args)
    {
        component.NextCharge = _timing.CurTime;
        Dirty(component);
    }

    private void OnExamined(EntityUid uid, RechargeBasicEntityAmmoComponent component, ExaminedEvent args)
    {
        if (!TryComp<BasicEntityAmmoProviderComponent>(uid, out var ammo)
            || ammo.Count == ammo.Capacity ||
            component.NextCharge == null)
        {
            args.PushMarkup(Loc.GetString("recharge-basic-entity-ammo-full"));
            return;
        }

        var timeLeft = component.NextCharge + _metadata.GetPauseTime(uid) - _timing.CurTime;
        args.PushMarkup(Loc.GetString("recharge-basic-entity-ammo-can-recharge", ("seconds", Math.Round(timeLeft.Value.TotalSeconds, 1))));
    }

    public void Reset(EntityUid uid, RechargeBasicEntityAmmoComponent? recharge = null)
    {
        if (!Resolve(uid, ref recharge, false))
            return;

        if (recharge.NextCharge == null || recharge.NextCharge < _timing.CurTime)
        {
            recharge.NextCharge = _timing.CurTime + TimeSpan.FromSeconds(recharge.RechargeCooldown);
            Dirty(recharge);
        }
    }
}
