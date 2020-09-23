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

        public string StartingGear => Prototype.StartingGear;

        public Job(Mind mind, JobPrototype jobPrototype) : base(mind)
        {
            Prototype = jobPrototype;
            Name = jobPrototype.Name;
        }

        public override void Greet()
        {
            base.Greet();

            var chat = IoCManager.Resolve<IChatManager>();
            chat.DispatchServerMessage(Mind.Session, $"You are the {Name}.");
            if(!string.IsNullOrEmpty(Prototype.Supervisors))
                chat.DispatchServerMessage(Mind.Session, $"As the {Name}, you answer directly to {Prototype.Supervisors}. Special circumstances may change this.");

            // TODO: Uncomment this when we have adminhelp.
            //if(Prototype.RequireAdminNotify)
            //    chat.DispatchServerMessage(Mind.Session, "You are playing a job that is important for Game Progression. If you have to disconnect, please notify the admins via adminhelp.");
        }
    }


}
