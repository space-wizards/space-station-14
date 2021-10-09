using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.ContextMenu.UI;
using Content.Client.Popups;
using Content.Client.Verbs.UI;
using Content.Shared.GameTicking;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.Verbs
{
    [UsedImplicitly]
    public sealed class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public EntityMenuPresenter EntityMenu = default!;
        public VerbMenuPresenter VerbMenu = default!;

        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        /// <summary>
        ///     These flags determine what entities the user can see on the context menu.
        /// </summary>
        /// <remarks>
        ///     Verb execution will only be affected if the server also agrees that this player can see the targeted
        ///     entity.
        /// </remarks>
        public MenuVisibility Visibility;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeNetworkEvent<VerbsResponseEvent>(HandleVerbResponse);
            SubscribeNetworkEvent<SetSeeAllContextEvent>(SetSeeAllContext);

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

        private void SetSeeAllContext(SetSeeAllContextEvent args)
        {
            Visibility = args.MenuVisibility;
        }

        public void CloseAllMenus()
        {
            EntityMenu.Close();
            VerbMenu.Close();
        }

        public bool TryGetEntityMenuEntities(MapCoordinates targetPos, [NotNullWhen(true)] out List<IEntity>? menuEntities)
        {
            menuEntities = null;

            var player = _playerManager.LocalPlayer?.ControlledEntity;

            if (player == null)
                return false;

            var visibility = Visibility;
            if (!_eyeManager.CurrentEye.DrawFov)
                visibility |= MenuVisibility.NoFoV;

            if (!TryGetEntityMenuEntities(player, targetPos, out var entities, visibility, false))
                return false;

            if ((Visibility & MenuVisibility.Invisible) == MenuVisibility.Invisible)
            {
                menuEntities = entities;
                return true;
            }

            // Client specific visibility checks: Do these entities have valid sprites?
            foreach (var entity in entities.ToList())
            {
                if (!EntityManager.TryGetComponent(entity.Uid, out ISpriteComponent? spriteComponent) ||
                    !spriteComponent.Visible)
                {
                    entities.Remove(entity);
                }
            }

            if (entities.Count == 0)
                return false;

            menuEntities = entities;
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
}
