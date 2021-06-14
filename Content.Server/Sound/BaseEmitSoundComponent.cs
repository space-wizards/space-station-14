using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Sound
{
    /// <summary>
    /// Base sound emitter which defines most of the data fields.
    /// Default behavior first try to play the sound collection,
    /// and if one isn't assigned, then it will try to play the single sound.
    /// </summary>
    public abstract class BaseEmitSoundComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("variation")] public float PitchVariation { get; set; }
        [ViewVariables(VVAccess.ReadWrite)] [DataField("soundCollection")] public string? SoundCollectionName { get; set; }
    }
}
