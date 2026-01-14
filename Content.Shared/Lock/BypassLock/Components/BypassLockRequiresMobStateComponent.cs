using Content.Shared.Lock.BypassLock.Systems;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared.Lock.BypassLock.Components;

/// <summary>
/// This component lets the lock on this entity be pried open when the entity is in critical or dead state.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BypassLockSystem))]
public sealed partial class BypassLockRequiresMobStateComponent : Component
{
    /// <summary>
    /// The mobstate where the lock can be bypassed.
    /// </summary>
    [DataField]
    public HashSet<MobState> RequiredMobState = [];
}
