using Robust.Shared.GameObjects;

namespace Content.Server.Mining.Components;

/// <summary>
/// A magical placeholder for the real ORM. Just click it with an ore and it'll spit out sheets in the correct quantity.
/// </summary>
[RegisterComponent]
public class ReprocessorComponent : Component
{
    public override string Name => "Reprocessor";
}
