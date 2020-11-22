#nullable enable
using Content.Server.GameObjects.Components.ContainerExt;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    public class StealCondition : IObjectiveCondition
    {
        public string PrototypeId { get; private set; } = default!;
        public int Amount { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.PrototypeId, "prototype", "");
            serializer.DataField(this, x => x.Amount, "amount", 1);

            if (Amount < 1)
            {
                Logger.Error("StealCondition has an amount less than 1 ({0})", Amount);
            }
        }

        private string PrototypeName =>
            IoCManager.Resolve<IPrototypeManager>().TryIndex<EntityPrototype>(PrototypeId, out var prototype)
                ? prototype.Name
                : "[CANNOT FIND NAME]";

        public string GetTitle() => Loc.GetString("Steal {0} {1}", Amount > 1 ? $"{Amount}x" : "", Loc.GetString(PrototypeName));

        public string GetDescription() => Loc.GetString("We need you to steal {0}. Don't get caught.", Loc.GetString(PrototypeName));

        public SpriteSpecifier GetIcon()
        {
            return new SpriteSpecifier.EntityPrototype(PrototypeId);
        }

        public float GetProgress(Mind? mind)
        {
            if (mind?.OwnedEntity == null) return 0f;
            if (!mind.OwnedEntity.TryGetComponent<ContainerManagerComponent>(out var containerManagerComponent)) return 0f;

            float count = containerManagerComponent.CountPrototypeOccurencesRecursive(PrototypeId);
            return count/Amount;
        }

        public float GetDifficulty() => 1f;
    }
}
