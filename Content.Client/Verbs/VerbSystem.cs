using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client.Examine;
using Content.Client.Gameplay;
using Content.Client.Popups;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.Verbs
{
    [UsedImplicitly]
    public sealed class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ExamineSystem _examine = default!;
        [Dependency] private readonly SpriteTreeSystem _tree = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        /// <summary>
        ///     When a user right clicks somewhere, how large is the box we use to get entities for the context menu?
        /// </summary>
        public const float EntityMenuLookupSize = 0.25f;

        /// <summary>
        ///     These flags determine what entities the user can see on the context menu.
        /// </summary>
        public MenuVisibility Visibility;

        public Action<VerbsResponseEvent>? OnVerbsResponse;

        private List<EntityUid> _entities = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<VerbsResponseEvent>(HandleVerbResponse);
        }

        /// <summary>
        ///     Get all of the entities in an area for displaying on the context menu.
        /// </summary>
        public bool TryGetEntityMenuEntities(MapCoordinates targetPos, [NotNullWhen(true)] out List<EntityUid>? result)
        {
            result = null;

            if (_stateManager.CurrentState is not GameplayStateBase gameScreenBase)
                return false;

            var player = _playerManager.LocalEntity;
            if (player == null)
                return false;

            // If FOV drawing is disabled, we will modify the visibility option to ignore visiblity checks.
            var visibility = _eyeManager.CurrentEye.DrawFov
                ? Visibility
                : Visibility | MenuVisibility.NoFov;

            var ev = new MenuVisibilityEvent()
            {
                TargetPos = targetPos,
                Visibility = visibility,
            };

            RaiseLocalEvent(player.Value, ref ev);
            visibility = ev.Visibility;

            // Get entities
            _entities.Clear();
            var entitiesUnderMouse = _tree.QueryAabb(targetPos.MapId, Box2.CenteredAround(targetPos.Position, new Vector2(EntityMenuLookupSize, EntityMenuLookupSize)));

            // Do we have to do FoV checks?
            if ((visibility & MenuVisibility.NoFov) == 0)
            {
                bool Predicate(EntityUid e) => e == player;

                TryComp(player.Value, out ExaminerComponent? examiner);

                foreach (var ent in entitiesUnderMouse)
                {
                    if (_examine.CanExamine(player.Value, targetPos, Predicate, ent.Uid, examiner))
                        _entities.Add(ent.Uid);
                }
            }
            else
            {
                foreach (var ent in entitiesUnderMouse)
                {
                    _entities.Add(ent.Uid);
                }
            }

            if (_entities.Count == 0)
                return false;

            if (visibility == MenuVisibility.All)
            {
                result = new (_entities);
                return true;
            }

            // remove any entities in containers
            if ((visibility & MenuVisibility.InContainer) == 0)
            {
                for (var i = _entities.Count - 1; i >= 0; i--)
                {
                    var entity = _entities[i];

                    if (ContainerSystem.IsInSameOrTransparentContainer(player.Value, entity))
                        continue;

                    _entities.RemoveSwap(i);
                }
            }

            // remove any invisible entities
            if ((visibility & MenuVisibility.Invisible) == 0)
            {
                var spriteQuery = GetEntityQuery<SpriteComponent>();

                for (var i = _entities.Count - 1; i >= 0; i--)
                {
                    var entity = _entities[i];

                    if (!spriteQuery.TryGetComponent(entity, out var spriteComponent) ||
                        !spriteComponent.Visible ||
                        _tagSystem.HasTag(entity, "HideContextMenu"))
                    {
                        _entities.RemoveSwap(i);
                    }
                }
            }

            if (_entities.Count == 0)
                return false;

            result = new(_entities);
            return true;
        }

        /// <summary>
        ///     Ask the server to send back a list of server-side verbs, and for now return an incomplete list of verbs
        ///     (only those defined locally).
        /// </summary>
        public SortedSet<Verb> GetVerbs(NetEntity target, EntityUid user, List<Type> verbTypes, out List<VerbCategory> extraCategories, bool force = false)
        {
            if (!target.IsClientSide())
                RaiseNetworkEvent(new RequestServerVerbsEvent(target, verbTypes, adminRequest: force));

            // Some admin menu interactions will try get verbs for entities that have not yet been sent to the player.
            if (!TryGetEntity(target, out var local))
            {
                extraCategories = new();
                return new();
            }

            return GetLocalVerbs(local.Value, user, verbTypes, out extraCategories, force);
        }


        /// <summary>
        ///     Execute actions associated with the given verb.
        /// </summary>
        /// <remarks>
        ///     Unless this is a client-exclusive verb, this will also tell the server to run the same verb.
        /// </remarks>
        public void ExecuteVerb(EntityUid target, Verb verb)
        {
            ExecuteVerb(GetNetEntity(target), verb);
        }

        /// <summary>
        ///     Execute actions associated with the given verb.
        /// </summary>
        /// <remarks>
        ///     Unless this is a client-exclusive verb, this will also tell the server to run the same verb.
        /// </remarks>
        public void ExecuteVerb(NetEntity target, Verb verb)
        {
            if ( _playerManager.LocalEntity is not {} user)
                return;

            // is this verb actually valid?
            if (verb.Disabled)
            {
                // maybe send an informative pop-up message.
                if (!string.IsNullOrWhiteSpace(verb.Message))
                    _popupSystem.PopupEntity(verb.Message, user);

                return;
            }

            if (verb.ClientExclusive || target.IsClientSide())
                // is this a client exclusive (gui) verb?
                ExecuteVerb(verb, user, GetEntity(target));
            else
                EntityManager.RaisePredictiveEvent(new ExecuteVerbEvent(target, verb));
        }

        private void HandleVerbResponse(VerbsResponseEvent msg)
        {
            OnVerbsResponse?.Invoke(msg);
        }
    }
}
