using Content.Server.Tabletop.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Events;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public partial class TabletopSystem : SharedTabletopSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ViewSubscriberSystem _viewSubscriberSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            SubscribeNetworkEvent<TabletopStopPlayingEvent>(OnStopPlaying);
            SubscribeLocalEvent<TabletopGameComponent, ActivateInWorldEvent>(OnTabletopActivate);
            SubscribeLocalEvent<TabletopGameComponent, ComponentShutdown>(OnGameShutdown);

            SubscribeLocalEvent<TabletopGamerComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<TabletopGamerComponent, ComponentShutdown>(OnGamerShutdown);

            InitializeMap();
            InitializeDraggable();
        }

        private void OnTabletopActivate(EntityUid uid, TabletopGameComponent component, ActivateInWorldEvent args)
        {
            // Check that a player is attached to the entity.
            if (!EntityManager.TryGetComponent(args.User.Uid, out ActorComponent? actor))
                return;

            // Check that the entity can interact with the game board.
            if(_actionBlockerSystem.CanInteract(args.User))
                OpenSessionFor(actor.PlayerSession, uid);
        }

        private void OnGameShutdown(EntityUid uid, TabletopGameComponent component, ComponentShutdown args)
        {
            CleanupSession(uid);
        }

        private void OnStopPlaying(TabletopStopPlayingEvent msg, EntitySessionEventArgs args)
        {
            CloseSessionFor((IPlayerSession)args.SenderSession, msg.TableUid);
        }

        private void OnPlayerDetached(EntityUid uid, TabletopGamerComponent component, PlayerDetachedEvent args)
        {
            if(component.Tabletop.IsValid())
                CloseSessionFor(args.Player, component.Tabletop);
        }

        private void OnGamerShutdown(EntityUid uid, TabletopGamerComponent component, ComponentShutdown args)
        {
            if (!EntityManager.TryGetComponent(uid, out ActorComponent? actor))
                return;

            if(component.Tabletop.IsValid())
                CloseSessionFor(actor.PlayerSession, component.Tabletop);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var gamer in EntityManager.EntityQuery<TabletopGamerComponent>(true))
            {
                if (!EntityManager.TryGetEntity(gamer.Tabletop, out var table))
                    continue;

                if (!gamer.Owner.TryGetComponent(out ActorComponent? actor))
                {
                    gamer.Owner.RemoveComponent<TabletopGamerComponent>();
                    return;
                };

                if (actor.PlayerSession.Status > SessionStatus.Connected || CanSeeTable(gamer.Owner, table)
                                                                         || !StunnedOrNoHands(gamer.Owner))
                    continue;

                CloseSessionFor(actor.PlayerSession, table.Uid);
            }
        }
    }
}
