#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs.Roles;
using Content.Shared.GameObjects.Components.Actor;
using Content.Shared.Objectives;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Actor
{
    [RegisterComponent]
    public class CharacterInfoComponent : SharedCharacterInfoComponent
    {
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            if(session?.AttachedEntity != Owner) return;

            switch (message)
            {
                case RequestCharacterInfoMessage _:
                    var conditions = new Dictionary<string, List<ConditionInfo>>();
                    var jobTitle = "No Profession";
                    if (Owner.TryGetComponent(out MindComponent? mindComponent))
                    {
                        var mind = mindComponent.Mind;

                        if (mind != null)
                        {
                            // getting conditions
                            foreach (var objective in mind.AllObjectives)
                            {
                                if (!conditions.ContainsKey(objective.Prototype.Issuer))
                                    conditions[objective.Prototype.Issuer] = new List<ConditionInfo>();
                                foreach (var condition in objective.Conditions)
                                {
                                    conditions[objective.Prototype.Issuer].Add(new ConditionInfo(condition.Title,
                                        condition.Description, condition.Icon, condition.Progress));
                                }
                            }

                            // getting jobtitle
                            foreach (var role in mind.AllRoles)
                            {
                                if (role.GetType() == typeof(Job))
                                {
                                    jobTitle = role.Name;
                                    break;
                                }
                            }
                        }
                    }
                    SendNetworkMessage(new CharacterInfoMessage(jobTitle, conditions));
                    break;
            }
        }
    }
}
