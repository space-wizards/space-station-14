using System.Collections.Generic;
using System.Linq;
using Content.Server.Interfaces.Chat;
using Content.Shared.Roles;
using Robust.Shared.Localization;

namespace Content.Server.Mobs.Roles.Suspicion
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
            chatMgr.DispatchServerMessage(Mind.Session, Loc.GetString("suspicion-role-greeting", ("roleName", Name)));
            chatMgr.DispatchServerMessage(Mind.Session, Loc.GetString("suspicion-objective", ("objectiveText", Objective)));

            var allPartners = string.Join(", ", traitors.Where(p => p != this).Select(p => p.Mind.CharacterName));

            var partnerText = Loc.GetString(
                "suspicion-partners-in-crime",
                ("partnerCount", traitors.Count-1),
                ("partnerNames", allPartners)
            );

            chatMgr.DispatchServerMessage(Mind.Session, partnerText);
        }
    }
}
