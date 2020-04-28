using Content.Server.GameObjects;
using Content.Server.Interfaces.Chat;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.Mobs.Roles
{
    public class SuspicionInnocentRole : Role
    {
        public SuspicionInnocentRole(Mind mind) : base(mind)
        {
        }

        public override string Name => "Innocent";
        public override bool Antag => false;

        public override void Greet()
        {
            base.Greet();

            var chat = IoCManager.Resolve<IChatManager>();
            chat.DispatchServerMessage(Mind.Session, "You're an innocent!");
        }
    }
}
