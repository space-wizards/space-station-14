using Content.Server.Interfaces.Chat;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Mobs.Roles.Traitor
{
    public class TraitorRole : Role
    {
        public TraitorRole(Mind mind) : base(mind)
        {
        }

        public override string Name => Loc.GetString("traitor-role-name");
        public override bool Antagonist => true;

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
