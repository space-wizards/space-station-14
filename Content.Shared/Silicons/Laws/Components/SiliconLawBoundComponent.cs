using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for entities which are bound to silicon laws and can view them.
/// </summary>
[RegisterComponent, Access(typeof(SharedSiliconLawSystem))]
public sealed class SiliconLawBoundComponent : Component
{
    /// <summary>
    /// The sidebar action that toggles the laws screen.
    /// </summary>
    [DataField("viewLawsAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ViewLawsAction = "ViewLaws";

    /// <summary>
    /// The action for toggling laws. Stored here so we can remove it later.
    /// </summary>
    [DataField("providedAction")]
    public InstantAction? ProvidedAction;

    /// <summary>
    /// The last entity that provided laws to this entity.
    /// </summary>
    [DataField("lastLawProvider")]
    public EntityUid? LastLawProvider;
}

/// <summary>
/// Event raised to get the laws that a law-bound entity has.
///
/// Is first raised on the entity itself, then on the
/// entity's station, then on the entity's grid,
/// before being broadcast.
/// </summary>
/// <param name="Entity"></param>
[ByRefEvent]
public record struct GetSiliconLawsEvent(EntityUid Entity)
{
    public EntityUid Entity = Entity;

    public readonly List<SiliconLaw> Laws = new();

    public bool Handled = false;
}

public sealed class ToggleLawsScreenEvent : InstantActionEvent
{

}

[NetSerializable, Serializable]
public enum SiliconLawsUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SiliconLawBuiState : BoundUserInterfaceState
{
    public List<SiliconLaw> Laws;

    public SiliconLawBuiState(List<SiliconLaw> laws)
    {
        Laws = laws;
    }
}
