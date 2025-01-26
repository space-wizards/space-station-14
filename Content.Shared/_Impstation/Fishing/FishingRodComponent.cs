using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Fishing;

// I have tried to make this as generic as possible but "delete joint on cycle / right-click reels in" is very specific behavior.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
//Imp : Basically a copy of GrapplingGunComponent
public sealed partial class FishingRodComponent : Component
{
    /// <summary>
    /// Hook's reeling force and speed - the higher the number, the faster the hook rewinds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReelRate = 2.5f;

    [DataField("jointId"), AutoNetworkedField]
    public string Joint = string.Empty;

    [DataField, AutoNetworkedField]
    public EntityUid? Projectile;

    [DataField, AutoNetworkedField]
    public bool Reeling;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ReelSound = new SoundPathSpecifier("/Audio/Weapons/reel.ogg")
    {
        Params = AudioParams.Default.WithLoop(true)
    };

    [DataField, AutoNetworkedField]
    public SoundSpecifier? CycleSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/kinetic_reload.ogg");

    [DataField]
    public SpriteSpecifier RopeSprite =
        new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");

    public EntityUid? Stream;
}
