using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Fluids.Components
{
    /// <summary>
    /// Puddle on a floor
    /// </summary>
    [RegisterComponent, NetworkedComponent, Access(typeof(SharedPuddleSystem))]
    public sealed partial class PuddleComponent : Component
    {
        [DataField("spillSound")]
        public SoundSpecifier SpillSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

        [DataField("overflowVolume")]
        public FixedPoint2 OverflowVolume = FixedPoint2.New(20);

        [DataField("solution")] public string SolutionName = "puddle";
    }
}
