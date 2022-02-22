using Content.Server.Morgue.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Morgue
{
    [UsedImplicitly]
    public sealed class MorgueSystem : EntitySystem
    {

        private float _accumulatedFrameTime;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CrematoriumEntityStorageComponent, GetVerbsEvent<AlternativeVerb>>(AddCremateVerb);
        }

        private void AddCremateVerb(EntityUid uid, CrematoriumEntityStorageComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || component.Cooking || component.Open)
                return;

            AlternativeVerb verb = new();
            verb.Text = Loc.GetString("cremate-verb-get-data-text");
            // TODO VERB ICON add flame/burn symbol?
            verb.Act = () => component.TryCremate();
            verb.Impact = LogImpact.Medium; // could be a body? or evidence? I dunno.
            args.Verbs.Add(verb);
        }

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime >= 10)
            {
                foreach (var morgue in EntityManager.EntityQuery<MorgueEntityStorageComponent>())
                {
                    morgue.Update();
                }
                _accumulatedFrameTime -= 10;
            }
        }
    }
}
