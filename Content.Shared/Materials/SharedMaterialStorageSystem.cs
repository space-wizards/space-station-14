using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Research.Components;

namespace Content.Shared.Materials;

/// <summary>
/// This handles storing materials and modifying their amounts
/// <see cref="MaterialStorageComponent"/>
/// </summary>
public abstract class SharedMaterialStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    /// <summary>
    /// Default volume for a sheet if the material's entity prototype has no material composition.
    /// </summary>
    private const int DefaultSheetVolume = 100;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialStorageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MaterialStorageComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MaterialStorageComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<InsertingMaterialStorageComponent>();
        while (query.MoveNext(out var uid, out var inserting))
        {
            if (_timing.CurTime < inserting.EndTime)
                continue;

            _appearance.SetData(uid, MaterialStorageVisuals.Inserting, false);
            RemComp(uid, inserting);
        }
    }

    private void OnMapInit(EntityUid uid, MaterialStorageComponent component, MapInitEvent args)
    {
        _appearance.SetData(uid, MaterialStorageVisuals.Inserting, false);
    }

    /// <summary>
    /// Gets all the materials stored on this entity
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="localOnly">Include only materials held "locally", as determined by event subscribers</param>
    /// <returns></returns>
    public Dictionary<ProtoId<MaterialPrototype>, int> GetStoredMaterials(Entity<MaterialStorageComponent?> ent, bool localOnly = false)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return new();

        // clone so we don't modify by accident.
        var mats = new Dictionary<ProtoId<MaterialPrototype>, int>(ent.Comp.Storage);
        var ev = new GetStoredMaterialsEvent((ent, ent.Comp), mats, localOnly);
        RaiseLocalEvent(ent, ref ev, true);

        return ev.Materials;
    }

    /// <summary>
    /// Gets the volume of a specified material contained in this storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="material"></param>
    /// <param name="component"></param>
    /// <param name="localOnly"></param>
    /// <returns>The volume of the material</returns>
    [PublicAPI]
    public int GetMaterialAmount(EntityUid uid, MaterialPrototype material, MaterialStorageComponent? component = null, bool localOnly = false)
    {
        return GetMaterialAmount(uid, material.ID, component, localOnly);
    }

    /// <summary>
    /// Gets the volume of a specified material contained in this storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="material"></param>
    /// <param name="component"></param>
    /// <param name="localOnly"></param>
    /// <returns>The volume of the material</returns>
    public int GetMaterialAmount(EntityUid uid, string material, MaterialStorageComponent? component = null, bool localOnly = false)
    {
        if (!Resolve(uid, ref component))
            return 0; //you have nothing
        return GetStoredMaterials((uid, component), localOnly).GetValueOrDefault(material, 0);
    }

    /// <summary>
    /// Gets the total volume of all materials in the storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="localOnly"></param>
    /// <returns>The volume of all materials in the storage</returns>
    public int GetTotalMaterialAmount(EntityUid uid, MaterialStorageComponent? component = null, bool localOnly = false)
    {
        if (!Resolve(uid, ref component))
            return 0;
        return GetStoredMaterials((uid, component), localOnly).Values.Sum();
    }

    // TODO: Revisit this if we ever decide to do things with storage limits. As it stands, the feature is unused.
    /// <summary>
    /// Tests if a specific amount of volume will fit in the storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="volume"></param>
    /// <param name="component"></param>
    /// <param name="localOnly"></param>
    /// <returns>If the specified volume will fit</returns>
    public bool CanTakeVolume(EntityUid uid, int volume, MaterialStorageComponent? component = null, bool localOnly = false)
    {
        if (!Resolve(uid, ref component))
            return false;
        return component.StorageLimit == null || GetTotalMaterialAmount(uid, component, true) + volume <= component.StorageLimit;
    }

    /// <summary>
    /// Checks if a certain material prototype is supported by this entity.
    /// </summary>
    public bool IsMaterialWhitelisted(Entity<MaterialStorageComponent?> ent, ProtoId<MaterialPrototype> material)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.MaterialWhiteList == null)
            return true;

        return ent.Comp.MaterialWhiteList.Contains(material);
    }

    /// <summary>
    /// Checks if the specified material can be changed by the specified volume.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="materialId"></param>
    /// <param name="volume"></param>
    /// <param name="component"></param>
    /// <param name="localOnly"></param>
    /// <returns>If the amount can be changed</returns>
    public bool CanChangeMaterialAmount(EntityUid uid, string materialId, int volume, MaterialStorageComponent? component = null, bool localOnly = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanTakeVolume(uid, volume, component))
            return false;

        if (!IsMaterialWhitelisted((uid, component), materialId))
            return false;

        var amount = GetMaterialAmount(uid, materialId, component, localOnly);
        return amount + volume >= 0;
    }

    /// <summary>
    /// Checks if the specified materials can be changed by the specified volumes.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="materials"></param>
    /// <returns>If the amount can be changed</returns>
    /// <param name="localOnly"></param>
    public bool CanChangeMaterialAmount(Entity<MaterialStorageComponent?> entity, Dictionary<string,int> materials, bool localOnly = false)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var inVolume = materials.Values.Sum();
        var stored = GetStoredMaterials((entity, entity.Comp), localOnly);

        if (!CanTakeVolume(entity, inVolume, entity.Comp))
            return false;

        foreach (var (material, amount) in materials)
        {
            if (!IsMaterialWhitelisted(entity, material))
                return false;

            if (stored.GetValueOrDefault(material) + amount < 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Changes the amount of a specific material in the storage.
    /// Still respects the filters in place.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="materialId"></param>
    /// <param name="volume"></param>
    /// <param name="component"></param>
    /// <param name="dirty"></param>
    /// <param name="localOnly"></param>
    /// <returns>If it was successful</returns>
    public bool TryChangeMaterialAmount(EntityUid uid, string materialId, int volume, MaterialStorageComponent? component = null, bool dirty = true, bool localOnly = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanChangeMaterialAmount(uid, materialId, volume, component, localOnly))
            return false;

        var changeEv = new ConsumeStoredMaterialsEvent((uid, component), new() {{materialId, volume}}, localOnly);
        RaiseLocalEvent(uid, ref changeEv);
        var remaining = changeEv.Materials.Values.First();

        var existing = component.Storage.GetOrNew(materialId);

        var localUpperLimit = component.StorageLimit == null ? int.MaxValue : component.StorageLimit.Value - existing;
        var localLowerLimit = -existing;
        var localChange = Math.Clamp(remaining, localLowerLimit, localUpperLimit);

        existing += localChange;

        if (existing == 0)
            component.Storage.Remove(materialId);
        else
            component.Storage[materialId] = existing;

        var ev = new MaterialAmountChangedEvent();
        RaiseLocalEvent(uid, ref ev);

        if (dirty)
            Dirty(uid, component);
        return true;
    }

    /// <summary>
    /// Changes the amount of a specific material in the storage.
    /// Still respects the filters in place.
    /// </summary>
    /// <returns>If the amount can be changed</returns>
    public bool TryChangeMaterialAmount(Entity<MaterialStorageComponent?> entity, Dictionary<string, int> materials, bool localOnly = false)
    {
        return TryChangeMaterialAmount(entity, materials.Select(p => (new ProtoId<MaterialPrototype>(p.Key), p.Value)).ToDictionary(), localOnly);
    }

    /// <summary>
    /// Changes the amount of a specific material in the storage.
    /// Still respects the filters in place.
    /// </summary>
    /// <returns>If the amount can be changed</returns>
    public bool TryChangeMaterialAmount(
        Entity<MaterialStorageComponent?> entity,
        Dictionary<ProtoId<MaterialPrototype>, int> materials,
        bool localOnly = false)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        foreach (var (material, amount) in materials)
        {
            if (!CanChangeMaterialAmount(entity, material, amount, entity))
                return false;
        }

        var changeEv = new ConsumeStoredMaterialsEvent((entity, entity.Comp), materials, localOnly);
        RaiseLocalEvent(entity, ref changeEv);

        foreach (var (material, remaining) in changeEv.Materials)
        {
            var existing = entity.Comp.Storage.GetOrNew(material);

            var localUpperLimit = entity.Comp.StorageLimit == null ? int.MaxValue : entity.Comp.StorageLimit.Value - existing;
            var localLowerLimit = -existing;
            var localChange = Math.Clamp(remaining, localLowerLimit, localUpperLimit);

            existing += localChange;

            if (existing == 0)
                entity.Comp.Storage.Remove(material);
            else
                entity.Comp.Storage[material] = existing;

        }

        var ev = new MaterialAmountChangedEvent();
        RaiseLocalEvent(entity, ref ev);

        Dirty(entity, entity.Comp);
        return true;
    }

    /// <summary>
    /// Tries to set the amount of material in the storage to a specific value.
    /// Still respects the filters in place.
    /// </summary>
    /// <param name="uid">The entity to change the material storage on.</param>
    /// <param name="materialId">The ID of the material to change.</param>
    /// <param name="volume">The stored material volume to set the storage to.</param>
    /// <param name="component">The storage component on <paramref name="uid"/>. Resolved automatically if not given.</param>
    /// <returns>True if it was successful (enough space etc).</returns>
    [PublicAPI]
    public bool TrySetMaterialAmount(
        EntityUid uid,
        string materialId,
        int volume,
        MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        var curAmount = GetMaterialAmount(uid, materialId, component);
        var delta = volume - curAmount;
        return TryChangeMaterialAmount(uid, materialId, delta, component);
    }

    /// <summary>
    /// Tries to insert an entity into the material storage.
    /// </summary>
    public virtual bool TryInsertMaterialEntity(EntityUid user,
        EntityUid toInsert,
        EntityUid receiver,
        MaterialStorageComponent? storage = null,
        MaterialComponent? material = null,
        PhysicalCompositionComponent? composition = null)
    {
        if (!Resolve(receiver, ref storage))
            return false;

        if (!Resolve(toInsert, ref material, ref composition, false))
            return false;

        if (_whitelistSystem.IsWhitelistFail(storage.Whitelist, toInsert))
            return false;

        if (HasComp<UnremoveableComponent>(toInsert))
            return false;

        // Material Whitelist checked implicitly by CanChangeMaterialAmount();

        var multiplier = TryComp<StackComponent>(toInsert, out var stackComponent) ? stackComponent.Count : 1;
        var totalVolume = 0;
        foreach (var (mat, vol) in composition.MaterialComposition)
        {
            if (!CanChangeMaterialAmount(receiver, mat, vol * multiplier, storage))
                return false;
            totalVolume += vol * multiplier;
        }

        if (!CanTakeVolume(receiver, totalVolume, storage, localOnly: true))
            return false;

        foreach (var (mat, vol) in composition.MaterialComposition)
        {
            TryChangeMaterialAmount(receiver, mat, vol * multiplier, storage);
        }

        var insertingComp = EnsureComp<InsertingMaterialStorageComponent>(receiver);
        insertingComp.EndTime = _timing.CurTime + storage.InsertionTime;
        if (!storage.IgnoreColor)
        {
            _prototype.TryIndex<MaterialPrototype>(composition.MaterialComposition.Keys.First(), out var lastMat);
            insertingComp.MaterialColor = lastMat?.Color;
        }
        _appearance.SetData(receiver, MaterialStorageVisuals.Inserting, true);
        Dirty(receiver, insertingComp);

        var ev = new MaterialEntityInsertedEvent(material);
        RaiseLocalEvent(receiver, ref ev);
        return true;
    }

    /// <summary>
    /// Broadcasts an event that will collect a list of which materials
    /// are allowed to be inserted into the materialStorage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public void UpdateMaterialWhitelist(EntityUid uid, MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;
        var ev = new GetMaterialWhitelistEvent(uid);
        RaiseLocalEvent(uid, ref ev);
        component.MaterialWhiteList = ev.Whitelist;
        Dirty(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, MaterialStorageComponent component, InteractUsingEvent args)
    {
        if (args.Handled || !component.InsertOnInteract)
            return;
        args.Handled = TryInsertMaterialEntity(args.User, args.Used, uid, component);
    }

    private void OnDatabaseModified(Entity<MaterialStorageComponent> ent, ref TechnologyDatabaseModifiedEvent args)
    {
        UpdateMaterialWhitelist(ent);
    }

    public int GetSheetVolume(MaterialPrototype material)
    {
        if (material.StackEntity == null)
            return DefaultSheetVolume;

        var proto = _prototype.Index<EntityPrototype>(material.StackEntity);

        if (!proto.TryGetComponent<PhysicalCompositionComponent>(out var composition, EntityManager.ComponentFactory))
            return DefaultSheetVolume;

        return composition.MaterialComposition.FirstOrDefault(kvp => kvp.Key == material.ID).Value;
    }
}
