namespace Content.Shared.Administration.Components;

/// <summary>
/// This component does nothing. It exists for admin and testing purposes.
/// </summary>
/// <remarks>
/// As an example, this component can be added to an entity and then used as
/// the target component for a pinpointer.
/// </remarks>
[RegisterComponent]
public sealed partial class MarkerOneComponent : Component;

/// <inheritdoc cref="MarkerOneComponent"/>
[RegisterComponent]
public sealed partial class MarkerTwoComponent : Component;

/// <inheritdoc cref="MarkerOneComponent"/>
[RegisterComponent]
public sealed partial class MarkerThreeComponent : Component;
