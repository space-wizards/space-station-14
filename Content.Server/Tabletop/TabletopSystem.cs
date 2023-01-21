using Content.Server.Tabletop.Components;
using Content.Shared.Interaction;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Events;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public sealed partial class TabletopSystem : SharedTabletopSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ViewSubscriberSystem _viewSubscriberSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<TabletopStopPlayingEvent>(OnStopPlaying);
            SubscribeLocalEvent<TabletopGameComponent, ActivateInWorldEvent>(OnTabletopActivate);
            SubscribeLocalEvent<TabletopGameComponent, ComponentShutdown>(OnGameShutdown);
            SubscribeLocalEvent<TabletopGamerComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<TabletopGamerComponent, ComponentShutdown>(OnGamerShutdown);
            SubscribeLocalEvent<TabletopGameComponent, GetVerbsEvent<ActivationVerb>>(AddPlayGameVerb);

            InitializeMap();
        }

        protected override void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession is not IPlayerSession playerSession)
                return;

            if (!TryComp(msg.TableUid, out TabletopGameComponent? tabletop) || tabletop.Session is not { } session)
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

            if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
                return;

            ActivationVerb verb = new();
            verb.Text = Loc.GetString("tabletop-verb-play-game");
            verb.IconTexture = "/Textures/Interface/VerbIcons/die.svg.192dpi.png";
            verb.Act = () => OpenSessionFor(actor.PlayerSession, uid);
            args.Verbs.Add(verb);
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

            foreach (var gamer in EntityManager.EntityQuery<TabletopGamerComponent>())
            {
                if (!EntityManager.EntityExists(gamer.Tabletop))
                    continue;

                if (!EntityManager.TryGetComponent(gamer.Owner, out ActorComponent? actor))
                {
                    EntityManager.RemoveComponent<TabletopGamerComponent>(gamer.Owner);
                    return;
                }

                var gamerUid = (gamer).Owner;

                if (actor.PlayerSession.Status != SessionStatus.InGame || !CanSeeTable(gamerUid, gamer.Tabletop))
                    CloseSessionFor(actor.PlayerSession, gamer.Tabletop);
            }
        }
    }
}
