using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Light.Component;

/// <summary>
/// Simple light toggle on use + with action.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedToggleLightSystem))]
public sealed partial class ToggleLightComponent : Robust.Shared.GameObjects.Component
{
    [DataField("toggleActionId", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ToggleActionId = "ToggleLight";

    [DataField("toggleAction")]
    public InstantAction? ToggleAction;
}
