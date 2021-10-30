using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.ContextMenu.UI;
using Content.Client.Popups;
using Content.Client.Verbs.UI;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.Verbs
{
    [UsedImplicitly]
    public sealed class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IEntityLookup _entityLookup = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        /// <summary>
        ///     When a user right clicks somewhere, how large is the box we use to get entities for the context menu?
        /// </summary>
        public const float EntityMenuLookupSize = 1f;

        public EntityMenuPresenter EntityMenu = default!;
        public VerbMenuPresenter VerbMenu = default!;

        [Dependency] private readonly IEyeManager _eyeManager = default!;

        /// <summary>
        ///     These flags determine what entities the user can see on the context menu.
        /// </summary>
        public MenuVisibility Visibility;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeNetworkEvent<VerbsResponseEvent>(HandleVerbResponse);

            EntityMenu = new(this);
            VerbMenu = new(this);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            CloseAllMenus();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EntityMenu?.Dispose();
            VerbMenu?.Dispose();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            EntityMenu?.Update();
        }

        public void CloseAllMenus()
        {
            EntityMenu.Close();
            VerbMenu.Close();
        }

        /// <summary>
        ///     Get all of the entities in an area for displaying on the context menu.
        /// </summary>
        public bool TryGetEntityMenuEntities(MapCoordinates targetPos, [NotNullWhen(true)] out List<IEntity>? result)
        {
            result = null;
            var player = _playerManager.LocalPlayer?.ControlledEntity;

            if (player == null)
                return false;

            var visibility = _eyeManager.CurrentEye.DrawFov
                ? Visibility
                : Visibility | MenuVisibility.NoFov;

            // Check if we have LOS to the clicked-location.
            if ((visibility & MenuVisibility.NoFov) == 0 &&
                !player.InRangeUnOccluded(targetPos, range: ExamineSystemShared.ExamineRange))
                return false;

            // Get entities
            var entities = _entityLookup.GetEntitiesIntersecting(
                    targetPos.MapId,
                    Box2.CenteredAround(targetPos.Position, (EntityMenuLookupSize, EntityMenuLookupSize)))
                .ToList();

            if (entities.Count == 0)
                return false;

            if (visibility == MenuVisibility.All)
            {
                result = entities;
                return true;
            }

            // remove any entities in containers
            if ((visibility & MenuVisibility.InContainer) == 0)
            {
                foreach (var entity in entities.ToList())
                {
                    if (!player.IsInSameOrTransparentContainer(entity))
                        entities.Remove(entity);
                }
            }

            // remove any invisible entities
            if ((visibility & MenuVisibility.Invisible) == 0)
            {
                foreach (var entity in entities.ToList())
                {
                    if (!EntityManager.TryGetComponent(entity.Uid, out ISpriteComponent? spriteComponent) ||
                    !spriteComponent.Visible)
                    {
                        entities.Remove(entity);
                        continue;
                    }

                    if (entity.HasTag("HideContextMenu"))
                        entities.Remove(entity);
                }
            }

            // Remove any entities that do not have LOS
            if ((visibility & MenuVisibility.NoFov) == 0)
            {
                var playerPos = player.Transform.MapPosition;
                foreach (var entity in entities.ToList())
                {
                    if (!ExamineSystemShared.InRangeUnOccluded(
                        playerPos,
                        entity.Transform.MapPosition,
                        ExamineSystemShared.ExamineRange,
                        null))
                    {
                        entities.Remove(entity);
                    }
                }
            }

            if (entities.Count == 0)
                return false;

            result = entities;
            return true;
        }

        /// <summary>
        ///     Ask the server to send back a list of server-side verbs, and for now return an incomplete list of verbs
        ///     (only those defined locally).
        /// </summary>
        public Dictionary<VerbType, SortedSet<Verb>> GetVerbs(IEntity target, IEntity user, VerbType verbTypes)
        {
            if (!target.Uid.IsClientSide())
            {
                RaiseNetworkEvent(new RequestServerVerbsEvent(target.Uid, verbTypes));
            }
            
            return GetLocalVerbs(target, user, verbTypes);
        }

        /// <summary>
        ///     Execute actions associated with the given verb.
        /// </summary>
        /// <remarks>
        ///     Unless this is a client-exclusive verb, this will also tell the server to run the same verb. However, if the verb
        ///     is disabled and has a tooltip, this function will only generate a pop-up-message instead of executing anything.
        /// </remarks>
        public void ExecuteVerb(EntityUid target, Verb verb, VerbType verbType)
        {
            if (verb.Disabled)
            {
                if (verb.Message != null)
                    _popupSystem.PopupCursor(verb.Message);
                return;
            }

            ExecuteVerb(verb);

            if (!verb.ClientExclusive)
            {
                RaiseNetworkEvent(new ExecuteVerbEvent(target, verb, verbType));
            }
        }

        private void HandleVerbResponse(VerbsResponseEvent msg)
        {
            if (!VerbMenu.RootMenu.Visible || VerbMenu.CurrentTarget != msg.Entity)
                return;

            VerbMenu.AddServerVerbs(msg.Verbs);
        }
    }

    [Flags]
    public enum MenuVisibility
    {
        // What entities can a user see on the entity menu?
        Default = 0,          // They can only see entities in FoV.
        NoFov = 1 << 0,         // They ignore FoV restrictions
        InContainer = 1 << 1,   // They can see through containers.
        Invisible = 1 << 2,   // They can see entities without sprites and the "HideContextMenu" tag is ignored.
        All = NoFov | InContainer | Invisible
    }
}
