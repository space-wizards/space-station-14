using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.Polymorph.Components;

/// <summary>
/// Intended for use with the trigger system.
/// Polymorphs the user of the trigger.
/// </summary>
[RegisterComponent]
public sealed partial class PolymorphOnTriggerComponent : Component
{
    /// <summary>
    /// Polymorph settings.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;
}
