using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Markers;
using Content.Server.Players;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

#nullable enable
namespace Content.Server.GameObjects.Components.Observer
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent
    {
        private bool _canReturnToBody = true;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
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
                                session?.AttachedEntity!.Transform.Coordinates =
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
                                session?.AttachedEntity!.Transform.Coordinates = warpPoint.Owner.Transform.Coordinates ;
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
            return comp.EntityQuery<WarpPointComponent>().ToList();
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
