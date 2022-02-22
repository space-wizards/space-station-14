using System.Threading;
using Content.Server.Act;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Morgue;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(MorgueEntityStorageComponent))]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
#pragma warning disable 618
    public sealed class CrematoriumEntityStorageComponent : MorgueEntityStorageComponent, IExamine, ISuicideAct
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [DataField("cremateStartSound")] private SoundSpecifier _cremateStartSound = new SoundPathSpecifier("/Audio/Items/lighter1.ogg");
        [DataField("crematingSound")] private SoundSpecifier _crematingSound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");
        [DataField("cremateFinishSound")] private SoundSpecifier _cremateFinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");

        [ViewVariables]
        public bool Cooking { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        private int _burnMilis = 5000;

        private CancellationTokenSource? _cremateCancelToken;

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!_entities.TryGetComponent<AppearanceComponent>(Owner, out var appearance)) return;

            if (inDetailsRange)
            {
                if (appearance.TryGetData(CrematoriumVisuals.Burning, out bool isBurning) && isBurning)
                {
                    message.AddMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-is-burning", ("owner", Owner)) + "\n");
                }

                if (appearance.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents)
                {
                    message.AddMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-has-contents"));
                }
                else
                {
                    message.AddText(Loc.GetString("crematorium-entity-storage-component-on-examine-details-empty"));
                }
            }
        }

        public override bool CanOpen(EntityUid user, bool silent = false)
        {
            if (Cooking)
            {
                if (!silent)
                    Owner.PopupMessage(user, Loc.GetString("crematorium-entity-storage-component-is-cooking-safety-message"));
                return false;
            }
            return base.CanOpen(user, silent);
        }

        public void TryCremate()
        {
            if (Cooking) return;
            if (Open) return;

            SoundSystem.Play(Filter.Pvs(Owner), _cremateStartSound.GetSound(), Owner);

            Cremate();
        }

        public void Cremate()
        {
            if (Open)
                CloseStorage();

            if(_entities.TryGetComponent(Owner, out AppearanceComponent appearanceComponent))
                appearanceComponent.SetData(CrematoriumVisuals.Burning, true);
            Cooking = true;

            SoundSystem.Play(Filter.Pvs(Owner), _crematingSound.GetSound(), Owner);

            _cremateCancelToken?.Cancel();

            _cremateCancelToken = new CancellationTokenSource();
            Owner.SpawnTimer(_burnMilis, () =>
            {
                if (_entities.Deleted(Owner))
                    return;
                if(_entities.TryGetComponent(Owner, out appearanceComponent))
                    appearanceComponent.SetData(CrematoriumVisuals.Burning, false);
                Cooking = false;

                if (Contents.ContainedEntities.Count > 0)
                {
                    for (var i = Contents.ContainedEntities.Count - 1; i >= 0; i--)
                    {
                        var item = Contents.ContainedEntities[i];
                        Contents.Remove(item);
                        _entities.DeleteEntity(item);
                    }

                    var ash = _entities.SpawnEntity("Ash", _entities.GetComponent<TransformComponent>(Owner).Coordinates);
                    Contents.Insert(ash);
                }

                TryOpenStorage(Owner);

                SoundSystem.Play(Filter.Pvs(Owner), _cremateFinishSound.GetSound(), Owner);

            }, _cremateCancelToken.Token);
        }

        SuicideKind ISuicideAct.Suicide(EntityUid victim, IChatManager chat)
        {
            if (_entities.TryGetComponent(victim, out ActorComponent? actor) && actor.PlayerSession.ContentData()?.Mind is {} mind)
            {
                EntitySystem.Get<GameTicker>().OnGhostAttempt(mind, false);

                if (mind.OwnedEntity is {Valid: true} entity)
                {
                    entity.PopupMessage(Loc.GetString("crematorium-entity-storage-component-suicide-message"));
                }
            }

            victim.PopupMessageOtherClients(Loc.GetString("crematorium-entity-storage-component-suicide-message-others", ("victim", victim)));

            if (CanInsert(victim))
            {
                Insert(victim);
                EntitySystem.Get<StandingStateSystem>().Down(victim, false);
            }
            else
            {
                _entities.DeleteEntity(victim);
            }

            Cremate();

            return SuicideKind.Heat;
        }
    }
}
