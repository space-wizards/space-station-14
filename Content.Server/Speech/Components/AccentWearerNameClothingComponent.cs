using Content.Server.Speech.EntitySystems;

namespace Content.Server.Speech.Components;

/// <summary>
/// Applies any accent components on this item to the name of the wearer while worn.
/// </summary>
[RegisterComponent]
[Access(typeof(AccentWearerNameClothingSystem))]
public sealed partial class AccentWearerNameClothingComponent : Component;
