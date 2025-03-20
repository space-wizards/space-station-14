using Robust.Shared.Prototypes;

namespace Content.Shared.Farming;

[RegisterComponent]
public sealed partial class CompostableFieldComponent : Component
{
    /// <summary>
    /// Time taken to apply the compost
    /// </summary>
    [DataField]
    public float CompostTime = 5.0f;
}