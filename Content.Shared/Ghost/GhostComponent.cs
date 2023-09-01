using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Ghost;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedGhostSystem))]
[AutoGenerateComponentState]
public sealed partial class GhostComponent : Component
{
    // I have no idea what this means I just wanted to kill comp references.
    [ViewVariables]
    public bool IsAttached;

    #region Ghost Actions

    [DataField("toggleLightingAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ToggleLightingAction = "GhostToggleLighting";

    [DataField("toggleFoVAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ToggleFoVAction = "GhostToggleFoV";

    [DataField("toggleGhostsAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ToggleGhostsAction = "GhostToggleGhostVisibility";

    [DataField("toggleGhostHearingAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ToggleGhostHearingAction = "GhostToggleHearing";

    [DataField("booAction", customTypeSerializer:typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string BooAction = "GhostBoo";

    [DataField("booRadius")]
    public float BooRadius = 3;

    [DataField("booMaxTargets")]
    public int BooMaxTargets = 3;

    #endregion

    /// <summary>
    ///     If true, ChatSystem will send all messages to this entity.
    /// </summary>
    [DataField("canGhostHear"), AutoNetworkedField]
    public bool CanGhostHear = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("timeOfDeath", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan TimeOfDeath = TimeSpan.Zero;

    // TODO: instead of this funny stuff just give it access and update in system dirtying when needed
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanGhostInteract
    {
        get => _canGhostInteract;
        set
        {
            if (_canGhostInteract == value) return;
            _canGhostInteract = value;
            Dirty();
        }
    }

    [DataField("canInteract"), AutoNetworkedField]
    private bool _canGhostInteract;

    /// <summary>
    ///     Changed by <see cref="SharedGhostSystem.SetCanReturnToBody"/>
    /// </summary>
    // TODO MIRROR change this to use friend classes when thats merged
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanReturnToBody
    {
        get => _canReturnToBody;
        set
        {
            if (_canReturnToBody == value) return;
            _canReturnToBody = value;
            Dirty();
        }
    }

    /// <summary>
    /// Ghost color
    /// </summary>
    /// <remarks>Used to allow admins to change ghost colors. Should be removed if the capability to edit existing sprite colors is ever added back.</remarks>
    [DataField("color"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Color color = Color.White;

    [DataField("canReturnToBody"), AutoNetworkedField]
    private bool _canReturnToBody;
}

public sealed partial class ToggleFoVActionEvent : InstantActionEvent { }

public sealed partial class ToggleGhostsActionEvent : InstantActionEvent { }

public sealed partial class ToggleLightingActionEvent : InstantActionEvent { }

public sealed partial class ToggleGhostHearingActionEvent : InstantActionEvent { }

public sealed partial class BooActionEvent : InstantActionEvent { }
