using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.LieDown;

/// <summary>
///     Makes the target to lie down.
/// </summary>
[Access(typeof(SharedLieDownSystem))]
[NetworkedComponent, RegisterComponent]
public sealed class LyingDownComponent : Component
{
    /// <summary>
    ///     The action to lie down or stand up.
    /// </summary>
    [DataField("make-to-stand-up-action", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string? MakeToStandUpAction = "action-name-make-standup";
}

[Serializable, NetSerializable]
public sealed class ChangeStandingStateEvent : EntityEventArgs {}
