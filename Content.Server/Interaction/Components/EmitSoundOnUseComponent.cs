using Content.Shared.Audio;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Interaction.Components
{
    /// <summary>
    /// Simple sound emitter that emits sound on use in hand
    /// </summary>
    [RegisterComponent]
    public class EmitSoundOnUseComponent : Component, IUse
    {
        /// <inheritdoc />
        ///
        public override string Name => "EmitSoundOnUse";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("sound")] public string? _soundName;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("variation")] public float _pitchVariation;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("semitoneVariation")] public int _semitoneVariation;

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(_soundName))
            {
                if (_pitchVariation > 0.0)
                {
                    SoundSystem.Play(Filter.Pvs(Owner), _soundName, Owner, AudioHelpers.WithVariation(_pitchVariation).WithVolume(-2f));
                    return true;
                }
                if (_semitoneVariation > 0)
                {
                    SoundSystem.Play(Filter.Pvs(Owner), _soundName, Owner, AudioHelpers.WithSemitoneVariation(_semitoneVariation).WithVolume(-2f));
                    return true;
                }
                SoundSystem.Play(Filter.Pvs(Owner), _soundName, Owner, AudioParams.Default.WithVolume(-2f));
                return true;
            }
            return false;
        }
    }
}
