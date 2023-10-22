using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// This component determines if something should be shown in the cookbook or not, and what category it should go in.
/// </summary>
[RegisterComponent]
public sealed partial class CookbookDocumentationComponent : Component
{
    /// <summary>
    /// What guides to include show when opening the guidebook. The first entry will be used to select the currently
    /// selected guidebook.
    /// </summary>
    [DataField("category")]
    [ViewVariables]
    public string Category = "Unknown";
}