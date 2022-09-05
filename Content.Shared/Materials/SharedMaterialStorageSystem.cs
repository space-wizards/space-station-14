using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Stacks;

namespace Content.Shared.Materials;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedMaterialStorageSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialStorageComponent, InteractUsingEvent>(OnInteractUsing);
    }

    public int GetMaterialAmount(MaterialStorageComponent component, MaterialPrototype material)
    {
        return GetMaterialAmount(component, material.ID);
    }

    public int GetMaterialAmount(MaterialStorageComponent component, string material)
    {
        return !component.Storage.TryGetValue(material, out var amount) ? 0 : amount;
    }

    public int GetTotalMaterialAmount(MaterialStorageComponent component)
    {
        return component.Storage.Values.Sum();
    }

    public bool CanTakeVolume(MaterialStorageComponent component, int volume)
    {
        return component.StorageLimit == null || (GetTotalMaterialAmount(component) + volume <= component.StorageLimit);
    }

    public bool CanChangeMaterialAmount(MaterialStorageComponent component, string materialId, int volume)
    {
        return CanTakeVolume(component, volume) &&
               (component.MaterialWhiteList == null || component.MaterialWhiteList.Contains(materialId)) &&
               (!component.Storage.TryGetValue(materialId, out var amount) || amount + volume >= 0);
    }

    public bool TryChangeMaterialAmount(MaterialStorageComponent component, string materialId, int volume)
    {
        if (!CanChangeMaterialAmount(component, materialId, volume))
            return false;

        if (!component.Storage.ContainsKey(materialId))
            component.Storage.Add(materialId, 0);
        component.Storage[materialId] += volume;

        return true;
    }

    public bool TryInsertMaterialEntity(EntityUid toInsert, MaterialStorageComponent component)
    {
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

        var multiplier = TryComp<SharedStackComponent>(toInsert, out var stackComponent) ? stackComponent.Count : 1;

        var totalVolume = 0;
        foreach (var (mat, vol) in component.Storage)
        {
            if (!CanChangeMaterialAmount(component, mat, vol))
                return false;
            totalVolume += vol * multiplier;
        }

        if (!CanTakeVolume(component, totalVolume))
            return false;

        foreach (var (mat, vol) in material._materials)
        {
            TryChangeMaterialAmount(component, mat, vol * multiplier);
        }

        OnFinishInsertMaterialEntity(toInsert, component);
        RaiseLocalEvent(component.Owner, new MaterialEntityInsertedEvent(material._materials));
        return true;
    }

    /// <remarks>
    ///     This is done because of popup spam and not being able
    ///     to do entity deletion clientside.
    /// </remarks>
    protected abstract void OnFinishInsertMaterialEntity(EntityUid toInsert, MaterialStorageComponent component);

    private void OnInteractUsing(EntityUid uid, MaterialStorageComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        TryInsertMaterialEntity(args.Used, component);
    }
}
