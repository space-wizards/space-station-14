using Content.Shared.Beeper.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Beeper.Components;

/// <summary>
/// This is used for an item that beeps based on proximity to a specified component.
/// </summary>
/// <remarks>
/// Requires <see cref="ItemToggleComponent"/> to control it. Otherwise, will be always active.
/// </remarks>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(BeeperSystem))]
public sealed partial class BeeperComponent : Component
{
    /// <summary>
    /// Default beeper sound.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultBeep = new("BeeperBeep");

    /// <summary>
    /// How much to scale the interval by. Min - 0, max - 1.
    /// Setting this will lerp Interval between MinBeepInterval and MaxBeepInterval.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 IntervalScaling = 0;

    /// <summary>
    /// The maximum interval between beeps.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxBeepInterval = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    /// The minimum interval between beeps.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MinBeepInterval = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    /// Interval for the next beep
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Interval;

    /// <summary>
    /// Next time beeper beeps.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBeep = TimeSpan.Zero;

    /// <summary>
    /// Is the beep muted
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsMuted;

    /// <summary>
    /// The sound played when the locator beeps.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier BeepSound = new SoundCollectionSpecifier(DefaultBeep)
    {
        Params = AudioParams.Default.WithMaxDistance(1f),
    };
}
