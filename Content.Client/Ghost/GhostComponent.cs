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

        public InstantAction DisableLightingAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/VerbIcons/light.svg.192dpi.png")),
            DisplayName = "ghost-gui-toggle-lighting-manager-name",
            Description = "ghost-gui-toggle-lighting-manager-desc",
            ClientExclusive = true,
            CheckCanInteract = false,
            Event = new DisableLightingActionEvent(),
        };

        public InstantAction ToggleGhostsAction = new()
        {
            Icon = new SpriteSpecifier.Rsi(new ResourcePath("Mobs/Ghosts/ghost_human.rsi"), "icon"),
            DisplayName = "ghost-gui-toggle-ghost-visibility-name",
            Description = "ghost-gui-toggle-ghost-visibility-desc",
            ClientExclusive = true,
            CheckCanInteract = false,
            Event = new ToggleGhostsActionEvent(),
        };
    }

    public sealed class DisableLightingActionEvent : InstantActionEvent { };

    public sealed class ToggleGhostsActionEvent : InstantActionEvent { };
}
