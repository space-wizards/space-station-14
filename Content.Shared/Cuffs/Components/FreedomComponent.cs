using Content.Shared.Actions;
using Content.Shared.Cuffs;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cuffs.Components;

/// <summary>
/// Grants the user an action to break out of restraints.
/// Like a freedom implant but infinite and not an implant.
/// </summary>
[RegisterComponent]
public sealed partial class FreedomComponent : Component
{
    /// <summary>
    /// Action to give to the user.
    /// It must use <see cref="BreakFreeEvent"/> to work.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Action = string.Empty;

    [DataField]
    public EntityUid? ActionEntity;
}

public sealed partial class BreakFreeEvent : InstantActionEvent
{
}
