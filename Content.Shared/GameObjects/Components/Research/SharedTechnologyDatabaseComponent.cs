using System;
using System.Collections;
using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Research
{
    public class SharedTechnologyDatabaseComponent : Component, IEnumerable<TechnologyPrototype>
    {
        public override string Name => "TechnologyDatabase";
        public override uint? NetID => ContentNetIDs.TECHNOLOGY_DATABASE;
        public override Type StateType => typeof(TechnologyDatabaseState);

        protected List<TechnologyPrototype> _technologies = new List<TechnologyPrototype>();
        public IReadOnlyList<TechnologyPrototype> Technologies => _technologies;

        public IEnumerator<TechnologyPrototype> GetEnumerator()
        {
            return Technologies.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public List<string> GetTechnologyIdList()
        {
            List<string> techIds = new List<string>();

            foreach (var tech in _technologies)
            {
                techIds.Add(tech.ID);
            }

            return techIds;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (serializer.Reading)
            {
                var techs = serializer.ReadDataField("technologies", new List<string>());
                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                foreach (var id in techs)
                {
                    if (!prototypeManager.TryIndex(id, out TechnologyPrototype tech)) continue;
                    _technologies.Add(tech);
                }
            } else if (serializer.Writing)
            {
                var techs = GetTechnologyIdList();
                serializer.DataField(ref techs, "technologies", new List<string>());
            }
        }
    }

    [Serializable, NetSerializable]
    public class TechnologyDatabaseState : ComponentState
    {
        public List<string> Technologies;
        public TechnologyDatabaseState(List<string> technologies) : base(ContentNetIDs.TECHNOLOGY_DATABASE)
        {
            technologies = technologies;
        }

        public TechnologyDatabaseState(List<TechnologyPrototype> technologies) : base(ContentNetIDs.TECHNOLOGY_DATABASE)
        {
            Technologies = new List<string>();
            foreach (var technology in technologies)
            {
                Technologies.Add(technology.ID);
            }
        }
    }
}
