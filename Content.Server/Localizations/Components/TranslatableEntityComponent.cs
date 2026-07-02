namespace Content.Server.Localizations.Components;

/// <summary>
///     Marks entity as translatable.
///     Name and description of the translated entity will be changed to their LocIds on mapinit.
/// </summary>
[RegisterComponent]
public sealed partial class TranslatableEntityComponent : Component;
