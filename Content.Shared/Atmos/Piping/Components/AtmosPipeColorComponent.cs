using Content.Shared.Atmos.Piping.EntitySystems;
using JetBrains.Annotations;

namespace Content.Shared.Atmos.Piping.Components;

/// <summary>
/// Component for the color of a pipe.
/// </summary>
[RegisterComponent]
public sealed partial class AtmosPipeColorComponent : Component
{
    /// <summary>
    /// The color of the pipe.
    /// </summary>
    [DataField]
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// The color of the pipe.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), UsedImplicitly]
    public Color ColorVV
    {
        get => Color;
        set => IoCManager.Resolve<IEntityManager>().System<AtmosPipeColorSystem>().SetColor(Owner, this, value);
    }
}

/// <summary>
/// Event for when the color of a pipe changes.
/// </summary>
[ByRefEvent]
public record struct AtmosPipeColorChangedEvent(Color Color)
{
    public Color Color = Color;
}
