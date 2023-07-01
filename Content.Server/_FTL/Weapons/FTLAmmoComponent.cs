using Content.Shared.Tag;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._FTL.Weapons;

/// <summary>
/// This is used for tracking the physical ammo used by silos.
/// </summary>
[RegisterComponent]
public sealed class FTLAmmoComponent : Component
{
    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<FTLAmmoType>))]
    public string Prototype { get; set; } = "";

    [DataField("tag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
    public string Tag = default!;
}
