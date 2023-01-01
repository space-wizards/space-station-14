using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Parallax.Biomes;

[RegisterComponent, NetworkedComponent]
public sealed class BiomeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("seed")]
    public int Seed;

    [ViewVariables(VVAccess.ReadWrite),
     DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<BiomePrototype>))]
    public string BiomePrototype = "Grasslands";
}
