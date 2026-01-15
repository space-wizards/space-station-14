using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Components;

// I have tried to make this as generic as possible but "delete joint on cycle / right-click reels in" is very specific behavior.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    public float ReelForce = 10000f;

    /// <summary>
    /// Highest mass that can be reeled in without resistance
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReelMassCoefficient = 50f;

    /// <summary>
    /// Margin between max length and the grappling gun when reeling the grappling hook in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RopeMargin = 0.2f;
    
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
    public float RopeMaxLength = 10f;

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

    [DataField("jointId"), AutoNetworkedField]
    public string Joint = string.Empty;

    [DataField, AutoNetworkedField]
    public EntityUid? Projectile;

    [ViewVariables(VVAccess.ReadWrite), DataField("reeling"), AutoNetworkedField]
    public bool Reeling;

    [ViewVariables(VVAccess.ReadWrite), DataField("reelSound"), AutoNetworkedField]
    public SoundSpecifier? ReelSound = new SoundPathSpecifier("/Audio/Weapons/reel.ogg")
    {
        Params = AudioParams.Default.WithLoop(true)
    };

    [ViewVariables(VVAccess.ReadWrite), DataField("cycleSound"), AutoNetworkedField]
    public SoundSpecifier? CycleSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/kinetic_reload.ogg");

    /// <summary>
    /// Sound that plays when the rope breaks due to physics
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("breakSound"), AutoNetworkedField]
    public SoundSpecifier? BreakSound = new SoundPathSpecifier("/Audio/Items/snap.ogg");

    [DataField, ViewVariables]
    public SpriteSpecifier RopeSprite =
        new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");

    public EntityUid? Stream;
}
