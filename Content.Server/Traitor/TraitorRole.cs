using Content.Server.Chat.Managers;
using Content.Server.Roles;
using Content.Shared.Roles;

namespace Content.Server.Traitor
{
    public sealed class TraitorRole : Role
    {
        public AntagPrototype Prototype { get; }

        public TraitorRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind)
        {
            Prototype = antagPrototype;
            Name = Loc.GetString(antagPrototype.Name);
            Antagonist = antagPrototype.Antagonist;
        }

        public override string Name { get; }
        public override bool Antagonist { get; }

        public void GreetTraitor(string[] codewords)
        {
            if (Mind.TryGetSession(out var session))
            {
                var chatMgr = IoCManager.Resolve<IChatManager>();
                chatMgr.DispatchServerMessage(session, Loc.GetString("traitor-role-greeting"));
                chatMgr.DispatchServerMessage(session, Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ",codewords))));
            }
        }
    }
}
