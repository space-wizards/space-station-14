using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Audio;
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
        /// Whether someone is currently riding the vehicle
        /// </summary>
        public bool HasRider => Rider != null;

        /// <summary>
        /// The entity currently riding the vehicle.
        /// </summary>
        [ViewVariables]
        public EntityUid? Rider;

        /// <summary>
        /// The base offset for the vehicle (when facing east)
        /// </summary>
        public Vector2 BaseBuckleOffset = Vector2.Zero;

        /// <summary>
        /// The sound that the horn makes
        /// </summary>
        [DataField("hornSound")] public SoundSpecifier? HornSound =
        new SoundPathSpecifier("/Audio/Effects/Vehicle/carhorn.ogg")
        {
            Params = AudioParams.Default.WithVolume(-3f)
        };

        public IPlayingAudioStream? HonkPlayingStream;

        /// Use ambient sound component for the idle sound.

        /// <summary>
        /// The action for the horn (if any)
        /// </summary>
        [DataField("hornAction")]
        public InstantAction HornAction = new()
        {
            UseDelay = TimeSpan.FromSeconds(3.4),
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Objects/Fun/bikehorn.rsi/icon.png")),
            DisplayName = "action-name-honk",
            Description = "action-desc-honk",
            Event = new HonkActionEvent(),
        };

        /// <summary>
        /// Whether the vehicle has a key currently inside it or not.
        /// </summary>
        [ViewVariables]
        public bool HasKey = false;

        // TODO: Fix this
        /// <summary>
        /// Whether the vehicle should treat north as its unique direction in its visualizer
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
    }
}
