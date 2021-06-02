using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Sound
{
    /// <summary>
    /// Simple sound emitter that emits sound on land
    /// </summary>
    [RegisterComponent]
    public class EmitSoundOnLandComponent : Component, ILand
    {
        /// <inheritdoc />
        ///
        public override string Name => "EmitSoundOnLand";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("sound")] public string? _soundName;

        void ILand.Land(LandEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(_soundName))
            {
                SoundSystem.Play(Filter.Pvs(Owner), _soundName, Owner, AudioParams.Default.WithVolume(-2f));
            }
        }
    }
}
