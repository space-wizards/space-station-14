using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Labels.Components;
using Robust.Server.GameObjects;
using Content.Shared.Database;
using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Botany.Systems;

/// <summary>
/// System for taking a sample of a plant.
/// </summary>
public sealed class BotanySeedSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantTrayComponent, PlantingSeedAttemptEvent>(OnPlantingSeedAttempt);
    }

    private void OnAfterInteract(Entity<SeedComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !HasComp<PlantTrayComponent>(args.Target))
            return;

        var ev = new PlantingSeedAttemptEvent(ent, args.User);
        RaiseLocalEvent(args.Target.Value, ref ev);

        args.Handled = true;
    }

    private void OnPlantingSeedAttempt(Entity<PlantTrayComponent> ent, ref PlantingSeedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_plantTray.TryGetPlant(ent.AsNullable(), out _))
        {
            _popup.PopupCursor(
                Loc.GetString("plant-holder-component-already-seeded-message", ("name", MetaData(ent.Owner).EntityName)),
                args.User,
                PopupType.Medium);
            return;
        }

        var plantUid = Spawn(args.Seed.Comp.PlantProtoId, _transform.GetMapCoordinates(ent.Owner), args.Seed.Comp.PlantData);
        if (!TryComp<PlantDataComponent>(plantUid, out var plantData))
            return;

        var name = Loc.GetString(plantData.DisplayName);
        var noun = Loc.GetString(plantData.Noun);
        _popup.PopupCursor(Loc.GetString("plant-holder-component-plant-success-message",
                ("seedName", name),
                ("seedNoun", noun)),
            args.User,
            PopupType.Medium);

        if (TryComp<PaperLabelComponent>(args.Seed, out var paperLabel))
            _itemSlots.TryEjectToHands(args.Seed, paperLabel.LabelSlot, args.User);

        _plantTray.PlantingPlantInTray(ent.Owner, plantUid, args.Seed.Comp.HealthOverride);
        QueueDel(args.Seed);

        if (plantData.PlantLogImpact != null)
        {
            _adminLogger.Add(LogType.Botany,
                plantData.PlantLogImpact.Value,
                $"{ToPrettyString(args.User):player} planted {Loc.GetString(plantData.DisplayName):seed} at Pos:{Transform(ent.Owner).Coordinates}.");
        }
    }
}

[ByRefEvent]
public sealed class PlantingSeedAttemptEvent(Entity<SeedComponent> seed, EntityUid user) : CancellableEntityEventArgs
{
    public Entity<SeedComponent> Seed { get; } = seed;
    public EntityUid User { get; } = user;
}
