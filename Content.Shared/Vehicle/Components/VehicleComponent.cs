using Content.Shared.Actions.ActionTypes;
using Content.Shared.Sound;
using Content.Shared.Vehicle;
using Robust.Shared.Utility;

namespace Content.Shared.Vehicle.Components
{
    /// <summary>
    /// This is particularly for vehicles that use
    /// buckle. Stuff like clown cars may need a different
    /// component at some point.
    /// All vehicles should have Physics, Strap, and SharedPlayerInputMover components.
    /// </summary>
    [RegisterComponent]
    public sealed class VehicleComponent : Component
    {
        /// <summary>
        /// Whether the vehicle currently has a key inside it
        /// </summary>
        public bool HasKey = false;

        /// <summary>
        /// Whether someone is currently riding the vehicle
        /// </summary
        public bool HasRider = false;

        /// <summary>
        /// The entity currently riding the vehicle.
        /// </summary>
        [ViewVariables]
        public EntityUid? Rider;

        /// <summary>
        /// Whether the vehicle should treat north as it's unique direction in its visualizer
        /// </summary>
        [DataField("northOnly")]
        public bool NorthOnly = false;

        /// <summary>
        /// What the y buckle offset should be in north / south
        /// </summary>
        [DataField("northOverride")]
        public float NorthOverride = 0f;

        /// <summary>
        /// What the y buckle offset should be in north / south
        /// </summary>
        [DataField("southOverride")]
        public float SouthOverride = 0f;

        /// <summary>
        /// The base offset for the vehicle (when facing east)
        /// </summary>
        public Vector2 BaseBuckleOffset = Vector2.Zero;

        /// <summary>
        /// The sound that the horn makes
        /// </summary>
        [DataField("hornSound")]
        public SoundSpecifier? HornSound = new SoundPathSpecifier("/Audio/Effects/Vehicle/carhorn.ogg");

        /// <summary>
        /// The sound that the vehicle makes with a key inserted
        /// </summary>
        [DataField("startupSound")]
        public SoundSpecifier StartupSound = new SoundPathSpecifier("/Audio/Effects/Vehicle/vehiclestartup.ogg");

        /// Use ambient sound component for the idle sound.

        /// <summary>
        /// The action for the horn (if any)
        /// </summary>
        [DataField("hornAction")]
        public InstantAction HornAction = new()
        {
        UseDelay = TimeSpan.FromSeconds(3.4),
        Icon = new SpriteSpecifier.Texture(new ResourcePath("Objects/Fun/bikehorn.rsi/icon.png")),
        Name = "action-name-honk",
        Description = "action-desc-honk",
        Event = new HonkActionEvent(),
        };

        /// <summary>
        /// The prototype for the key
        /// </summary>
        [DataField("key", required: true)]
        public string Key = string.Empty;
    }
}
