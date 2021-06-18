using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Sound
{
    /// <summary>
    /// Base sound emitter which defines most of the data fields.
    /// </summary>
    public abstract class BaseEmitSoundComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("variation")] public float PitchVariation { get; set; } = 0.0f;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("soundCollection", required: true)] public string SoundCollectionName { get; set; } = default!;
    }
}
