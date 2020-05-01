using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Sound
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

        public string _soundName;
        public float _pitchVariation;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundName, "sound", string.Empty);
            serializer.DataField(ref _pitchVariation, "variation", 0.0f);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(_soundName))
            {
                if (_pitchVariation > 0.0)
                {
                    Owner.GetComponent<SoundComponent>().Play(_soundName, AudioHelpers.WithVariation(_pitchVariation).WithVolume(-2f));
                    return true;
                }
                Owner.GetComponent<SoundComponent>().Play(_soundName, AudioParams.Default.WithVolume(-2f));
                return true;
            }
            return false;
        }
    }
}
