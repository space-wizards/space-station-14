using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Clothing;

/// <summary>
/// This handles <see cref="CursedMaskComponent"/>
/// </summary>
public abstract class SharedCursedMaskSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CursedMaskComponent, ClothingGotEquippedEvent>(OnClothingEquip);
        SubscribeLocalEvent<CursedMaskComponent, ClothingGotUnequippedEvent>(OnClothingUnequip);
        SubscribeLocalEvent<CursedMaskComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<CursedMaskComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnMovementSpeedModifier);
        SubscribeLocalEvent<CursedMaskComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnModifyDamage);
    }

    private void OnClothingEquip(Entity<CursedMaskComponent> ent, ref ClothingGotEquippedEvent args)
    {
        RandomizeCursedMask(ent, args.Wearer);
        TryTakeover(ent, args.Wearer);
    }

    protected virtual void OnClothingUnequip(Entity<CursedMaskComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RandomizeCursedMask(ent, args.Wearer);
    }

    private void OnExamine(Entity<CursedMaskComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString($"cursed-mask-examine-{ent.Comp.CurrentState.ToString()}"));
    }

    private void OnMovementSpeedModifier(Entity<CursedMaskComponent> ent, ref InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (ent.Comp.CurrentState == CursedMaskExpression.Joy)
            args.Args.ModifySpeed(ent.Comp.JoySpeedModifier);
    }

    private void OnModifyDamage(Entity<CursedMaskComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (ent.Comp.CurrentState == CursedMaskExpression.Despair)
            args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, ent.Comp.DespairDamageModifier);
    }

    protected void RandomizeCursedMask(Entity<CursedMaskComponent> ent, EntityUid wearer)
    {
        var random = new System.Random((int) _timing.CurTick.Value);
        ent.Comp.CurrentState = random.Pick(Enum.GetValues<CursedMaskExpression>());
        _appearance.SetData(ent, CursedMaskVisuals.State, ent.Comp.CurrentState);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(wearer);
    }

    protected virtual void TryTakeover(Entity<CursedMaskComponent> ent, EntityUid wearer)
    {

    }
}
