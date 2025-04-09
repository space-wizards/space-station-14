using Robust.Shared.Audio;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class BlazingDashComponent : Component
{
    /// <summary>
    ///     Indicates whether the heretic is using blazing dash.
    /// </summary>
    [DataField, AutoNetworkedField] public bool BlazingDashActive;

    /// <summary>
    ///     Indicates when blazing dash should end.
    /// </summary>
    [ViewVariables, AutoPausedField] public TimeSpan BlazingDashEndTime;

    public TimeSpan BlazingDashDuration = TimeSpan.FromSeconds(5);
    public SoundSpecifier DashSound = new SoundPathSpecifier("/Audio/Magic/fireball.ogg");
    public int DashFireStacks = 4;
    public float DashWalkSpeed = 1.5f;
    public float DashRunSpeed = 2f;
}

