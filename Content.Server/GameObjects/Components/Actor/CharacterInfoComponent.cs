#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs;
using Content.Shared.GameObjects.Components.Actor;
using Content.Shared.Objectives;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Actor
{
    [RegisterComponent]
    public class CharacterInfoComponent : SharedCharacterInfoComponent
    {
        public override void OnAdd()
        {
            Owner.EntityManager.EventBus.SubscribeEvent<MindComponent.ObjectivesChangedMessage>(EventSource.Local, this, ObjectivesChanged);
        }

        public override void OnRemove()
        {
            Owner.EntityManager.EventBus.UnsubscribeEvent<MindComponent.ObjectivesChangedMessage>(EventSource.Local, this);
        }

        private void ObjectivesChanged(MindComponent.ObjectivesChangedMessage ev)
        {
            SendCharacterInfoMessage();
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            switch (message)
            {
                case RequestCharacterInfoMessage msg:
                    SendCharacterInfoMessage();
                    break;
            }
        }

        public void SendCharacterInfoMessage()
        {
            var conditions = new Dictionary<string, List<ConditionInfo>>();
            var jobTitle = "Professional Greyshirt";
            if (Owner.TryGetComponent(out MindComponent? mindComponent))
            {
                if (mindComponent.Mind?.AllObjectives != null)
                {
                    foreach (var objective in mindComponent.Mind?.AllObjectives!)
                    {
                        if (!conditions.ContainsKey(objective.Prototype.Issuer))
                            conditions[objective.Prototype.Issuer] = new List<ConditionInfo>();
                        foreach (var condition in objective.Prototype.Conditions)
                        {
                            conditions[objective.Prototype.Issuer].Add(new ConditionInfo(condition.GetTitle(),
                                condition.GetDescription(), condition.GetIcon()));
                        }
                    }
                }
            }
            SendNetworkMessage(new CharacterInfoMessage(jobTitle, conditions));
        }
    }
}
