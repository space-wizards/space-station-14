using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.Ghost;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedGhostSystem))]
[AutoGenerateComponentState]
public sealed partial class GhostComponent : Component
{
    // I have no idea what this means I just wanted to kill comp references.
    [ViewVariables]
    public bool IsAttached;

    public InstantAction ToggleLightingAction = new()
    {
        Icon = new SpriteSpecifier.Texture(new ("Interface/VerbIcons/light.svg.192dpi.png")),
        DisplayName = "ghost-gui-toggle-lighting-manager-name",
        Description = "ghost-gui-toggle-lighting-manager-desc",
        ClientExclusive = true,
        CheckCanInteract = false,
        Event = new ToggleLightingActionEvent(),
    };

    public InstantAction ToggleFoVAction = new()
    {
        Icon = new SpriteSpecifier.Texture(new ("Interface/VerbIcons/vv.svg.192dpi.png")),
        DisplayName = "ghost-gui-toggle-fov-name",
        Description = "ghost-gui-toggle-fov-desc",
        ClientExclusive = true,
        CheckCanInteract = false,
        Event = new ToggleFoVActionEvent(),
    };

    public InstantAction ToggleGhostsAction = new()
    {
        Icon = new SpriteSpecifier.Rsi(new ("Mobs/Ghosts/ghost_human.rsi"), "icon"),
        DisplayName = "ghost-gui-toggle-ghost-visibility-name",
        Description = "ghost-gui-toggle-ghost-visibility-desc",
        ClientExclusive = true,
        CheckCanInteract = false,
        Event = new ToggleGhostsActionEvent(),
    };

    [ViewVariables(VVAccess.ReadWrite), DataField("timeOfDeath", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan TimeOfDeath = TimeSpan.Zero;

    [DataField("booRadius")]
    public float BooRadius = 3;

    [DataField("booMaxTargets")]
    public int BooMaxTargets = 3;

    [DataField("action")]
    public InstantAction Action = new()
    {
        UseDelay = TimeSpan.FromSeconds(120),
        Icon = new SpriteSpecifier.Texture(new ("Interface/Actions/scream.png")),
        DisplayName = "action-name-boo",
        Description = "action-description-boo",
        CheckCanInteract = false,
        Event = new BooActionEvent(),
    };

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

public sealed partial class BooActionEvent : InstantActionEvent { }

public sealed partial class ToggleFoVActionEvent : InstantActionEvent { };

public sealed partial class ToggleGhostsActionEvent : InstantActionEvent { };

public sealed partial class ToggleLightingActionEvent : InstantActionEvent { };
