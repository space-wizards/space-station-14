using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Research
{
    public partial class SharedTechnologyDatabaseComponentDataClass : ISerializationHooks
    {
        [DataField("technologies")]
        private List<string> _technologyIds;

        [DataClassTarget("technologies")]
        protected readonly List<TechnologyPrototype> _technologies = new();

        public void BeforeSerialization()
        {
            var techIds = new List<string>();

            foreach (var tech in _technologies)
            {
                techIds.Add(tech.ID);
            }

            _technologyIds = techIds;
        }

        public void AfterDeserialization()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var id in _technologyIds)
            {
                if (prototypeManager.TryIndex(id, out TechnologyPrototype tech))
                {
                    _technologies.Add(tech);
                }
            }
        }
    }
}
