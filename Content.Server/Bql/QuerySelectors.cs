using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Mind.Components;
using Content.Server.Power.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Tag;
using Robust.Server.Bql;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Bql
{
    public class QuerySelectors
    {
        [RegisterBqlQuerySelector]
        public class MindfulQuerySelector : BqlQuerySelector
        {
            public override string Token => "mindful";

            public override QuerySelectorArgument[] Arguments => Array.Empty<QuerySelectorArgument>();

            public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input,
                IReadOnlyList<object> arguments, bool isInverted)
            {
                return input.Where(x =>
                {
                    if (x.TryGetComponent<MindComponent>(out var mind))
                    {
                        return (mind.Mind?.VisitingEntity?.Uid == x.Uid) ^ isInverted;
                    }

                    return isInverted;
                });
            }

            public override IEnumerable<IEntity> DoInitialSelection(IReadOnlyList<object> arguments, bool isInverted)
            {
                if (isInverted)
                {
                    return base.DoInitialSelection(arguments, isInverted);
                }

                return IoCManager.Resolve<IEntityManager>().EntityQuery<MindComponent>()
                    .Where(mind => (mind.Mind?.VisitingEntity?.Uid == mind.Mind?.CurrentEntity?.Uid) ^ isInverted)
                    .Select(x => x.Owner);
            }
        }

        [RegisterBqlQuerySelector]
        public class TaggedQuerySelector : BqlQuerySelector
        {
            public override string Token => "tagged";

            public override QuerySelectorArgument[] Arguments => new [] { QuerySelectorArgument.String };

            public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
            {
                return input.Where(x =>
                    x.HasComponent<TagComponent>() &&
                    (x.GetComponent<TagComponent>().Tags.Contains((string) arguments[0]) ^ isInverted));
            }

            public override IEnumerable<IEntity> DoInitialSelection(IReadOnlyList<object> arguments, bool isInverted)
            {
                return IoCManager.Resolve<IEntityManager>().EntityQuery<TagComponent>()
                    .Where(tag => tag.Tags.Contains((string) arguments[0]))
                    .Select(x => x.Owner);
            }
        }

        [RegisterBqlQuerySelector]
        public class AliveQuerySelector : BqlQuerySelector
        {
            public override string Token => "alive";

            public override QuerySelectorArgument[] Arguments => Array.Empty<QuerySelectorArgument>();

            public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
            {
                return input.Where(x =>
                    x.HasComponent<MindComponent>() &&
                    (!(x.GetComponent<MindComponent>().Mind?.CharacterDeadPhysically ?? false) ^ isInverted));
            }

            public override IEnumerable<IEntity> DoInitialSelection(IReadOnlyList<object> arguments, bool isInverted)
            {
                return IoCManager.Resolve<IEntityManager>().EntityQuery<MindComponent>()
                    .Where(mind => !(mind.Mind?.CharacterDeadPhysically ?? false) ^ isInverted)
                    .Select(x => x.Owner);
            }
        }

        [RegisterBqlQuerySelector]
        public class HasReagentQuerySelector : BqlQuerySelector
        {
            public override string Token => "hasreagent";

            public override QuerySelectorArgument[] Arguments => new [] { QuerySelectorArgument.String };

            public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
            {
                var reagent = (string) arguments[0];
                return input.Where(x =>
                {
                    if (x.TryGetComponent<SolutionContainerManagerComponent>(out var solutionContainerManagerComponent))
                    {
                        return solutionContainerManagerComponent.Solutions
                            .Any((solution) => solution.Value.ContainsReagent(reagent)) ^ isInverted;
                    }

                    return isInverted;
                });
            }
        }

        [RegisterBqlQuerySelector]
        public class PoweredQuerySelector : BqlQuerySelector
        {
            public override string Token => "powered";

            public override QuerySelectorArgument[] Arguments => Array.Empty<QuerySelectorArgument>();

            public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
            {
                return input.Where(x =>
                    x.TryGetComponent<ApcPowerReceiverComponent>(out var apcPowerReceiver)
                        ? apcPowerReceiver.Powered ^ isInverted
                        : isInverted);
            }
        }
    }
}
