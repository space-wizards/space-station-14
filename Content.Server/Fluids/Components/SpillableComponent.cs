using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Fluids.Components;

[RegisterComponent]
public class SpillableComponent : Component
{
    public override string Name => "Spillable";

    [DataField("solution")]
    public string SolutionName = "puddle";

    /// <summary>
    ///     Should this item be spilled when worn as clothing?
    ///     Doesn't count for pockets or hands.
    /// </summary>
    [DataField("spillWorn")]
    public bool SpillWorn = true;
}
