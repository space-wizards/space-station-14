using Content.Server.Body.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Body.Components
{
    /// <summary>
    ///     Attempts to pump blood throughout the body.
    /// </summary>
    [RegisterComponent, Friend(typeof(CirculatorSystem))]
    public class CirculatorComponent : Component
    {
        public override string Name { get; } = "Circulator";

        public float AccumulatedFrametime = 0.0f;

        /// <summary>
        ///     How often this circulator should attempt to pump blood
        /// </summary>
        [DataField("baseHeartRate")]
        public float HeartRate = 0.5f;
    }
}
