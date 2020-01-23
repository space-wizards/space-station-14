using System;
using System.Collections.Generic;
using Content.Server.Interfaces.Chat;
using Content.Shared.Jobs;
using Robust.Shared.IoC;

namespace Content.Server.Mobs.Roles
{
    public class Job : Role
    {
        public JobPrototype Prototype { get; }

        public override string Name { get; }

        public String StartingGear => Prototype.StartingGear;

        public Job(Mind mind, JobPrototype jobPrototype) : base(mind)
        {
            Prototype = jobPrototype;
            Name = jobPrototype.Name;
        }

        public override void Greet()
        {
            base.Greet();

            var chat = IoCManager.Resolve<IChatManager>();
            chat.DispatchServerMessage(
                Mind.Session,
                String.Format("You're a new {0}. Do your best!", Name));
        }
    }


}
