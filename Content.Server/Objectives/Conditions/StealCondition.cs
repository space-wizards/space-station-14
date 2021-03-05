#nullable enable
using System;
using Content.Server.GameObjects;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class StealCondition : IObjectiveCondition, ISerializationHooks
    {
        private Mind? _mind;
        [DataField("prototype")] private string _prototypeId = string.Empty;
        [DataField("amount")] private int _amount = 1;

        public IObjectiveCondition GetAssigned(Mind mind)
        {
            return new StealCondition
            {
                _mind = mind,
                _prototypeId = _prototypeId,
                _amount = _amount
            };
        }

        void ISerializationHooks.AfterDeserialization()
        {
            if (_amount < 1)
            {
                Logger.Error("StealCondition has an amount less than 1 ({0})", _amount);
            }
        }

        private string PrototypeName =>
            IoCManager.Resolve<IPrototypeManager>().TryIndex<EntityPrototype>(_prototypeId, out var prototype)
                ? prototype.Name
                : "[CANNOT FIND NAME]";

        public string Title => Loc.GetString("Steal {0}{1}", _amount > 1 ? $"{_amount}x " : "", Loc.GetString(PrototypeName));

        public string Description => Loc.GetString("We need you to steal {0}. Don't get caught.", Loc.GetString(PrototypeName));

        public SpriteSpecifier Icon => new SpriteSpecifier.EntityPrototype(_prototypeId);

        public float Progress
        {
            get
            {
                if (_mind?.OwnedEntity == null) return 0f;
                if (!_mind.OwnedEntity.TryGetComponent<ContainerManagerComponent>(out var containerManagerComponent)) return 0f;

                float count = containerManagerComponent.CountPrototypeOccurencesRecursive(_prototypeId);
                return count/_amount;
            }
        }

        public float Difficulty => 1f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is StealCondition stealCondition &&
                   Equals(_mind, stealCondition._mind) &&
                   _prototypeId == stealCondition._prototypeId && _amount == stealCondition._amount;
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
            return HashCode.Combine(_mind, _prototypeId, _amount);
        }
    }
}
