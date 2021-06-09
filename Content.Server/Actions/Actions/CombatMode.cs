#nullable enable
using Content.Server.CombatMode;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Notification;
using JetBrains.Annotations;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class CombatMode : IToggleAction
    {
        public bool DoToggleAction(ToggleActionEventArgs args)
        {
            if (!args.Performer.TryGetComponent(out CombatModeComponent? combatMode))
            {
                return false;
            }

            args.Performer.PopupMessage(args.ToggledOn ? Loc.GetString("hud-combat-enabled") : Loc.GetString("hud-combat-disabled"));
            combatMode.IsInCombatMode = args.ToggledOn;

            return true;
        }
    }
}
