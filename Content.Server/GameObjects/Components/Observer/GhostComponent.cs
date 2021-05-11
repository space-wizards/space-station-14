using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Markers;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs;
using Content.Server.Players;
using Content.Shared.GameObjects.Components.Observer;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

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

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();

            // Allow this entity to be seen by other ghosts.
            Owner.EnsureComponent<VisibilityComponent>().Layer |= (int) VisibilityFlags.Ghost;

            // Allows this entity to see other ghosts.
            Owner.EnsureComponent<EyeComponent>().VisibilityMask |= (uint) VisibilityFlags.Ghost;

            _timeOfDeath = _gameTimer.RealTime;
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            //Perf: If the entity is deleting itself, no reason to change these back.
            if(Owner.LifeStage < EntityLifeStage.Terminating)
            {
                // Entity can't be seen by ghosts anymore.
                if (Owner.TryGetComponent<VisibilityComponent>(out var visComp))
                    visComp.Layer &= ~(int) VisibilityFlags.Ghost;

                // Entity can't see ghosts anymore.
                if (Owner.TryGetComponent<EyeComponent>(out var eyeComp))
                    eyeComp.VisibilityMask &= ~(uint) VisibilityFlags.Ghost;
            }

            base.Shutdown();
        }

        public override ComponentState GetComponentState(ICommonSession player) => new GhostComponentState(CanReturnToBody);

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null!)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case ReturnToBodyComponentMessage:
                {
                    if (!Owner.TryGetComponent(out IActorComponent? actor) ||
                        !CanReturnToBody)
                    {
                        break;
                    }

                    if (netChannel == actor.playerSession.ConnectedClient)
                    {
                        var o = actor.playerSession.ContentData()!.Mind;
                        o?.UnVisit();
                    }
                    break;
                }
                case GhostWarpToLocationRequestMessage warp:
                {
                    if (session?.AttachedEntity != Owner)
                    {
                        break;
                    }

                    foreach (var warpPoint in FindWaypoints())
                    {
                        if (warp.Name == warpPoint.Location)
                        {
                            Owner.Transform.Coordinates = warpPoint.Owner.Transform.Coordinates;
                            break;
                        }
                    }

                    Logger.Warning($"User {session.Name} tried to warp to an invalid warp: {warp.Name}");

                    break;
                }
                case GhostWarpToTargetRequestMessage target:
                {
                    if (session?.AttachedEntity != Owner)
                    {
                        break;
                    }

                    if (!Owner.TryGetComponent(out IActorComponent? actor))
                    {
                        break;
                    }

                    if (!Owner.EntityManager.TryGetEntity(target.Target, out var entity))
                    {
                        Logger.Warning($"User {session.Name} tried to warp to an invalid entity id: {target.Target}");
                        break;
                    }

                    if (!_playerManager.TryGetSessionByChannel(actor.playerSession.ConnectedClient, out var player) ||
                        player.AttachedEntity != entity)
                    {
                        break;
                    }

                    Owner.Transform.Coordinates = entity.Transform.Coordinates;
                    break;
                }
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
                        if (point.Location == null)
                        {
                            continue;
                        }

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
    }
}
