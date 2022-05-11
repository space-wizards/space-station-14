using Content.Server.Morgue.Components;
using Content.Shared.Morgue;
using Content.Shared.Examine;
using Content.Shared.Database;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Content.Shared.Interaction.Events;
using Robust.Server.GameObjects;
using Content.Server.Players;
using Content.Server.GameTicking;
using Content.Shared.Popups;
using Content.Server.Popups;
using Content.Shared.Standing;

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
            SubscribeLocalEvent<CrematoriumEntityStorageComponent, ExaminedEvent>(OnCrematoriumExamined);
            SubscribeLocalEvent<CrematoriumEntityStorageComponent, SuicideEvent>(OnSuicide);
            SubscribeLocalEvent<MorgueEntityStorageComponent, ExaminedEvent>(OnMorgueExamined);
        }

        private void OnSuicide(EntityUid uid, CrematoriumEntityStorageComponent component, SuicideEvent args)
        {
            if (args.Handled) return;
            args.SetHandled(SuicideKind.Heat);
            var victim = args.Victim;
            if (TryComp(victim, out ActorComponent? actor) && actor.PlayerSession.ContentData()?.Mind is { } mind)
            {
                Get<GameTicker>().OnGhostAttempt(mind, false);

                if (mind.OwnedEntity is { Valid: true } entity)
                {
                    entity.PopupMessage(Loc.GetString("crematorium-entity-storage-component-suicide-message"));
                }
            }

            victim.PopupMessageOtherClients(Loc.GetString("crematorium-entity-storage-component-suicide-message-others", ("victim", victim)));

            if (component.CanInsert(victim))
            {
                component.Insert(victim);
                Get<StandingStateSystem>().Down(victim, false);
            }
            else
            {
                
                EntityManager.DeleteEntity(victim);
            }

            component.Cremate();
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

        private void OnCrematoriumExamined(EntityUid uid, CrematoriumEntityStorageComponent component, ExaminedEvent args)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            if (args.IsInDetailsRange)
            {
                if (appearance.TryGetData(CrematoriumVisuals.Burning, out bool isBurning) && isBurning)
                {
                    args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-is-burning", ("owner", uid)));
                }

                if (appearance.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents)
                {
                    args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-has-contents"));
                }
                else
                {
                    args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-empty"));
                }
            }
        }

        private void OnMorgueExamined(EntityUid uid, MorgueEntityStorageComponent component, ExaminedEvent args)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance)) return;

            if (args.IsInDetailsRange)
            {
                if (appearance.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul)
                {
                    args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-body-has-soul"));
                }
                else if (appearance.TryGetData(MorgueVisuals.HasMob, out bool hasMob) && hasMob)
                {
                    args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-body-has-no-soul"));
                }
                else if (appearance.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents)
                {
                    args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-has-contents"));
                }
                else
                {
                    args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-empty"));
                }
            }
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
