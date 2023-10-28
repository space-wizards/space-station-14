using Content.Server.Popups;
using Content.Server.Tabletop.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public sealed partial class TabletopSystem : SharedTabletopSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly EyeSystem _eye = default!;
        [Dependency] private readonly ViewSubscriberSystem _viewSubscriberSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<TabletopStopPlayingEvent>(OnStopPlaying);
            SubscribeLocalEvent<TabletopGameComponent, ActivateInWorldEvent>(OnTabletopActivate);
            SubscribeLocalEvent<TabletopGameComponent, ComponentShutdown>(OnGameShutdown);
            SubscribeLocalEvent<TabletopGamerComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<TabletopGamerComponent, ComponentShutdown>(OnGamerShutdown);
            SubscribeLocalEvent<TabletopGameComponent, GetVerbsEvent<ActivationVerb>>(AddPlayGameVerb);
            SubscribeLocalEvent<TabletopGameComponent, InteractUsingEvent>(OnInteractUsing);

            SubscribeNetworkEvent<TabletopRequestTakeOut>(OnTabletopRequestTakeOut);

            InitializeMap();
        }

        private void OnTabletopRequestTakeOut(TabletopRequestTakeOut msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession is not { } playerSession)
                return;

            var table = GetEntity(msg.TableUid);

            if (!TryComp(table, out TabletopGameComponent? tabletop) || tabletop.Session is not { } session)
                return;

            if (!msg.Entity.IsValid())
                return;

            var entity = GetEntity(msg.Entity);

            if (!TryComp(entity, out TabletopHologramComponent? hologram))
            {
                _popupSystem.PopupEntity(Loc.GetString("tabletop-error-remove-non-hologram"), table, args.SenderSession);
                return;
            }

            // Check if player is actually playing at this table
            if (!session.Players.ContainsKey(playerSession))
                return;

            // Find the entity, remove it from the session and set it's position to the tabletop
            session.Entities.TryGetValue(entity, out var result);
            session.Entities.Remove(result);
            QueueDel(result);
        }

        private void OnInteractUsing(EntityUid uid, TabletopGameComponent component, InteractUsingEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out HandsComponent? hands))
                return;

            if (component.Session is not { } session)
                return;

            if (hands.ActiveHand == null)
                return;

            if (hands.ActiveHand.HeldEntity == null)
                return;

            var handEnt = hands.ActiveHand.HeldEntity.Value;

            if (!TryComp<ItemComponent>(handEnt, out var item))
                return;

            var meta = MetaData(handEnt);
            var protoId = meta.EntityPrototype?.ID;

            var hologram = Spawn(protoId, session.Position.Offset(-1, 0));

            // Make sure the entity can be dragged and can be removed, move it into the board game world and add it to the Entities hashmap
            EnsureComp<TabletopDraggableComponent>(hologram);
            EnsureComp<TabletopHologramComponent>(hologram);
            session.Entities.Add(hologram);

            _popupSystem.PopupEntity(Loc.GetString("tabletop-added-piece"), uid, args.User);
        }

        protected override void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession is not { } playerSession)
                return;

            if (!TryComp(GetEntity(msg.TableUid), out TabletopGameComponent? tabletop) || tabletop.Session is not { } session)
                return;

            // Check if player is actually playing at this table
            if (!session.Players.ContainsKey(playerSession))
                return;

            base.OnTabletopMove(msg, args);
        }

        /// <summary>
        /// Add a verb that allows the player to start playing a tabletop game.
        /// </summary>
        private void AddPlayGameVerb(EntityUid uid, TabletopGameComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            var playVerb = new ActivationVerb()
            {
                Text = Loc.GetString("tabletop-verb-play-game"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
                Act = () => OpenSessionFor(actor.PlayerSession, uid)
            };

            args.Verbs.Add(playVerb);
        }

        private void OnTabletopActivate(EntityUid uid, TabletopGameComponent component, ActivateInWorldEvent args)
        {
            // Check that a player is attached to the entity.
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            OpenSessionFor(actor.PlayerSession, uid);
        }

        private void OnGameShutdown(EntityUid uid, TabletopGameComponent component, ComponentShutdown args)
        {
            CleanupSession(uid);
        }

        private void OnStopPlaying(TabletopStopPlayingEvent msg, EntitySessionEventArgs args)
        {
            CloseSessionFor(args.SenderSession, GetEntity(msg.TableUid));
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

            var query = EntityQueryEnumerator<TabletopGamerComponent>();
            while (query.MoveNext(out var uid, out var gamer))
            {
                if (!Exists(gamer.Tabletop))
                    continue;

                if (!TryComp(uid, out ActorComponent? actor))
                {
                    EntityManager.RemoveComponent<TabletopGamerComponent>(uid);
                    return;
                }

                if (actor.PlayerSession.Status != SessionStatus.InGame || !CanSeeTable(uid, gamer.Tabletop))
                    CloseSessionFor(actor.PlayerSession, gamer.Tabletop);
            }
        }
    }
}
