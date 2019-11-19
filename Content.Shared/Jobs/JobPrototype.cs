using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Jobs
{
    [Prototype("job")]
    public class JobPrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; private set; }
        public string Name { get; private set; }

        public string StartingGear { get; private set; }
        public IEnumerable<string> Department { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").AsString();
            Name = mapping.GetNode("name").ToString();
            StartingGear = mapping.GetNode("startingGear").ToString();
            Department = mapping.GetNode("department").AllNodes.Select(i => i.ToString());
        }
    }
}
