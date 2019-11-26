using System;
using System.Collections.Generic;
using Content.Server.Interfaces.Chat;
using Content.Shared.Jobs;
using Robust.Shared.IoC;

namespace Content.Server.Mobs.Roles
{
    public class Job : Role
    {
        private readonly JobPrototype _jobPrototype;

        public override string Name { get; }

        public String StartingGear => _jobPrototype.StartingGear;

        public Job(Mind mind, JobPrototype jobPrototype) : base(mind)
        {
            _jobPrototype = jobPrototype;
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

        public override bool Equals(object o)
        {
            if (ReferenceEquals(null, o)) return false;
            if (ReferenceEquals(this, o)) return true;
            if (o.GetType() != this.GetType()) return false;
            return Equals((Job) o);
        }

        public bool Equals(Job other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name;
        }
    }


}
