using Content.Server.Containers;
using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
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
        [ViewVariables]
        [DataField("owner", required: true)] private string _owner = string.Empty;

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

        public string Title => Loc.GetString("objective-condition-steal-title", ("owner", Loc.GetString(_owner)), ("itemName", Loc.GetString(PrototypeName)));

        public string Description => Loc.GetString("objective-condition-steal-description",("itemName", Loc.GetString(PrototypeName)));

        public SpriteSpecifier Icon => new SpriteSpecifier.EntityPrototype(_prototypeId);

        public float Progress
        {
            get
            {
                if (_mind?.OwnedEntity is not {Valid: true} owned) return 0f;
                if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<ContainerManagerComponent?>(owned, out var containerManagerComponent)) return 0f;

                // slightly ugly but fixing it would just be duplicating it with a different return value
                float count = containerManagerComponent.CountPrototypeOccurencesRecursive(_prototypeId);
                if (count >= 1)
                    return 1;
                else
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
