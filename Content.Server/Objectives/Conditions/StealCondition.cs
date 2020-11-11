using System;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Content.Shared.Objectives;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    public class StealCondition : IObjectiveCondition
    {
        public string PrototypeId { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.PrototypeId, "prototype", "");
        }

        public string GetTitle() => $"Steal prototype {PrototypeId}";

        public string GetDescription() => $"We need you to steal prototype {PrototypeId}. Dont get caught.";

        public SpriteSpecifier GetIcon()
        {
            return new PrototypeIcon(PrototypeId);
        }

        public float GetProgress(Mind mind)
        {
            if (mind.OwnedEntity == null) return 0f;
            if (!mind.OwnedEntity.TryGetComponent<ContainerManagerComponent>(out var containerManagerComponent)) return 0f;

            foreach (var container in containerManagerComponent.GetAllContainers())
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (entity.Prototype?.ID == PrototypeId) return 1f;
                }
            }

            return 0f;
        }

        public float GetDifficulty() => 1f;
    }
}
