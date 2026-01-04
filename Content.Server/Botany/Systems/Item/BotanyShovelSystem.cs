
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Burial.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// System for using a shovel on a plant.
/// </summary>
public sealed class BotanyShovelSystem : EntitySystem
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

        SubscribeLocalEvent<ShovelComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantTrayComponent, TrayShovelAttemptEvent>(OnTrayShovelAttempt);
    }

    private void OnAfterInteract(Entity<ShovelComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach || !HasComp<PlantTrayComponent>(args.Target))
            return;

        var ev = new TrayShovelAttemptEvent(ent, args.User);
        RaiseLocalEvent(args.Target.Value, ref ev);

        args.Handled = true;
    }

    private void OnTrayShovelAttempt(Entity<PlantTrayComponent> ent, ref TrayShovelAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_plantTray.TryGetPlant(ent.AsNullable(), out var plantUid))
        {
            _popup.PopupCursor(
                Loc.GetString("plant-holder-component-no-plant-message", ("name", MetaData(ent.Owner).EntityName)),
                args.User);
            return;
        }

        _popup.PopupCursor(
            Loc.GetString("plant-holder-component-remove-plant-message", ("name", MetaData(ent.Owner).EntityName)),
            args.User,
            PopupType.Medium);
        _popup.PopupEntity(
            Loc.GetString("plant-holder-component-remove-plant-others-message",
                ("name", MetaData(args.User).EntityName)),
            ent.Owner,
            Filter.PvsExcept(args.User),
            true);
        _plant.RemovePlant(plantUid.Value);

    }
}

[ByRefEvent]
public sealed class TrayShovelAttemptEvent(Entity<ShovelComponent> shovel, EntityUid user) : CancellableEntityEventArgs
{
    public Entity<ShovelComponent> Shovel { get; } = shovel;
    public EntityUid User { get; } = user;
}
