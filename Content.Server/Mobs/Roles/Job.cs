using Content.Server.Interfaces.Chat;
using Content.Shared.Roles;
using Robust.Shared.IoC;

namespace Content.Server.Mobs.Roles
{
    public class Job : Role
    {
        public JobPrototype Prototype { get; }

        public override string Name { get; }
        public override bool Antagonist => false;

        public string? StartingGear => Prototype.StartingGear;

        public Job(Mind mind, JobPrototype jobPrototype) : base(mind)
        {
            Prototype = jobPrototype;
            Name = jobPrototype.Name;
        }

        public override void Greet()
        {
            base.Greet();

            if (Mind.TryGetSession(out var session))
            {
                var chat = IoCManager.Resolve<IChatManager>();
                chat.DispatchServerMessage(session, $"You're a new {Name}. Do your best!");
            }
        }
    }
}
