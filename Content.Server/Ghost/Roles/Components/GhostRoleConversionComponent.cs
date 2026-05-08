using Content.Shared.Guidebook;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Ghost.Roles.Components;

/// <summary>
/// Added by conversion antagonist systems (zombies, etc.) to override the guidebook entry
/// shown in the ghost role popup, taking priority over <see cref="GhostRoleComponent.GuideEntry"/>.
/// Remove this component to restore the original guide entry. Future proofed for new conversion style antags.
/// </summary>
[RegisterComponent]
public sealed partial class GhostRoleConversionComponent : Component
{
    [DataField]
    public ProtoId<GuideEntryPrototype>? GuideEntry;
}
