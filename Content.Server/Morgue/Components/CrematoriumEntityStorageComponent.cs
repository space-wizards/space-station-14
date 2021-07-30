using System.Threading;
using Content.Server.Act;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Notification;
using Content.Server.Players;
using Content.Server.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Morgue;
using Content.Shared.Notification.Managers;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(MorgueEntityStorageComponent))]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class CrematoriumEntityStorageComponent : MorgueEntityStorageComponent, IExamine, ISuicideAct
    {
        public override string Name => "CrematoriumEntityStorage";

        [ViewVariables]
        public bool Cooking { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        private int _burnMilis = 3000;

        private CancellationTokenSource? _cremateCancelToken;

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (Appearance == null) return;

            if (inDetailsRange)
            {
                if (Appearance.TryGetData(CrematoriumVisuals.Burning, out bool isBurning) && isBurning)
                {
                    message.AddMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-is-burning",("owner", Owner)) + "\n");
                }

                if (Appearance.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents)
                {
                    message.AddMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-has-contents"));
                }
                else
                {
                    message.AddText(Loc.GetString("crematorium-entity-storage-component-on-examine-details-empty"));
                }
            }
        }

        public override bool CanOpen(IEntity user, bool silent = false)
        {
            if (Cooking)
            {
                if (!silent) Owner.PopupMessage(user, Loc.GetString("crematorium-entity-storage-component-is-cooking-safety-message"));
                return false;
            }
            return base.CanOpen(user, silent);
        }

        public void TryCremate()
        {
            if (Cooking) return;
            if (Open) return;

            Cremate();
        }

        public void Cremate()
        {
            if (Open)
                CloseStorage();

            Appearance?.SetData(CrematoriumVisuals.Burning, true);
            Cooking = true;

            _cremateCancelToken?.Cancel();

            _cremateCancelToken = new CancellationTokenSource();
            Owner.SpawnTimer(_burnMilis, () =>
            {
                if (Owner.Deleted)
                    return;

                Appearance?.SetData(CrematoriumVisuals.Burning, false);
                Cooking = false;

                if (Contents.ContainedEntities.Count > 0)
                {
                    for (var i = Contents.ContainedEntities.Count - 1; i >= 0; i--)
                    {
                        var item = Contents.ContainedEntities[i];
                        Contents.Remove(item);
                        item.Delete();
                    }

                    var ash = Owner.EntityManager.SpawnEntity("Ash", Owner.Transform.Coordinates);
                    Contents.Insert(ash);
                }

                TryOpenStorage(Owner);

                SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Machines/ding.ogg", Owner);
            }, _cremateCancelToken.Token);
        }

        SuicideKind ISuicideAct.Suicide(IEntity victim, IChatManager chat)
        {
            var mind = victim.PlayerSession()?.ContentData()?.Mind;

            if (mind != null)
            {
                EntitySystem.Get<GameTicker>().OnGhostAttempt(mind, false);
                mind.OwnedEntity?.PopupMessage(Loc.GetString("crematorium-entity-storage-component-suicide-message"));
            }

            victim.PopupMessageOtherClients(Loc.GetString("crematorium-entity-storage-component-suicide-message-others", ("victim", victim)));

            if (CanInsert(victim))
            {
                Insert(victim);
                EntitySystem.Get<StandingStateSystem>().Down(victim, false);
            }
            else
            {
                victim.Delete();
            }

            Cremate();

            return SuicideKind.Heat;
        }

        [Verb]
        private sealed class CremateVerb : Verb<CrematoriumEntityStorageComponent>
        {
            protected override void GetData(IEntity user, CrematoriumEntityStorageComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) || component.Cooking || component.Open)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("cremate-verb-get-data-text");
            }

            /// <inheritdoc />
            protected override void Activate(IEntity user, CrematoriumEntityStorageComponent component)
            {
                component.TryCremate();
            }
        }
    }
}
