
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// System for taking a sample of a plant.
/// </summary>
public sealed class BotanyHoeSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly PlantTraitsSystem _plantTraits = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BotanyHoeComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantTrayComponent, TrayHoeAttemptEvent>(OnTrayHoeAttempt);
    }

    private void OnAfterInteract(Entity<BotanyHoeComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach || !HasComp<PlantTrayComponent>(args.Target))
            return;

        var ev = new TrayHoeAttemptEvent(ent, args.User);
        RaiseLocalEvent(args.Target.Value, ref ev);

        args.Handled = true;
    }

    private void OnTrayHoeAttempt(Entity<PlantTrayComponent> ent, ref TrayHoeAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.WeedLevel <= 0)
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-no-weeds-message"), args.User);
            return;
        }

        _popup.PopupCursor(
            Loc.GetString("plant-holder-component-remove-weeds-message", ("name", MetaData(ent.Owner).EntityName)),
            args.User,
            PopupType.Medium);
        _popup.PopupEntity(
            Loc.GetString("plant-holder-component-remove-weeds-others-message",
                ("otherName", MetaData(args.User).EntityName)),
            ent.Owner,
            Filter.PvsExcept(args.User),
            true);

        _plantTray.AdjustWeed(ent.AsNullable(), -args.Hoe.Comp.WeedAmount);
    }
}

[ByRefEvent]
public sealed class TrayHoeAttemptEvent(Entity<BotanyHoeComponent> hoe, EntityUid user) : CancellableEntityEventArgs
{
    public Entity<BotanyHoeComponent> Hoe { get; } = hoe;
    public EntityUid User { get; } = user;
}
