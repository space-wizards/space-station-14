using Content.Server.Administration.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Administration.Components;

/// <summary>
/// Component to track the timer for the SuperBonk smite.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(SuperBonkSystem))]
public sealed partial class SuperBonkComponent : Component
{
    /// <summary>
    /// All of the tables the target will be bonked on.
    /// </summary>
    [DataField]
    public List<EntityUid>.Enumerator Tables;

    /// <summary>
    /// How often should we bonk.
    /// </summary>
    [DataField]
    public TimeSpan BonkCooldown = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Next time when we will bonk.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextBonk = TimeSpan.Zero;

    /// <summary>
    /// Whether to remove the clumsy component from the target after SuperBonk is done.
    /// </summary>
    [DataField]
    public bool RemoveClumsy = true;

    /// <summary>
    /// Whether to stop Super Bonk on the target once he dies. Otherwise it will continue until no other tables are left
    /// or the target is gibbed.
    /// </summary>
    [DataField]
    public bool StopWhenDead = true;
}
