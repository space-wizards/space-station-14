using Content.Shared.Clothing.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Clothing;

/// <summary>
/// This handles <see cref="CursedMaskComponent"/>
/// </summary>
public abstract class SharedCursedMaskSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CursedMaskComponent, ClothingGotEquippedEvent>(OnClothingEquip);
        SubscribeLocalEvent<CursedMaskComponent, ClothingGotUnequippedEvent>(OnClothingUnequip);
    }

    private void OnClothingEquip(Entity<CursedMaskComponent> ent, ref ClothingGotEquippedEvent args)
    {
        RandomizeCursedMask(ent);
        TryTakeover(ent, args.Wearer);
    }

    protected virtual void OnClothingUnequip(Entity<CursedMaskComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RandomizeCursedMask(ent);
    }

    private void RandomizeCursedMask(Entity<CursedMaskComponent> ent)
    {
        var random = new System.Random((int) _timing.CurTick.Value);
        ent.Comp.CurrentState = random.Next(0, ent.Comp.CurseStates);
        _appearance.SetData(ent, CursedMaskVisuals.State, ent.Comp.CurrentState);
    }

    protected virtual void TryTakeover(Entity<CursedMaskComponent> ent, EntityUid wearer)
    {

    }
}
