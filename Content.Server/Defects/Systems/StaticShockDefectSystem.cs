using Content.Server.Electrocution;
using Content.Shared.Defects.Components;
using Content.Shared.Interaction;
using Robust.Shared.Random;

namespace Content.Server.Defects.Systems;

/// <summary>
/// Zaps the user when they successfully use the item on a target.
/// Insulation is handled automatically by the electrocution system.
/// Combat gloves, Captain's gloves, and similar insulated items will block the zap.
/// </summary>
public sealed class StaticShockDefectSystem : EntitySystem
{
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaticShockDefectComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<StaticShockDefectComponent> ent, ref AfterInteractEvent args)
    {
        // Only fire when used on an actual target.
        if (args.Target == null)
            return;

        // Do nothing if we don't roll above our probability
        if (!_random.Prob(ent.Comp.ShockChance))
            return;

        // Zap
        _electrocution.TryDoElectrocution(
            args.User,
            ent.Owner,
            ent.Comp.ShockDamage,
            ent.Comp.StunTime,
            refresh: true);
    }
}
