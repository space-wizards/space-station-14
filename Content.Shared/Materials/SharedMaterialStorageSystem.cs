using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

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

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialStorageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MaterialStorageComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MaterialStorageComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<MaterialStorageComponent, ComponentHandleState>(OnHandleState);
    }

    public override void Update(float frameTime)
    {
        if (_timing.InPrediction)
            return;

        base.Update(frameTime);
        foreach (var inserting in EntityQuery<InsertingMaterialStorageComponent>())
        {
            if (_timing.CurTime < inserting.EndTime)
                continue;

            _appearance.SetData(inserting.Owner, MaterialStorageVisuals.Inserting, false);
            RemComp(inserting.Owner, inserting);
        }
    }

    private void OnMapInit(EntityUid uid, MaterialStorageComponent component, MapInitEvent args)
    {
        _appearance.SetData(uid, MaterialStorageVisuals.Inserting, false);
    }

    private void OnGetState(EntityUid uid, MaterialStorageComponent component, ref ComponentGetState args)
    {
        args.State = new MaterialStorageComponentState(component.Storage, component.MaterialWhiteList);
    }

    private void OnHandleState(EntityUid uid, MaterialStorageComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MaterialStorageComponentState state)
            return;

        component.Storage = new Dictionary<string, int>(state.Storage);

        if (state.MaterialWhitelist != null)
            component.MaterialWhiteList = new List<string>(state.MaterialWhitelist);
    }

    /// <summary>
    /// Gets the volume of a specified material contained in this storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="material"></param>
    /// <param name="component"></param>
    /// <returns>The volume of the material</returns>
    [PublicAPI]
    public int GetMaterialAmount(EntityUid uid, MaterialPrototype material, MaterialStorageComponent? component = null)
    {
        return GetMaterialAmount(uid, material.ID, component);
    }

    /// <summary>
    /// Gets the volume of a specified material contained in this storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="material"></param>
    /// <param name="component"></param>
    /// <returns>The volume of the material</returns>
    public int GetMaterialAmount(EntityUid uid, string material, MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0; //you have nothing
        return !component.Storage.TryGetValue(material, out var amount) ? 0 : amount;
    }

    /// <summary>
    /// Gets the total volume of all materials in the storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns>The volume of all materials in the storage</returns>
    public int GetTotalMaterialAmount(EntityUid uid, MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0;
        return component.Storage.Values.Sum();
    }

    /// <summary>
    /// Tests if a specific amount of volume will fit in the storage.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="volume"></param>
    /// <param name="component"></param>
    /// <returns>If the specified volume will fit</returns>
    public bool CanTakeVolume(EntityUid uid, int volume, MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        return component.StorageLimit == null || GetTotalMaterialAmount(uid, component) + volume <= component.StorageLimit;
    }

    /// <summary>
    /// Checks if the specified material can be changed by the specified volume.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="materialId"></param>
    /// <param name="volume"></param>
    /// <param name="component"></param>
    /// <returns>If the amount can be changed</returns>
    public bool CanChangeMaterialAmount(EntityUid uid, string materialId, int volume, MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        return CanTakeVolume(uid, volume, component) &&
               (component.MaterialWhiteList == null || component.MaterialWhiteList.Contains(materialId)) &&
               (!component.Storage.TryGetValue(materialId, out var amount) || amount + volume >= 0);
    }

    /// <summary>
    /// Changes the amount of a specific material in the storage.
    /// Still respects the filters in place.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="materialId"></param>
    /// <param name="volume"></param>
    /// <param name="component"></param>
    /// <returns>If it was successful</returns>
    public bool TryChangeMaterialAmount(EntityUid uid, string materialId, int volume, MaterialStorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (!CanChangeMaterialAmount(uid, materialId, volume, component))
            return false;
        if (!component.Storage.ContainsKey(materialId))
            component.Storage.Add(materialId, 0);
        component.Storage[materialId] += volume;

        RaiseLocalEvent(uid, new MaterialAmountChangedEvent());
        Dirty(component);
        return true;
    }

    /// <summary>
    /// Tries to insert an entity into the material storage.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="toInsert"></param>
    /// <param name="receiver"></param>
    /// <param name="component"></param>
    /// <returns>If it was successful</returns>
    public virtual bool TryInsertMaterialEntity(EntityUid user, EntityUid toInsert, EntityUid receiver, MaterialStorageComponent? component = null)
    {
        if (!Resolve(receiver, ref component))
            return false;

        if (!TryComp<MaterialComponent>(toInsert, out var material))
            return false;

        if (component.EntityWhitelist?.IsValid(toInsert) == false)
            return false;

        if (component.MaterialWhiteList != null)
        {
            var matUsed = false;
            foreach (var mat in material.Materials)
            {
                if (component.MaterialWhiteList.Contains(mat.ID))
                    matUsed = true;
            }

            if (!matUsed)
                return false;
        }

        var multiplier = TryComp<StackComponent>(toInsert, out var stackComponent) ? stackComponent.Count : 1;
        var totalVolume = 0;
        foreach (var (mat, vol) in component.Storage)
        {
            if (!CanChangeMaterialAmount(receiver, mat, vol, component))
                return false;
            totalVolume += vol * multiplier;
        }

        if (!CanTakeVolume(receiver, totalVolume, component))
            return false;

        foreach (var (mat, vol) in material._materials)
        {
            TryChangeMaterialAmount(receiver, mat, vol * multiplier, component);
        }

        var insertingComp = EnsureComp<InsertingMaterialStorageComponent>(receiver);
        insertingComp.EndTime = _timing.CurTime + component.InsertionTime;
        if (!component.IgnoreColor)
        {
            _prototype.TryIndex<MaterialPrototype>(material._materials.Keys.Last(), out var lastMat);
            insertingComp.MaterialColor = lastMat?.Color;
        }
        _appearance.SetData(receiver, MaterialStorageVisuals.Inserting, true);

        RaiseLocalEvent(component.Owner, new MaterialEntityInsertedEvent(material._materials));
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
        RaiseLocalEvent(uid, ev);
        component.MaterialWhiteList = ev.Whitelist;
        Dirty(component);
    }

    private void OnInteractUsing(EntityUid uid, MaterialStorageComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = TryInsertMaterialEntity(args.User, args.Used, uid, component);
    }
}
