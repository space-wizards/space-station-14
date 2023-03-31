using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Ghost;
using Robust.Shared.Utility;

namespace Content.Client.Ghost
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedGhostComponent))]
    public sealed class GhostComponent : SharedGhostComponent
    {
        public bool IsAttached { get; set; }

        public InstantAction ToggleLightingAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/VerbIcons/light.svg.192dpi.png")),
            DisplayName = "ghost-gui-toggle-lighting-manager-name",
            Description = "ghost-gui-toggle-lighting-manager-desc",
            UserPopup = "ghost-gui-toggle-lighting-manager-popup",
            ClientExclusive = true,
            CheckCanInteract = false,
            Event = new ToggleLightingActionEvent(),
        };

        public InstantAction ToggleFoVAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/VerbIcons/vv.svg.192dpi.png")),
            DisplayName = "ghost-gui-toggle-fov-name",
            Description = "ghost-gui-toggle-fov-desc",
            UserPopup = "ghost-gui-toggle-fov-popup",
            ClientExclusive = true,
            CheckCanInteract = false,
            Event = new ToggleFoVActionEvent(),
        };

        public InstantAction ToggleGhostsAction = new()
        {
            Icon = new SpriteSpecifier.Rsi(new ResourcePath("Mobs/Ghosts/ghost_human.rsi"), "icon"),
            DisplayName = "ghost-gui-toggle-ghost-visibility-name",
            Description = "ghost-gui-toggle-ghost-visibility-desc",
            UserPopup = "ghost-gui-toggle-ghost-visibility-popup",
            ClientExclusive = true,
            CheckCanInteract = false,
            Event = new ToggleGhostsActionEvent(),
        };
    }

    public sealed class ToggleLightingActionEvent : InstantActionEvent { };

    public sealed class ToggleFoVActionEvent : InstantActionEvent { };

    public sealed class ToggleGhostsActionEvent : InstantActionEvent { };
}
