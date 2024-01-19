
using Content.Shared.Chemistry;
using Robust.Shared.Audio;

namespace Content.Client.Chemistry.Components;

[RegisterComponent]
public sealed partial class MedipenRefillerComponent : Component
{
    [DataField]
    public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks")
    {
        Params = AudioParams.Default.WithVolume(8f)
    };
}
