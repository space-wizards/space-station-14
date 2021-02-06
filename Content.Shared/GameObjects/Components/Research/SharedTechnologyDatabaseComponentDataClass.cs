using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Research
{
    public partial class SharedTechnologyDatabaseComponentDataClass
    {
        [DataClassTarget("technologies")]
        protected List<TechnologyPrototype> _technologies = new();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "technologies",
                new List<string>(),
                techs =>
                {
                    var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

                    foreach (var id in techs)
                    {
                        if (prototypeManager.TryIndex(id, out TechnologyPrototype tech))
                        {
                            _technologies.Add(tech);
                        }
                    }
                },
                () =>
                {
                    var techIds = new List<string>();

                    foreach (var tech in _technologies)
                    {
                        techIds.Add(tech.ID);
                    }

                    return techIds;
                });
        }
    }
}
