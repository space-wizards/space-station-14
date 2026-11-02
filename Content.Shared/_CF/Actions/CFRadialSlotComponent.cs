using Robust.Shared.GameStates;

namespace Content.Shared.CF.Actions;

/// <summary>
/// Puts an action on the radial wheel. Spell rank 0 is a cantrip.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFRadialSlotComponent : Component
{
    [DataField, AutoNetworkedField]
    public CFRadialGroup Group = CFRadialGroup.Action;

    /// <summary>
    /// Used when <see cref="Group"/> is <see cref="CFRadialGroup.Spell"/>. 0 is a cantrip.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SpellRank;

    /// <summary>
    /// Actions this activity spends. 0 uses the default: 1 for an action, the spell's catalog cost for a spell.
    /// Free activities and reactions stay at 0 and spend nothing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Actions;
}

public enum CFRadialGroup : byte
{
    /// <summary>An activity that costs one, two, or three actions.</summary>
    Action,

    /// <summary>Not shown. Reactions resolve in code when their trigger happens.</summary>
    Reaction,

    Free,

    Spell,
}
