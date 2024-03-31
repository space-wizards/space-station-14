using System.Threading;

namespace Content.Shared.LegallyDistinctSpaceFerret;

[RegisterComponent]
public sealed partial class BrainrotComponent : Component
{
    [DataField]
    public float Duration = 10.0f;

    public List<CancellationTokenSource> Cancels = [];
}
