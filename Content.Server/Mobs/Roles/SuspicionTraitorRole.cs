using Content.Server.GameObjects;
using Content.Server.Interfaces.Chat;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.Mobs.Roles
{
    public sealed class SuspicionTraitorRole : Role
    {
        public SuspicionTraitorRole(Mind mind) : base(mind)
        {
        }

        public override string Name => "Traitor";
        public override bool Antag => true;

        public override void Greet()
        {
            base.Greet();

            var chat = IoCManager.Resolve<IChatManager>();
            chat.DispatchServerMessage(Mind.Session, "You're a traitor!");
        }
    }
}
