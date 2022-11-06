using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Research.Components
{
    [NetworkedComponent()]
    public abstract class SharedTechnologyDatabaseComponent : Component
    {
        [DataField("technologies", customTypeSerializer: typeof(PrototypeIdListSerializer<TechnologyPrototype>))]
        public readonly List<string> TechnologyIds = new();

        /// <summary>
        ///     Returns whether a technology is unlocked on this database or not.
        /// </summary>
        /// <param name="technology">The technology to be checked</param>
        /// <returns>Whether it is unlocked or not</returns>
        public bool IsTechnologyUnlocked(string technologyId)
        {
            return TechnologyIds.Contains(technologyId);
        }

        /// <summary>
        ///     Returns whether a technology can be unlocked on this database,
        ///     taking parent technologies into account.
        /// </summary>
        /// <param name="technology">The technology to be checked</param>
        /// <returns>Whether it could be unlocked or not</returns>
        public bool CanUnlockTechnology(TechnologyPrototype technology)
        {
            if (IsTechnologyUnlocked(technology.ID)) return false;
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            foreach (var technologyId in technology.RequiredTechnologies)
            {
                protoMan.TryIndex(technologyId, out TechnologyPrototype? requiredTechnology);
                if (requiredTechnology == null)
                    return false;

                if (!IsTechnologyUnlocked(requiredTechnology.ID))
                    return false;
            }
            return true;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TechnologyDatabaseState : ComponentState
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
