using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Components;

// I have tried to make this as generic as possible but "delete joint on cycle / right-click reels in" is very specific behavior.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class GrapplingGunComponent : Component
{
    /// <summary>
    /// Hook's reeling speed when there's no resistance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReelRate = 2.5f;

    /// <summary>
    /// Amount of force to use while reeling. This is made extremely small when compensating for frametime
    /// Don't be afraid to use large numbers, but do beware that this becomes fast as fuck in frictionless conditions such as space
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReelForce = 4000f;

    /// <summary>
    /// Highest mass that can be reeled in without resistance
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReelMassCoefficient = 80f;

    /// <summary>
    /// Margin between max length and the grappling gun when reeling the grappling hook in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RopeMargin = 0.2f;

    /// <summary>
    /// Margin from the min length for the rope to be considered fully reeled-in, preventing it from being reeled in further
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RopeFullyReeledMargin = 0.22f;

    /// <summary>
    /// Minimum length for the grappling hook's rope
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RopeMinLength = 1f;

    /// <summary>
    /// Maximum length the grapple can actually be.
    /// If this is too large, then the rope gets culled out of PVS, causing issues
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? RopeMaxLength;

    /// <summary>
    /// Stiffness of the rope, in N/m
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RopeStiffness = 1f;

    /// <summary>
    /// Amount of force, in newtons, needed to snap the rope
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RopeBreakPoint = 50000f;

    /// <summary>
    /// Entity UID of the grapple's hook
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Projectile;

    /// <summary>
    /// Whether or not the grappling gun is currently reeling in
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Reeling;

    /// <summary>
    /// Looping sound used while the grappling gun is reeling
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ReelSound = new SoundPathSpecifier("/Audio/Weapons/reel.ogg")
    {
        Params = AudioParams.Default.WithLoop(true)
    };

    /// <summary>
    /// Sound that plays when the user cycles the grappling gun by using it in their hand
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? CycleSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/kinetic_reload.ogg");

    /// <summary>
    /// Sound that plays when the rope breaks due to physics
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? BreakSound = new SoundPathSpecifier("/Audio/Items/snap.ogg");

    /// <summary>
    /// Sprite specifier for the rope, used to visualize the joint
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier RopeSprite =
        new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");

    /// <summary>
    /// Entity UID for the audio stream, which plays <see cref="ReelSound"/>.
    /// </summary>
    [ViewVariables]
    public EntityUid? Stream;
}
