using System.Collections.Generic;
using Content.Server.Mind.Components;
using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Server.Traitor;

namespace Content.Server.Objectives.Conditions
{
    [DataDefinition]
    public class RandomTraitorAliveCondition : IObjectiveCondition
    {
        protected Mind.Mind? Target;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            var entityMgr = IoCManager.Resolve<IEntityManager>();
            List<Mind.Mind> _allOtherTraitors = new List<Mind.Mind>();

            foreach (var targetMind in entityMgr.EntityQuery<MindComponent>())
            {
                if (targetMind.Mind?.CharacterDeadIC == false && targetMind.Mind != mind && targetMind.Mind?.HasRole<TraitorRole>() == true)
                {
                        _allOtherTraitors.Add(targetMind.Mind);
                }
            }

            return new RandomTraitorAliveCondition {Target = IoCManager.Resolve<IRobustRandom>().Pick(_allOtherTraitors)};
        }

        public string Title
        {
            get
            {
                var targetName = string.Empty;

                if (Target == null)
                    return Loc.GetString("objective-condition-other-traitor-alive-title", ("targetName", targetName));

                if (Target.CharacterName != null)
                    targetName = Target.CharacterName;
                else if (Target.OwnedEntity is {Valid: true} owned)
                    targetName = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(owned).EntityName;

                return Loc.GetString("objective-condition-other-traitor-alive-title", ("targetName", targetName));
            }
        }

        public string Description => Loc.GetString("objective-condition-other-traitor-alive-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Misc/bureaucracy.rsi"), "folder_red");

        public float Progress => (!Target?.CharacterDeadIC ?? true) ? 1f : 0f;

        public float Difficulty => 1.75f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is RandomTraitorAliveCondition kpc && Equals(Target, kpc.Target);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is RandomTraitorAliveCondition alive && alive.Equals(this);
        }

        public override int GetHashCode()
        {
            return Target?.GetHashCode() ?? 0;
        }
    }
}
