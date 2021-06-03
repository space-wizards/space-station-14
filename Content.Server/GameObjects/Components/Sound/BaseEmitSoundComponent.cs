using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Sound
{
    public enum EmitSoundMode
    {
        Auto,
        Single,
        RandomFromCollection
    }

    /// <summary>
    /// Base sound emitter which defines most of the data fields.
    /// Default EmitSoundMode is Auto, which will first try to play a sound collection,
    /// and if one isn't assigned, then it will try to play a single sound.
    /// </summary>
    public abstract class BaseEmitSoundComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("sound")] public string? _soundName;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("variation")] public float _pitchVariation;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("semitoneVariation")] public int _semitoneVariation;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("soundCollection")] public string? _soundCollectionName;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("emitSoundMode")] public EmitSoundMode _emitSoundMode;
    }
}
