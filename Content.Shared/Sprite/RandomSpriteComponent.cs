using Robust.Shared.GameStates;

namespace Content.Shared.Sprite;

[RegisterComponent, NetworkedComponent]
public sealed partial class RandomSpriteComponent : Component
{
    /// <summary>
    /// Whether or not all groups from <see cref="Available"/> are used,
    /// or if only one is picked at random.
    /// </summary>
    [DataField("getAllGroups")]
    public bool GetAllGroups;

    /// <summary>
    /// Available colors based on group, parsed layer enum, state, and color.
    /// Stored as a list so we can have groups of random sprites (e.g. tech_base + tech_flare for holoparasite)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("available")]
    public List<Dictionary<string, (string State, string? Color)>> Available = new();

    /// <summary>
    /// Selected colors
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("selected")]
    public Dictionary<string, (string State, Color? Color)> Selected = new();
}
