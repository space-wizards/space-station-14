using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Examine;
using Content.Client.Gameplay;
using Content.Client.Popups;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using JetBrains.Annotations;
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
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        /// <summary>
        ///     When a user right clicks somewhere, how large is the box we use to get entities for the context menu?
        /// </summary>
        public const float EntityMenuLookupSize = 0.25f;

        [Dependency] private readonly IEyeManager _eyeManager = default!;

        /// <summary>
        ///     These flags determine what entities the user can see on the context menu.
        /// </summary>
        public MenuVisibility Visibility;

        public Action<VerbsResponseEvent>? OnVerbsResponse;

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
            List<EntityUid> entities;
            var examineFlags = LookupFlags.All & ~LookupFlags.Sensors;

            // Do we have to do FoV checks?
            if ((visibility & MenuVisibility.NoFov) == 0)
            {
                var entitiesUnderMouse = gameScreenBase.GetClickableEntities(targetPos).ToHashSet();
                bool Predicate(EntityUid e) => e == player || entitiesUnderMouse.Contains(e);

                TryComp(player.Value, out ExaminerComponent? examiner);

                entities = new();
                foreach (var ent in _entityLookup.GetEntitiesInRange(targetPos, EntityMenuLookupSize, flags: examineFlags))
                {
                    if (_examine.CanExamine(player.Value, targetPos, Predicate, ent, examiner))
                        entities.Add(ent);
                }
            }
            else
            {
                entities = _entityLookup.GetEntitiesInRange(targetPos, EntityMenuLookupSize, flags: examineFlags).ToList();
            }

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
                for (var i = entities.Count - 1; i >= 0; i--)
                {
                    var entity = entities[i];

                    if (ContainerSystem.IsInSameOrTransparentContainer(player.Value, entity))
                        continue;

                    entities.RemoveSwap(i);
                }
            }

            // remove any invisible entities
            if ((visibility & MenuVisibility.Invisible) == 0)
            {
                var spriteQuery = GetEntityQuery<SpriteComponent>();

                for (var i = entities.Count - 1; i >= 0; i--)
                {
                    var entity = entities[i];

                    if (!spriteQuery.TryGetComponent(entity, out var spriteComponent) ||
                        !spriteComponent.Visible ||
                        _tagSystem.HasTag(entity, "HideContextMenu"))
                    {
                        entities.RemoveSwap(i);
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
