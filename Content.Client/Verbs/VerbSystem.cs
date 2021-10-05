using System.Collections.Generic;
using Content.Client.ContextMenu.UI;
using Content.Client.Popups;
using Content.Client.Verbs.UI;
using Content.Shared.GameTicking;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Verbs
{
    [UsedImplicitly]
    public sealed class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public EntityMenuPresenter EntityMenu = default!;
        public VerbMenuPresenter VerbMenu = default!;

        /// <summary>
        ///     Whether to show all entities on the context menu.
        /// </summary>
        /// <remarks>
        ///     Verb execution will only be affected if the server also agrees that this player can see the target
        ///     entity.
        /// </remarks>
        public bool CanSeeAllContext = false;

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
            CanSeeAllContext = args.CanSeeAllContext;
        }

        public void CloseAllMenus()
        {
            EntityMenu.Close();
            VerbMenu.Close();
        }

        /// <summary>
        ///     Ask the server to send back a list of server-side verbs, and for now return an incomplete list of verbs
        ///     (only those defined locally).
        /// </summary>
        public override Dictionary<VerbType, SortedSet<Verb>> GetVerbs(IEntity target, IEntity user, VerbType verbTypes)
        {
            if (!target.Uid.IsClientSide())
            {
                RaiseNetworkEvent(new RequestServerVerbsEvent(target.Uid, verbTypes));
            }
            
            return base.GetVerbs(target, user, verbTypes);
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
