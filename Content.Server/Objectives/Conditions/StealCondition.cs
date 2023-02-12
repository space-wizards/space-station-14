using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    // Oh god my eyes
    [UsedImplicitly]
    [DataDefinition]
    public sealed class StealCondition : IObjectiveCondition, ISerializationHooks
    {
        private Mind.Mind? _mind;
        [DataField("prototype")] private string _prototypeId = string.Empty;

        /// <summary>
        /// Help newer players by saying e.g. "steal the chief engineer's advanced magboots"
        /// instead of "steal advanced magboots. Should be a loc string.
        /// </summary>
        [DataField("owner")] private string? _owner = null;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new StealCondition
            {
                _mind = mind,
                _prototypeId = _prototypeId,
                _owner = _owner
            };
        }

        private string PrototypeName =>
            IoCManager.Resolve<IPrototypeManager>().TryIndex<EntityPrototype>(_prototypeId, out var prototype)
                ? prototype.Name
                : "[CANNOT FIND NAME]";

        public string Title =>
            _owner == null
                ? Loc.GetString("objective-condition-steal-title-no-owner", ("itemName", Loc.GetString(PrototypeName)))
                : Loc.GetString("objective-condition-steal-title", ("owner", Loc.GetString(_owner)), ("itemName", Loc.GetString(PrototypeName)));

        public string Description => Loc.GetString("objective-condition-steal-description",("itemName", Loc.GetString(PrototypeName)));

        public SpriteSpecifier Icon => new SpriteSpecifier.EntityPrototype(_prototypeId);

        public float Progress
        {
            get
            {
                var uid = _mind?.OwnedEntity;
                var entMan = IoCManager.Resolve<IEntityManager>();

                // TODO make this a container system function
                // or: just iterate through transform children, instead of containers?

                var metaQuery = entMan.GetEntityQuery<MetaDataComponent>();
                var managerQuery = entMan.GetEntityQuery<ContainerManagerComponent>();
                var stack = new Stack<ContainerManagerComponent>();

                if (!metaQuery.TryGetComponent(_mind?.OwnedEntity, out var meta))
                    return 0;

                if (meta.EntityPrototype?.ID == _prototypeId)
                    return 1;

                if (!managerQuery.TryGetComponent(uid, out var currentManager))
                    return 0;

                do
                {
                    foreach (var container in currentManager.Containers.Values)
                    {
                        foreach (var entity in container.ContainedEntities)
                        {
                            if (metaQuery.GetComponent(entity).EntityPrototype?.ID == _prototypeId)
                                return 1;
                            if (!managerQuery.TryGetComponent(entity, out var containerManager))
                                continue;
                            stack.Push(containerManager);
                        }
                    }
                } while (stack.TryPop(out currentManager));

                return 0;
            }
        }

        public float Difficulty => 2.25f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is StealCondition stealCondition &&
                   Equals(_mind, stealCondition._mind) &&
                   _prototypeId == stealCondition._prototypeId;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StealCondition) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_mind, _prototypeId);
        }
    }
}
