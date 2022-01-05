using System.Collections.Generic;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.CharacterInfo;

public class CharacterInfoSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestCharacterInfoEvent>(OnRequestCharacterInfoEvent);
    }

    private void OnRequestCharacterInfoEvent(RequestCharacterInfoEvent msg, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue
            || args.SenderSession.AttachedEntity != msg.EntityUid)
            return;

        var entity = args.SenderSession.AttachedEntity.Value;

        var conditions = new Dictionary<string, List<ConditionInfo>>();
        var jobTitle = "No Profession";
        if (EntityManager.TryGetComponent(entity, out MindComponent? mindComponent) && mindComponent.Mind != null)
        {
            var mind = mindComponent.Mind;

            // Get objectives
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

            // Get job title
            foreach (var role in mind.AllRoles)
            {
                if (role.GetType() != typeof(Job)) continue;

                jobTitle = role.Name;
                break;
            }
        }

        RaiseNetworkEvent(new CharacterInfoEvent(entity, jobTitle, conditions),
            Filter.SinglePlayer(args.SenderSession));
    }
}
