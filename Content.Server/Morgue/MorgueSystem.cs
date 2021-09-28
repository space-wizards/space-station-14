using Content.Server.Morgue.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Morgue
{
    [UsedImplicitly]
    public class MorgueSystem : EntitySystem
    {

        private float _accumulatedFrameTime;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CrematoriumEntityStorageComponent, GetAlternativeVerbsEvent>(AddCremateVerb);
            SubscribeLocalEvent<BodyBagEntityStorageComponent, GetAlternativeVerbsEvent>(AddRemoveLabelVerb);
        }

        private void AddCremateVerb(EntityUid uid, CrematoriumEntityStorageComponent component, GetAlternativeVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract || component.Cooking || component.Open)
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("cremate-verb-get-data-text");
            // TODO VERB ICON add flame/burn symbol?
            verb.Act = () => component.TryCremate();
            args.Verbs.Add(verb);
        }

        /// <summary>
        ///     This adds the "remove label" verb to the list of verbs. Yes, this is a stupid function name, but it's
        ///     consistent with other get-verb event handlers.
        /// </summary>
        private void AddRemoveLabelVerb(EntityUid uid, BodyBagEntityStorageComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract || component.LabelContainer?.ContainedEntity == null)
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("remove-label-verb-get-data-text");
            // TODO VERB ICON Add cancel/X icon? or maybe just use the pick-up or eject icon?
            verb.Act = () => component.RemoveLabel(args.User);
            args.Verbs.Add(verb);
        }

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime >= 10)
            {
                foreach (var morgue in EntityManager.EntityQuery<MorgueEntityStorageComponent>(true))
                {
                    morgue.Update();
                }
                _accumulatedFrameTime -= 10;
            }
        }
    }
}
