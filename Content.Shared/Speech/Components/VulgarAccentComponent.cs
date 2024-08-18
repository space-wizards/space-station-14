using Robust.Shared.GameStates;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VulgarAccentComponent : Component
{
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> Pack = "SwearWords";

    [DataField]
    public float SwearProb = 0.5f;
}
