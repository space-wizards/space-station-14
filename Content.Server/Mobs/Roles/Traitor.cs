using Content.Server.Interfaces.Chat;
using SS14.Shared.Console;
using SS14.Shared.IoC;

namespace Content.Server.Mobs.Roles
{
    public sealed class Traitor : Role
    {
        public Traitor(Mind mind) : base(mind)
        {
        }

        public override string Name => "Traitor";

        public override void Greet()
        {
            base.Greet();

            var chat = IoCManager.Resolve<IChatManager>();
            chat.DispatchServerMessage(
                Mind.Session,
                "You're a traitor. Go fuck something up. Or something. I don't care to be honest.");
        }
    }
}
