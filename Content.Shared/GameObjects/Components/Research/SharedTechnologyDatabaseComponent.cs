using System;
using System.Collections;
using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
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
