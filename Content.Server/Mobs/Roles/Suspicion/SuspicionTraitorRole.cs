using System.Collections.Generic;
using System.Linq;
using Content.Server.Interfaces.Chat;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Shared.Roles;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Localization;

namespace Content.Server.Mobs.Roles
{
    public sealed class SuspicionTraitorRole : SuspicionRole
    {
        public AntagPrototype Prototype { get; }

        public SuspicionTraitorRole(Mind mind, AntagPrototype antagPrototype) : base(mind)
        {
            Prototype = antagPrototype;
            Name = antagPrototype.Name;
            Antagonist = antagPrototype.Antagonist;
        }

        public override string Name { get; }
        public string Objective => Prototype.Objective;
        public override bool Antagonist { get; }

        public void GreetSuspicion(List<SuspicionTraitorRole> traitors, IChatManager chatMgr)
        {
            chatMgr.DispatchServerMessage(Mind.Session, Loc.GetString("You're a {0}!", Name));
            chatMgr.DispatchServerMessage(Mind.Session, Loc.GetString("Objective: {0}", Objective));

            if (traitors.Count == 1)
            {
                // Only traitor.
                chatMgr.DispatchServerMessage(Mind.Session, Loc.GetString("You're on your own. Good luck!"));
                return;
            }

            var text = string.Join(", ", traitors.Where(p => p != this).Select(p => p.Mind.CharacterName));

            var pluralText = Loc.GetPluralString("Your partner in crime is: {0}",
                "Your partners in crime are: {0}",
                traitors.Count-1, text);

            chatMgr.DispatchServerMessage(Mind.Session, pluralText);
        }
    }
}
