using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Races;

[RegisterComponent]
public sealed partial class FelinidComponent : Component
{
    /// <summary>
    /// The hairball prototype to use.
    /// </summary>
    [DataField]
    public EntProtoId HairballPrototype = "Hairball";

    [DataField]
    public EntProtoId HairballAction = "HairballAction";

    [DataField]
    public EntityUid? HairballActionEntity;

    public EntityUid? PotentialTarget = null;
    public EntityUid? EatMouse = null;

    [DataField]
    public SoundSpecifier EatSound = new SoundCollectionSpecifier("eating");
}
