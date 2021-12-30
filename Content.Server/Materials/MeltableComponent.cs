using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Materials;

/// <summary>
/// Objects with this component can be stuck in an ORM and melted into their designated material.
/// </summary>
[RegisterComponent]
public class MeltableComponent : Component
{
    public override string Name => "Meltable";

    [DataField("material", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<MaterialPrototype>))]
    public string Material { get; } = default!;

    [DataField("units")]
    public int Units { get; } = 1;
}
