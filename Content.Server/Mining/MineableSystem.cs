using Content.Server.DoAfter;
using Content.Server.Mining.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Server.Mining;

public class MineableSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MineableComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private async void OnInteractUsing(EntityUid uid, MineableComponent component, InteractUsingEvent args)
    {
        if (!TryComp<PickaxeComponent>(args.Used, out var pickaxe))
            return;

        // Can't mine too many entities at once.
        if (pickaxe.MaxMiningEntities < pickaxe.MiningEntities.Count + 1)
            return;

        // Can't mine one object multiple times.
        if (pickaxe.MiningEntities.Contains(uid))
            return;

        var doAfter = new DoAfterEventArgs(args.User, component.BaseMineTime * pickaxe.MiningTimeMultiplier, default, uid)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f
        };

        pickaxe.MiningEntities.Add(uid);

        if (await _doAfterSystem.WaitDoAfter(doAfter) == DoAfterStatus.Finished)
        {
            _damageableSystem.TryChangeDamage(uid, pickaxe.Damage);
            SoundSystem.Play(Filter.Pvs(uid), pickaxe.MiningSound.GetSound(), AudioParams.Default);
        }

        pickaxe.MiningEntities.Remove(uid);
    }
}
