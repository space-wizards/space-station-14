using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class RechargeBasicEntityAmmoSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RechargeBasicEntityAmmoComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<RechargeBasicEntityAmmoComponent, ExaminedEvent>(OnExamined);
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

            if (_gun.UpdateBasicEntityAmmoCount((uid, ammo), ammo.Count.Value + 1))
            {
                // We don't predict this because occasionally on client it may not play.
                // PlayPredicted will still be predicted on the client.
                if (_netManager.IsServer)
                    _audio.PlayPvs(recharge.RechargeSound, uid);
            }

            if (ammo.Count == ammo.Capacity)
            {
                recharge.NextCharge = null;
                Dirty(uid, recharge);
                continue;
            }

            recharge.NextCharge = recharge.NextCharge.Value + TimeSpan.FromSeconds(recharge.RechargeCooldown);
            Dirty(uid, recharge);
        }
    }

    private void OnInit(Entity<RechargeBasicEntityAmmoComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextCharge = _timing.CurTime;
        Dirty(ent);
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

        var timeLeft = ent.Comp.NextCharge + _metadata.GetPauseTime(ent) - _timing.CurTime;
        args.PushMarkup(Loc.GetString("recharge-basic-entity-ammo-can-recharge", ("seconds", Math.Round(timeLeft.Value.TotalSeconds, 1))));
    }

    public void Reset(Entity<RechargeBasicEntityAmmoComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.NextCharge == null || ent.Comp.NextCharge < _timing.CurTime)
        {
            ent.Comp.NextCharge = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.RechargeCooldown);
            Dirty(ent);
        }
    }
}
