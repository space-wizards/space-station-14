using Content.Server.GameObjects.EntitySystems;
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundName, "sound", "");
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(_soundName))
            {
                Owner.GetComponent<SoundComponent>().Play(_soundName, AudioParams.Default.WithVolume(-2f));
                return true;
            }
            return false;
        }
    }
}
