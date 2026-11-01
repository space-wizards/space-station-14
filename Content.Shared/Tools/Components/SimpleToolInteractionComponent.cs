using Content.Shared.Interaction;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Tools.Components;

/// <summary>
/// This marker component causes <see cref="ToolInteractionEvent"/>s to be raised on this component's owner when
/// an entity with <see cref="ToolComponent"/> is used to interact with this component's owner.
/// </summary>
/// <seealso cref="SimpleToolUsageComponent"/>
[RegisterComponent, NetworkedComponent]
public sealed partial class SimpleToolInteractionComponent : Component;

/// <summary>
/// This event is raised on the entity with <see cref="SimpleToolInteractionComponent"/> when an entity with
/// <see cref="ToolComponent"/> is used on it.
/// </summary>
/// <param name="Tool">The entity with <see cref="ToolComponent"/>.</param>
/// <param name="User">The entity performing the interaction.</param>
/// <param name="ClickLocation">The location of the interaction click, copied from the originating <see cref="InteractUsingEvent"/>.</param>
[ByRefEvent]
public readonly record struct ToolInteractionEvent(
    Entity<ToolComponent> Tool,
    EntityUid User,
    EntityCoordinates ClickLocation
);
