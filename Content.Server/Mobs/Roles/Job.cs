using System;
using System.Collections.Generic;
using Content.Shared.Jobs;

namespace Content.Server.Mobs.Roles
{
    public class Job : Role
    {
        private readonly JobPrototype _jobPrototype;

        public Job(Mind mind, JobPrototype jobPrototype) : base(mind)
        {
            _jobPrototype = jobPrototype;
            Name = jobPrototype.Name;
        }

        public override string Name { get; }
    }
}
