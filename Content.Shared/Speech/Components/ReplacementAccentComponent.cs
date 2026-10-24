using Content.Shared.Speech.Prototypes;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Replaces full sentences or words within sentences with new strings.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ReplacementAccentSystem))]
public sealed partial class ReplacementAccentComponent : BaseAccentComponent
{
    [DataField(required: true)]
    public ProtoId<ReplacementAccentPrototype> Accent;
}
