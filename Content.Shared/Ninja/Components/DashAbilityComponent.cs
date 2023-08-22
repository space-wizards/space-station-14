using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Ninja.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

[RegisterComponent, NetworkedComponent, Access(typeof(DashAbilitySystem))]
public sealed class DashAbilityComponent : Component
{
    /// <summary>
    /// The action for dashing.
    /// </summary>
    [DataField("dashAction", required: true)]
    public WorldTargetAction KatanaDashAction = default!;

    /// <summary>
    /// Sound played when using dash action.
    /// </summary>
    [DataField("blinkSound")]
    public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg")
    {
        Params = AudioParams.Default.WithVolume(5f)
    };
}

public sealed class DashEvent : WorldTargetActionEvent { }
