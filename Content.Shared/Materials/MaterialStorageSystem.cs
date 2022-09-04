using System.Linq;

namespace Content.Shared.Materials;

/// <summary>
/// This handles...
/// </summary>
public abstract class MaterialStorageSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
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
}
