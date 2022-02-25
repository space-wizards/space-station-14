using Content.Server.CombatMode;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class CombatMode : IToggleAction
    {
        public bool DoToggleAction(ToggleActionEventArgs args)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(args.Performer, out CombatModeComponent? combatMode))
            {
                return false;
            }

            args.Performer.PopupMessage(Loc.GetString(args.ToggledOn ? "hud-combat-enabled" : "hud-combat-disabled"));
            combatMode.IsInCombatMode = args.ToggledOn;

            return true;
        }
    }
}
