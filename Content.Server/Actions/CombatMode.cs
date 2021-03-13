#nullable enable
using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class CombatMode : IToggleAction
    {
        [DataField("combatOn")] public string MessageOn { get; private set; } = "You enter combat mode!";
        [DataField("combatOff")] public string MessageOff { get; private set; } = "You exit combat mode.";

        public bool DoToggleAction(ToggleActionEventArgs args)
        {
            if (!args.Performer.TryGetComponent(out CombatModeComponent? combatMode))
            {
                return false;
            }

            args.Performer.PopupMessage(args.ToggledOn ? MessageOn : MessageOff);
            combatMode.IsInCombatMode = args.ToggledOn;

            return true;
        }
    }
}
