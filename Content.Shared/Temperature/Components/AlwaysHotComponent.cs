using Content.Shared.Temperature.Systems;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Makes the entity always set <c>IsHotEvent.IsHot</c> to true, no matter what.
/// </summary>
[RegisterComponent, Access(typeof(AlwaysHotSystem))]
public sealed partial class AlwaysHotComponent : Component
{
}
