using Content.Server.Administration.Logs;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Server.Power.Components;
using Content.Server.Construction.Components;
using Content.Server.Stack;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Materials;

/// <summary>
/// This handles <see cref="SharedMaterialStorageSystem"/>
/// </summary>
public sealed class MaterialStorageSystem : SharedMaterialStorageSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MaterialStorageComponent, MachineDeconstructedEvent>(OnDeconstructed);
    }

    private void OnDeconstructed(EntityUid uid, MaterialStorageComponent component, MachineDeconstructedEvent args)
    {
        if (!component.DropOnDeconstruct)
            return;

        foreach (var (material, amount) in component.Storage)
        {
            SpawnMultipleFromMaterial(amount, material, Transform(uid).Coordinates);
        }
    }

    public override bool TryInsertMaterialEntity(EntityUid user,
        EntityUid toInsert,
        EntityUid receiver,
        MaterialStorageComponent? storage = null,
        MaterialComponent? material = null,
        PhysicalCompositionComponent? composition = null)
    {
        if (!Resolve(receiver, ref storage) || !Resolve(toInsert, ref material, ref composition, false))
            return false;
        if (TryComp<ApcPowerReceiverComponent>(receiver, out var power) && !power.Powered)
            return false;
        if (!base.TryInsertMaterialEntity(user, toInsert, receiver, storage, material, composition))
            return false;
        _audio.PlayPvs(storage.InsertingSound, receiver);
        _popup.PopupEntity(Loc.GetString("machine-insert-item", ("user", user), ("machine", receiver),
            ("item", toInsert)), receiver);
        QueueDel(toInsert);

        // Logging
        TryComp<StackComponent>(toInsert, out var stack);
        var count = stack?.Count ?? 1;
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):player} inserted {count} {ToPrettyString(toInsert):inserted} into {ToPrettyString(receiver):receiver}");
        return true;
    }

    /// <summary>
    ///     Spawn an amount of a material in stack entities.
    ///     Note the 'amount' is material dependent.
    ///     1 biomass = 1 biomass in its stack,
    ///     but 100 plasma = 1 sheet of plasma, etc.
    /// </summary>
    public List<EntityUid> SpawnMultipleFromMaterial(int amount, string material, EntityCoordinates coordinates)
    {
        return SpawnMultipleFromMaterial(amount, material, coordinates, out _);
    }

    /// <summary>
    ///     Spawn an amount of a material in stack entities.
    ///     Note the 'amount' is material dependent.
    ///     1 biomass = 1 biomass in its stack,
    ///     but 100 plasma = 1 sheet of plasma, etc.
    /// </summary>
    public List<EntityUid> SpawnMultipleFromMaterial(int amount, string material, EntityCoordinates coordinates, out int overflowMaterial)
    {
        overflowMaterial = 0;
        if (!_prototypeManager.TryIndex<MaterialPrototype>(material, out var stackType))
        {
            Logger.Error("Failed to index material prototype " + material);
            return new List<EntityUid>();
        }

        return SpawnMultipleFromMaterial(amount, stackType, coordinates, out overflowMaterial);
    }

    /// <summary>
    ///     Spawn an amount of a material in stack entities.
    ///     Note the 'amount' is material dependent.
    ///     1 biomass = 1 biomass in its stack,
    ///     but 100 plasma = 1 sheet of plasma, etc.
    /// </summary>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleFromMaterial(int amount, MaterialPrototype materialProto, EntityCoordinates coordinates)
    {
        return SpawnMultipleFromMaterial(amount, materialProto, coordinates, out _);
    }

    /// <summary>
    ///     Spawn an amount of a material in stack entities.
    ///     Note the 'amount' is material dependent.
    ///     1 biomass = 1 biomass in its stack,
    ///     but 100 plasma = 1 sheet of plasma, etc.
    /// </summary>
    public List<EntityUid> SpawnMultipleFromMaterial(int amount, MaterialPrototype materialProto, EntityCoordinates coordinates, out int overflowMaterial)
    {
        overflowMaterial = 0;

        if (amount <= 0)
            return new List<EntityUid>();

        var entProto = _prototypeManager.Index<EntityPrototype>(materialProto.StackEntity);
        if (!entProto.TryGetComponent<PhysicalCompositionComponent>(out var composition))
            return new List<EntityUid>();

        var materialPerStack = composition.MaterialComposition[materialProto.ID];
        var amountToSpawn = amount / materialPerStack;
        overflowMaterial = amount - amountToSpawn * materialPerStack;
        return _stackSystem.SpawnMultiple(materialProto.StackEntity, amountToSpawn, coordinates);
    }
}
