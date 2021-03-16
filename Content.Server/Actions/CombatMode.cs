#nullable enable
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Actions;
using Content.Shared.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions
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
