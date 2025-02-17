using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Races;

[RegisterComponent]
public sealed partial class FelinidComponent : Component
{
    /// <summary>
    /// The hairball prototype to use.
    /// </summary>
    [DataField("hairballPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HairballPrototype = "Hairball";

    [DataField("hairballAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? HairballAction = "HairballAction";

    [DataField("hairballActionEntity")] public EntityUid? HairballActionEntity;

    public EntityUid? PotentialTarget = null;
    public EntityUid? EatMouse = null;

    [DataField]
    public SoundSpecifier EatSound = new SoundCollectionSpecifier("eating");
}
