// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.GhostRoleCast
{
    [RegisterComponent]
    public sealed partial class GhostRoleCastComponent : Component
    {
        public string GhostRoleName = "";
        public string GhostRoleDesc = "";
        public string GhostRoleRule = "";

        public InstantAction ToggleGhostRoleNameAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new("SS220/Interface/actions/GhostRoleSettings.png")),
            DisplayName = "action-toggle-ghostrole-cast-settings-name",
            Description = "action-toggle-ghostrole-cast-settings-desc",
            ClientExclusive = true,
            CheckCanInteract = false,
            Priority = -7,
            Event = new ToggleGhostRoleCastSettingsEvent(),
        };

        public EntityTargetAction ToggleGhostRoleCastAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new("SS220/Interface/actions/GhostRoleCast.png")),
            DisplayName = "action-toggle-ghostrole-cast-name",
            Description = "action-toggle-ghostrole-cast-desc",
            ClientExclusive = true,
            CheckCanInteract = false,
            Priority = -8,
            Repeat = true,
            DeselectOnMiss = false,
            //CanTargetSelf = false,
            Event = new ToggleGhostRoleCastActionEvent(),
        };

        public EntityTargetAction ToggleGhostRoleRemoveAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new("SS220/Interface/actions/GhostRoleRemove.png")),
            DisplayName = "action-toggle-ghostrole-remove-name",
            Description = "action-toggle-ghostrole-remove-desc",
            ClientExclusive = true,
            CheckCanInteract = false,
            Priority = -9,
            Repeat = true,
            DeselectOnMiss = false,
            //CanTargetSelf = false,
            Event = new ToggleGhostRoleRemoveActionEvent(),
        };
    }


    public sealed partial class ToggleGhostRoleCastSettingsEvent : InstantActionEvent { };
    public sealed partial class ToggleGhostRoleCastActionEvent : EntityTargetActionEvent { };
    public sealed partial class ToggleGhostRoleRemoveActionEvent : EntityTargetActionEvent { };

}
