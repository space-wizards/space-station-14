using Content.Shared.Audio;
using Content.Shared.Slippery;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Slippery
{
    internal sealed class SlipperySystem : SharedSlipperySystem
    {
        protected override void PlaySound(SlipperyComponent component)
        {
            SoundSystem.Play(component.SlipSound.GetSound(), Filter.Pvs(component.Owner), component.Owner, AudioHelpers.WithVariation(0.2f));
        }
    }
}
