using Robust.Shared.Audio;

namespace Content.Server.Medical.BiomassReclaimer
{
    [RegisterComponent]
    public sealed partial class ActiveBiomassReclaimerComponent : Component
    {
        /// <summary>
        /// Sound to be played when the reclaimer is activated.
        /// </summary>
        public readonly SoundSpecifier StartupSound = new SoundPathSpecifier("/Audio/Machines/reclaimer_startup.ogg");
    }
}
