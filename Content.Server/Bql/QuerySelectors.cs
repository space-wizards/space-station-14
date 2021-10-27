using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Mind.Components;
using Content.Shared.Tag;
using Robust.Server.Bql;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using YamlDotNet.Core.Tokens;

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
                    x.GetComponent<TagComponent>().Tags.Contains((string) arguments[0]));
            }

            public override IEnumerable<IEntity> DoInitialSelection(IReadOnlyList<object> arguments, bool isInverted)
            {
                return IoCManager.Resolve<IEntityManager>().EntityQuery<TagComponent>()
                    .Where(tag => tag.Tags.Contains((string) arguments[0]))
                    .Select(x => x.Owner);
            }
        }
}
}
