using Content.Server.Weapons.Ranged.Components;
using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed class RechargeBasicEntityAmmoSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RechargeBasicEntityAmmoComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RechargeBasicEntityAmmoComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (recharge, ammo) in
                 EntityQuery<RechargeBasicEntityAmmoComponent, BasicEntityAmmoProviderComponent>())
        {
            if (ammo.Count is null || ammo.Count == ammo.Capacity)
                continue;

            recharge.AccumulatedFrameTime += frameTime;

            if (recharge.AccumulatedFrameTime < recharge.NextRechargeTime)
                continue;

            recharge.AccumulatedFrameTime -= recharge.NextRechargeTime;
            UpdateCooldown(recharge);


            if (_gun.UpdateBasicEntityAmmoCount(ammo.Owner, ammo.Count.Value + 1, ammo))
            {
                SoundSystem.Play(recharge.RechargeSound.GetSound(), Filter.Pvs(recharge.Owner), recharge.Owner,
                    recharge.RechargeSound.Params);
            }
        }
    }

    private void OnInit(EntityUid uid, RechargeBasicEntityAmmoComponent component, ComponentInit args)
    {
        UpdateCooldown(component);
    }

    private void OnExamined(EntityUid uid, RechargeBasicEntityAmmoComponent component, ExaminedEvent args)
    {
        if (!TryComp<BasicEntityAmmoProviderComponent>(uid, out var ammo)
            || ammo.Count == ammo.Capacity)
        {
            args.PushMarkup(Loc.GetString("recharge-basic-entity-ammo-full"));
            return;
        }

        var timeLeft = component.NextRechargeTime - component.AccumulatedFrameTime;
        args.PushMarkup(Loc.GetString("recharge-basic-entity-ammo-can-recharge", ("seconds", Math.Round(timeLeft, 1))));
    }

    private void UpdateCooldown(RechargeBasicEntityAmmoComponent component)
    {
        component.NextRechargeTime = _random.NextFloat(component.MinRechargeCooldown, component.MaxRechargeCooldown);
    }
}
