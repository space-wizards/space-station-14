using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Markers;
using Content.Server.Players;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Utility;
using Robust.Shared.Localization;
using System;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;

#nullable enable
namespace Content.Server.GameObjects.Components.Observer
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent, IExamine
    {
        private bool _canReturnToBody = true;
        private TimeSpan _timeOfDeath = TimeSpan.Zero;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _gameTimer = default!;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReturnToBody
        {
            get => _canReturnToBody;
            set
            {
                _canReturnToBody = value;
                Dirty();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<VisibilityComponent>().Layer = (int) VisibilityFlags.Ghost;
            _timeOfDeath = _gameTimer.RealTime;
        }

        public override ComponentState GetComponentState() => new GhostComponentState(CanReturnToBody);

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg msg:
                    msg.NewPlayer.VisibilityMask |= (int) VisibilityFlags.Ghost;
                    Dirty();
                    break;
                case PlayerDetachedMsg msg:
                    msg.OldPlayer.VisibilityMask &= ~(int) VisibilityFlags.Ghost;
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null!)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case ReturnToBodyComponentMessage _:
                    if (!Owner.TryGetComponent(out IActorComponent? actor) ||
                        !CanReturnToBody)
                    {
                        break;
                    }

                    if (netChannel == actor.playerSession.ConnectedClient)
                    {
                        var o = actor.playerSession.ContentData()!.Mind;
                        o?.UnVisit();
                        Owner.Delete();
                    }
                    break;
                case ReturnToCloneComponentMessage _:

                    if (Owner.TryGetComponent(out VisitingMindComponent? mind))
                    {
                        Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new GhostReturnMessage(mind.Mind));
                    }
                    break;
                case GhostWarpRequestMessage warp:
                    if (warp.PlayerTarget != default)
                    {
                        foreach (var player in _playerManager.GetAllPlayers())
                        {
                            if (player.AttachedEntity != null && warp.PlayerTarget == player.AttachedEntity.Uid)
                            {
                                session!.AttachedEntity!.Transform.Coordinates =
                                    player.AttachedEntity.Transform.Coordinates;
                            }
                        }
                    }
                    else
                    {
                        foreach (var warpPoint in FindWaypoints())
                        {
                            if (warp.WarpName == warpPoint.Location)
                            {
                                session!.AttachedEntity!.Transform.Coordinates = warpPoint.Owner.Transform.Coordinates ;
                            }
                        }
                    }
                    break;
                case GhostRequestPlayerNameData _:
                    var playerNames = new Dictionary<EntityUid, string>();
                    foreach (var names in _playerManager.GetAllPlayers())
                    {
                        if (names.AttachedEntity != null && names.UserId != netChannel.UserId)
                        {
                            playerNames.Add(names.AttachedEntity.Uid,names.AttachedEntity.Name);
                        }
                    }
                    SendNetworkMessage(new GhostReplyPlayerNameData(playerNames));
                    break;
                case GhostRequestWarpPointData _:
                    var warpPoints = FindWaypoints();
                    var warpName = new List<string>();
                    foreach (var point in warpPoints)
                    {
                        warpName.Add(point.Location);
                    }
                    SendNetworkMessage(new GhostReplyWarpPointData(warpName));
                    break;
            }
        }

        private List<WarpPointComponent> FindWaypoints()
        {
            var comp = IoCManager.Resolve<IComponentManager>();
            return comp.EntityQuery<WarpPointComponent>(true).ToList();
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            var timeSinceDeath = _gameTimer.RealTime.Subtract(_timeOfDeath);
            //If we've been dead for longer than 1 minute use minutes, otherwise use seconds. Ignore the improper plurals.
            var deathTimeInfo = timeSinceDeath.Minutes > 0 ? Loc.GetString($"{timeSinceDeath.Minutes} minutes ago") : Loc.GetString($"{timeSinceDeath.Seconds} seconds ago");

            message.AddMarkup(Loc.GetString("Died [color=yellow]{0}[/color].", deathTimeInfo));
        }

        public class GhostReturnMessage : EntitySystemMessage
        {
            public GhostReturnMessage(Mind sender)
            {
                Sender = sender;
            }

            public Mind Sender { get; }
        }
    }
}
