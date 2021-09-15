using Content.Server.Cuffs.Components;
using Content.Server.Hands.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Cuffs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Content.Shared.Verbs;
using Robust.Shared.Localization;

namespace Content.Server.Cuffs
{
    [UsedImplicitly]
    internal sealed class CuffableSystem : SharedCuffableSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityManager.EventBus.SubscribeEvent<HandCountChangedEvent>(EventSource.Local, this, OnHandCountChanged);

            SubscribeLocalEvent<CuffableComponent, GetOtherVerbsEvent>(AddUncuffVerb);
        }

        private void AddUncuffVerb(EntityUid uid, CuffableComponent component, GetOtherVerbsEvent args)
        {
            if (!args.CanAccess || component.CuffedHandCount == 0)
                return;

            // check that the user is either un-cuffing themselves or is able to interact with the target.
            if (args.User != args.Target && !args.CanInteract)
                return;

            Verb verb = new();
            verb.Act = () => component.TryUncuff(args.User);
            verb.Text = Loc.GetString("uncuff-verb-get-data-text");
            //TODO VERB ICON add uncuffing symbol? may re-use the symbol showing that you are currently cuffed?
            args.Verbs.Add(verb);
        }

        /// <summary>
        ///     Check the current amount of hands the owner has, and if there's less hands than active cuffs we remove some cuffs.
        /// </summary>
        private void OnHandCountChanged(HandCountChangedEvent message)
        {
            var owner = message.Sender;

            if (!owner.TryGetComponent(out CuffableComponent? cuffable) ||
                !cuffable.Initialized) return;

            var dirty = false;
            var handCount = owner.GetComponentOrNull<HandsComponent>()?.Count ?? 0;

            while (cuffable.CuffedHandCount > handCount && cuffable.CuffedHandCount > 0)
            {
                dirty = true;

                var container = cuffable.Container;
                var entity = container.ContainedEntities[^1];

                container.Remove(entity);
                entity.Transform.WorldPosition = owner.Transform.WorldPosition;
            }

            if (dirty)
            {
                cuffable.CanStillInteract = handCount > cuffable.CuffedHandCount;
                cuffable.CuffedStateChanged();
                cuffable.Dirty();
            }
        }
    }
}
