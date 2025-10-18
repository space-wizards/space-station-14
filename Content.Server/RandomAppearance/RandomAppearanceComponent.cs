using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.RandomAppearance;

[RegisterComponent]
[Access(typeof(RandomAppearanceSystem))]
public sealed partial class RandomAppearanceComponent : Component
{
    [DataField("spriteStates")]
    public string[] SpriteStates = { "0", "1", "2", "3", "4" };

    /// <summary>
    ///     What appearance enum key should be set to the random sprite state?
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(EnumSerializer))]
    public Enum? EnumKey;
}
