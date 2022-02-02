using System;
using System.Collections;
using System.Collections.Generic;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Research.Components
{
    [NetworkedComponent()]
    public class SharedTechnologyDatabaseComponent : Component, IEnumerable<TechnologyPrototype>, ISerializationHooks
    {
        [DataField("technologies")] private List<string> _technologyIds = new();

        protected List<TechnologyPrototype> _technologies = new();

        /// <summary>
        ///     A read-only list of unlocked technologies.
        /// </summary>
        public IReadOnlyList<TechnologyPrototype> Technologies => _technologies;

        void ISerializationHooks.BeforeSerialization()
        {
            var techIds = new List<string>();

            foreach (var tech in _technologies)
            {
                techIds.Add(tech.ID);
            }

            _technologyIds = techIds;
        }

        void ISerializationHooks.AfterDeserialization()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var id in _technologyIds)
            {
                if (prototypeManager.TryIndex(id, out TechnologyPrototype? tech))
                {
                    _technologies.Add(tech);
                }
            }
        }

        public IEnumerator<TechnologyPrototype> GetEnumerator()
        {
            return Technologies.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Returns a list with the IDs of all unlocked technologies.
        /// </summary>
        /// <returns>A list of technology IDs</returns>
        public List<string> GetTechnologyIdList()
        {
            List<string> techIds = new List<string>();

            foreach (var tech in _technologies)
            {
                techIds.Add(tech.ID);
            }

            return techIds;
        }

        /// <summary>
        ///     Returns whether a technology is unlocked on this database or not.
        /// </summary>
        /// <param name="technology">The technology to be checked</param>
        /// <returns>Whether it is unlocked or not</returns>
        public bool IsTechnologyUnlocked(TechnologyPrototype technology)
        {
            return _technologies.Contains(technology);
        }

        /// <summary>
        ///     Returns whether a technology can be unlocked on this database,
        ///     taking parent technologies into account.
        /// </summary>
        /// <param name="technology">The technology to be checked</param>
        /// <returns>Whether it could be unlocked or not</returns>
        public bool CanUnlockTechnology(TechnologyPrototype technology)
        {
            if (technology == null || IsTechnologyUnlocked(technology)) return false;
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            foreach (var technologyId in technology.RequiredTechnologies)
            {
                protoMan.TryIndex(technologyId, out TechnologyPrototype? requiredTechnology);
                if (requiredTechnology == null)
                    return false;

                if (!IsTechnologyUnlocked(requiredTechnology))
                    return false;
            }
            return true;
        }
    }

    [Serializable, NetSerializable]
    public class TechnologyDatabaseState : ComponentState
    {
        public List<string> Technologies;
        public TechnologyDatabaseState(List<string> technologies)
        {
            Technologies = technologies;
        }

        public TechnologyDatabaseState(List<TechnologyPrototype> technologies)
        {
            Technologies = new List<string>();
            foreach (var technology in technologies)
            {
                Technologies.Add(technology.ID);
            }
        }
    }
}
