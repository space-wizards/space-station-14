using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

// I have tried to make this as generic as possible but "delete joint on cycle / right-click reels in" is very specific behavior.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrapplingGunComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("reeling"), AutoNetworkedField]
    public bool Reeling;

    [ViewVariables(VVAccess.ReadWrite), DataField("reelSound"), AutoNetworkedField]
    public SoundSpecifier? ReelSound = new SoundPathSpecifier("/Audio/Weapons/reel.ogg")
    {
        Params = AudioParams.Default.WithLoop(true)
    };

    public IPlayingAudioStream? Stream;
}
