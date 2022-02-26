using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.RandomAppearance;

[RegisterComponent]
[Friend(typeof(RandomAppearanceSystem))]
public sealed class RandomAppearanceComponent : Component, ISerializationHooks
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
    public System.Enum? EnumKey;

    void ISerializationHooks.AfterDeserialization()
    {
        if (IoCManager.Resolve<IReflectionManager>().TryParseEnumReference(EnumKeyRaw, out var @enum))
        {
            EnumKey = @enum;
        }
        else
        {
            Logger.Error($"RandomAppearance enum key {EnumKeyRaw} could not be parsed!");
        }
    }
}
