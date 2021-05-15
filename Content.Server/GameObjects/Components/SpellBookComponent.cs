#nullable enable
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class SpellBookComponent : Component
    {
        [ViewVariables] [DataField("usemessage")] private readonly string _useMessage = "You're a wiznerd, Garry!";

        [ViewVariables] [DataField("spells")] private readonly List<ActionType>? _grantedSpells = new();

        public override string Name => "SpellBook";

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent<SharedActionsComponent>(out var actions)) return false;
            if (_grantedSpells == null) return false;
            foreach(var spell in _grantedSpells)
            {
                actions.Grant(spell);
            }
            eventArgs.User.PopupMessage(_useMessage);
            Owner.Delete();
            return true;
        }
    }
}
