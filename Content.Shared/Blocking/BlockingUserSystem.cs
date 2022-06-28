using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Blocking;

public sealed class BlockingUserSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockingUserComponent, DamageChangedEvent>(OnDamgageChanged);
        SubscribeLocalEvent<BlockingUserComponent, DamageModifyEvent>(OnUserDamageModified);
    }

    private void OnDamgageChanged(EntityUid uid, BlockingUserComponent component, DamageChangedEvent args)
    {
        var items = _handsSystem.EnumerateHeld(uid);

        foreach (var item in items)
        {
            if (HasComp<BlockingComponent>(item))
                RaiseLocalEvent(item, args);
        }
    }

    private void OnUserDamageModified(EntityUid uid, BlockingUserComponent component, DamageModifyEvent args)
    {
        if (TryComp<BlockingComponent>(component.BlockingItem, out var blockingComponent))
        {
            if (_proto.TryIndex(blockingComponent.PassiveBlockDamageModifer, out DamageModifierSetPrototype? passiveblockModifier) && !blockingComponent.IsBlocking)
                args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, passiveblockModifier);

            if (_proto.TryIndex(blockingComponent.ActiveBlockDamageModifier, out DamageModifierSetPrototype? activeBlockModifier) && blockingComponent.IsBlocking)
            {
                args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, activeBlockModifier);
                SoundSystem.Play(blockingComponent.BlockSound.GetSound(), Filter.Pvs(component.Owner, entityManager: EntityManager), component.Owner, AudioHelpers.WithVariation(0.2f));
            }
        }
    }
}
