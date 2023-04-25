using Content.Shared.Actions.ActionTypes;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Vehicle.Components
{
    /// <summary>
    /// This is particularly for vehicles that use
    /// buckle. Stuff like clown cars may need a different
    /// component at some point.
    /// All vehicles should have Physics, Strap, and SharedPlayerInputMover components.
    /// </summary>
    [AutoGenerateComponentState]
    [RegisterComponent, NetworkedComponent]
    public sealed partial class VehicleComponent : Component
    {
        /// <summary>
        /// Whether someone is currently riding the vehicle
        /// </summary>
        public bool HasRider => Rider != null;

        /// <summary>
        /// The entity currently riding the vehicle.
        /// </summary>
        [ViewVariables]
        [AutoNetworkedField]
        public EntityUid? Rider;

        [ViewVariables]
        [AutoNetworkedField]
        public EntityUid? LastRider;

        [DataField("whitelist")]
        [ViewVariables]
        public EntityWhitelist? Whitelist;

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
            Icon = new SpriteSpecifier.Texture(new("Objects/Fun/bikehorn.rsi/icon.png")),
            DisplayName = "action-name-honk",
            Description = "action-desc-honk",
            Event = new HonkActionEvent(),
        };

        /// <summary>
        /// Whether the vehicle has a key currently inside it or not.
        /// </summary>
        [DataField("hasKey")]
        public bool HasKey = false;

        /// <summary>
        /// Determines from which side the vehicle will be displayed on top of the player.
        /// </summary>

        [DataField("southOver")]
        public bool SouthOver = false;

        [DataField("northOver")]
        public bool NorthOver = false;

        [DataField("westOver")]
        public bool WestOver = false;

        [DataField("eastOver")]
        public bool EastOver = false;

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

        [ViewVariables]
        public int DrawDepth = 0;

        [DataField("autoAnimate")]
        [ViewVariables]
        public bool AutoAnimate;

        [ViewVariables]
        [DataField("hideRider")]
        public bool HideRider = false;
    }
}
