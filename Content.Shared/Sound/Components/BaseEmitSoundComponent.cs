using Robust.Shared.Audio;

namespace Content.Shared.Sound.Components;

/// <summary>
/// Base sound emitter which defines most of the data fields.
/// Accepts both single sounds and sound collections.
/// </summary>
public abstract partial class BaseEmitSoundComponent : Component
{
    public static readonly AudioParams DefaultParams = AudioParams.Default.WithVolume(-2f);

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public SoundSpecifier? Sound;
}
