using Robust.Shared.GameStates;

namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// This status effect will add examination text directly to the afflicted individual.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ExaminableStatusEffectSystem))]
public sealed partial class ExaminableStatusEffectComponent : Component
{
    /// <summary>
    /// The text added to the afflicted.
    /// </summary>
    /// <value> Parameters passed in:
    /// <list type="bullet">
    ///     <item><c>subject</c> - The identity of the afflicted.</item>
    /// </list>
    /// </value>
    [DataField(required: true)]
    public LocId MessageId;

    /// <summary>
    /// If true, will only display the status effect if the afflicted has a
    /// <see cref="MobStateComponent" /> whose status is not critical or dead.
    /// </summary>
    [DataField]
    public bool RequiresConciousness = false;
}
