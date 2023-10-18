using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Communications;

/// <summary>
/// Only exists in shared to provide API and for access.
/// All logic is serverside.
/// </summary>
public abstract class SharedCommsHackerSystem : EntitySystem
{
    /// <summary>
    /// Set the threats prototype to choose from when hacking a comms console.
    /// </summary>
    public void SetThreats(EntityUid uid, string threats, CommsHackerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Threats = threats;
    }
}

/// <summary>
/// DoAfter event for comms console terror ability.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TerrorDoAfterEvent : SimpleDoAfterEvent { }
