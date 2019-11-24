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

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (ReferenceEquals(null, o)) return false;
            if (ReferenceEquals(this, o)) return true;
            if (o.GetType() != this.GetType()) return false;
            return Equals((Traitor) o);
        }

        public bool Equals(Traitor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name;
        }
    }
}
