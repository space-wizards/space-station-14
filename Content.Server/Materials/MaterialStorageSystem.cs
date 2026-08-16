using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.IdentityManagement;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Server.Power.Components;
using Content.Shared.Construction;
using Content.Shared.Database;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server.Materials;

/// <summary>
/// This handles <see cref="SharedMaterialStorageSystem"/>
/// </summary>
public sealed partial class MaterialStorageSystem : SharedMaterialStorageSystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
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
        _popup.PopupEntity(Loc.GetString("machine-insert-item",
                ("user", Identity.Entity(user, EntityManager)),
                ("machine", receiver),
                ("item", toInsert)),
            receiver);
        QueueDel(toInsert);

        // Logging
        TryComp<StackComponent>(toInsert, out var stack);
        var count = stack?.Count ?? 1;
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(user):player} inserted {count} {ToPrettyString(toInsert):inserted} into {ToPrettyString(receiver):receiver}");
        return true;
    }

    /// <summary>
    /// Eject a material out of this storage. The internal counts are updated.
    /// Material that cannot be ejected stays in storage. (e.g. only have 50 but a sheet needs 100).
    /// </summary>
    /// <param name="entity">The entity with storage to eject from.</param>
    /// <param name="material">The material prototype to eject.</param>
    /// <param name="maxAmount">The maximum amount to eject. If not given, as much as possible is ejected.</param>
    /// <param name="coordinates">The position where to spawn the created sheets. If not given, they're spawned next to the entity.</param>
    /// <param name="component">The storage component on <paramref name="entity"/>. Resolved automatically if not given.</param>
    /// <returns>The stack entities that were spawned.</returns>
    public List<EntityUid> EjectMaterial(
        EntityUid entity,
        string material,
        int? maxAmount = null,
        EntityCoordinates? coordinates = null,
        MaterialStorageComponent? component = null)
    {
        if (!Resolve(entity, ref component))
            return new List<EntityUid>();

        coordinates ??= Transform(entity).Coordinates;

        var amount = GetMaterialAmount(entity, material, component);
        if (maxAmount != null)
            amount = Math.Min(maxAmount.Value, amount);

        var spawned = SpawnMultipleFromMaterial(amount, material, coordinates.Value, out var overflow);

        TryChangeMaterialAmount(entity, material, -(amount - overflow), component);
        return spawned;
    }

    /// <summary>
    /// Eject all material stored in an entity, with the same mechanics as <see cref="EjectMaterial"/>.
    /// </summary>
    /// <param name="entity">The entity with storage to eject from.</param>
    /// <param name="coordinates">The position where to spawn the created sheets. If not given, they're spawned next to the entity.</param>
    /// <param name="component">The storage component on <paramref name="entity"/>. Resolved automatically if not given.</param>
    /// <returns>The stack entities that were spawned.</returns>
    public List<EntityUid> EjectAllMaterial(
        EntityUid entity,
        EntityCoordinates? coordinates = null,
        MaterialStorageComponent? component = null)
    {
        if (!Resolve(entity, ref component))
            return new List<EntityUid>();

        coordinates ??= Transform(entity).Coordinates;

        var allSpawned = new List<EntityUid>();
        foreach (var material in component.Storage.Keys.ToArray())
        {
            var spawned = EjectMaterial(entity, material, null, coordinates, component);
            allSpawned.AddRange(spawned);
        }

        return allSpawned;
    }
}
