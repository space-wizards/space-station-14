using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Allows a changeling slug to take over a corpse and become a full changeling again.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingSlugComponent : Component
{
    /// <summary>
    /// How long it takes to take over a corpse.
    /// </summary>
    [DataField]
    public TimeSpan TakeOverDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The sound to play when starting the takeover.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
}

/// <summary>
/// Action event for taking over a corpse.
/// </summary>
[ByRefEvent]
public sealed partial class ChangelingTakeOverCorpseActionEvent : EntityTargetActionEvent;

/// <summary>
/// DoAfter event for the takeover process.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ChangelingTakeOverCorpseDoAfterEvent : SimpleDoAfterEvent;
