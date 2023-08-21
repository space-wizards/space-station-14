using Content.Shared.Actions;
using Content.Shared.Ninja.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for a Space Ninja's katana, controls its dash sound.
/// Requires a ninja with a suit for abilities to work.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(EnergyKatanaSystem))]
public sealed partial class EnergyKatanaComponent : Component
{
    [DataField("ninja"), AutoNetworkedField]
    public EntityUid? Ninja = null;

    /// <summary>
    /// Sound played when using dash action.
    /// </summary>
    [DataField("blinkSound")]
    public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg")
    {
        Params = AudioParams.Default.WithVolume(5f)
    };
}

public sealed class KatanaDashEvent : WorldTargetActionEvent { }
