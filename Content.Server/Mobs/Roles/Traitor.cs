using Content.Server.Interfaces.Chat;
using Robust.Shared.IoC;

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

        public override bool Equals(Role role)
        {
            if (ReferenceEquals(null, role)) return false;
            if (ReferenceEquals(this, role)) return true;
            if (role.GetType() != this.GetType()) return false;
            return Equals((Traitor) role);
        }

        public bool Equals(Traitor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name;
        }
    }
}
