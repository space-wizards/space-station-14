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
            switch (message)
            {
                case RequestCharacterInfoMessage _:
                    var conditions = new Dictionary<string, List<ConditionInfo>>();
                    var jobTitle = "No Profession";
                    if (Owner.TryGetComponent(out MindComponent? mindComponent))
                    {
                        //getting conditions
                        if (mindComponent.Mind?.AllObjectives != null)
                        {
                            foreach (var objective in mindComponent.Mind?.AllObjectives!)
                            {
                                if (!conditions.ContainsKey(objective.Issuer))
                                    conditions[objective.Issuer] = new List<ConditionInfo>();
                                foreach (var condition in objective.Conditions)
                                {
                                    conditions[objective.Issuer].Add(new ConditionInfo(condition.GetTitle(),
                                        condition.GetDescription(), condition.GetIcon(), condition.GetProgress(mindComponent.Mind)));
                                }
                            }
                        }
                        //getting jobtitle
                        var jobRole =
                            mindComponent.Mind?.AllRoles.FirstOrDefault(role => role.GetType() == typeof(Job));
                        if(jobRole != null)
                        {
                            jobTitle = jobRole.Name;
                        }
                    }
                    SendNetworkMessage(new CharacterInfoMessage(jobTitle, conditions));
                    break;
            }
        }
    }
}
