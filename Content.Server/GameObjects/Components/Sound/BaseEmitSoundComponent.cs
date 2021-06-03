using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
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
    /// Base sound emitter which implements most of the logic.
    /// Default EmitSoundMode is Auto, which will first try to play a sound collection,
    /// and if one isn't assigned, then it will try to play a single sound.
    /// </summary>
    public abstract class BaseEmitSoundComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("sound")] public string? _soundName;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("variation")] public float _pitchVariation;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("semitoneVariation")] public int _semitoneVariation;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("soundCollection")] public string? _soundCollectionName;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("emitSoundMode")] public EmitSoundMode _emitSoundMode;

        public void PlaySoundBasedOnMode()
        {
            switch (_emitSoundMode)
            {
                case EmitSoundMode.Single:
                    PlaySingleSound();
                    break;
                case EmitSoundMode.RandomFromCollection:
                    PlayRandomSoundFromCollection();
                    break;
                case EmitSoundMode.Auto:
                    if (!string.IsNullOrEmpty(_soundCollectionName))
                    {
                        PlayRandomSoundFromCollection();
                        break;
                    }
                    PlaySingleSound();
                    break;
                default:
                    break;
            }
        }

        public void PlayRandomSoundFromCollection()
        {
             if (!string.IsNullOrWhiteSpace(_soundCollectionName))
            {
                var file = SelectRandomSoundFromSoundCollection(_soundCollectionName);
                PlaySingleSound(file);
            }
        }

        public bool PlaySingleSound()
        {
            if (!string.IsNullOrWhiteSpace(_soundName))
            {
                return PlaySingleSound(_soundName);
            }

            return false;
        }

        protected string SelectRandomSoundFromSoundCollection(string soundCollectionName)
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(soundCollectionName);
            return _random.Pick(soundCollection.PickFiles);
        }

        protected bool PlaySingleSound(string soundName)
        {
            if (!string.IsNullOrWhiteSpace(soundName))
            {
                if (_pitchVariation > 0.0)
                {
                    SoundSystem.Play(Filter.Pvs(Owner), soundName, Owner, AudioHelpers.WithVariation(_pitchVariation).WithVolume(-2f));
                    return true;
                }

                if (_semitoneVariation > 0)
                {
                    SoundSystem.Play(Filter.Pvs(Owner), soundName, Owner, AudioHelpers.WithSemitoneVariation(_semitoneVariation).WithVolume(-2f));
                    return true;
                }

                SoundSystem.Play(Filter.Pvs(Owner), soundName, Owner, AudioParams.Default.WithVolume(-2f));
                return true;
            }

            return false;
        }
    }
}
