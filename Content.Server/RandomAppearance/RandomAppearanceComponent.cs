using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.RandomAppearance;

[RegisterComponent, ComponentProtoName("RandomAppearance")]
[Friend(typeof(RandomAppearanceSystem))]
public class RandomAppearanceComponent : Component
{
    [DataField("spriteStates")]
    public string[] SpriteStates = {"0", "1", "2", "3", "4"};

    /// <summary>
    ///     What appearance enum key should be set to the random sprite state?
    /// </summary>
    [DataField("key", required: true)]
    public string EnumKeyRaw = default!;

    /// <summary>
    ///     The actual enum after reflection.
    /// </summary>
    public object EnumKey = default!;
}
