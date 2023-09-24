using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Ghost;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedGhostSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class GhostComponent : Component
{
    // I have no idea what this means I just wanted to kill comp references.
    [ViewVariables]
    public bool IsAttached;

    [DataField("toggleLightingAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleLightingAction = "ActionToggleLighting";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleLightingActionEntity;

    [DataField("toggleFovAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleFoVAction = "ActionToggleFov";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleFoVActionEntity;

    [DataField("toggleGhostsAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleGhostsAction = "ActionToggleGhosts";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleGhostsActionEntity;

    [ViewVariables(VVAccess.ReadWrite), DataField("timeOfDeath", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan TimeOfDeath = TimeSpan.Zero;

    [DataField("booRadius")]
    public float BooRadius = 3;

    [DataField("booMaxTargets")]
    public int BooMaxTargets = 3;

    [DataField]
    public EntProtoId BooAction = "ActionGhostBoo";

    [DataField, AutoNetworkedField]
    public EntityUid? BooActionEntity;

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
    [DataField("color"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color color = Color.White;

    [DataField("canReturnToBody"), AutoNetworkedField]
    private bool _canReturnToBody;
}

public sealed partial class BooActionEvent : InstantActionEvent { }

public sealed partial class ToggleFoVActionEvent : InstantActionEvent { };

public sealed partial class ToggleGhostsActionEvent : InstantActionEvent { };

public sealed partial class ToggleLightingActionEvent : InstantActionEvent { };
