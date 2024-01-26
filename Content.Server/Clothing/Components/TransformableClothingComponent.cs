using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Clothing.Components;


/// <summary>
/// This component is used for clothing that can be transformed entirely
/// from one form to another, e.g. head bandanas to face bandanas
/// </summary>
[RegisterComponent]
public sealed partial class TransformableClothingComponent : Component
{
    /// <summary>
    /// The prototype this clothing will be turned into.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId TransformProto;

    /// <summary>
    /// How long it takes to transform the clothing.
    /// </summary>
    [DataField]
    public TimeSpan TransformDelay = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    /// The sound to play when transforming the clothing.
    /// </summary>
    [DataField]
    public SoundSpecifier? TransformSound = new SoundCollectionSpecifier("storageRustle", new AudioParams().WithVolume(6));
}
