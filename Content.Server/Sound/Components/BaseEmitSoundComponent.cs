using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Sound.Components
{
    /// <summary>
    /// Base sound emitter which defines most of the data fields.
    /// Default behavior is to first try to play the sound collection,
    /// and if one isn't assigned, then try to play the single sound.
    /// </summary>
    public abstract class BaseEmitSoundComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("sound")] public string? SoundName { get; set; } = default!;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("variation")] public float PitchVariation { get; set; } = 0.0f;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("soundCollection")] public string? SoundCollectionName { get; set; } = default!;
    }
}
