using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Clothing.Components;

[Access(typeof(StickyClothingSystem))]
[RegisterComponent]
public sealed class StickyClothingComponent : Component
{
    /// <summary>
    ///     Is that clothing is worn?
    /// </summary>
    public bool IsActive = false;
}
