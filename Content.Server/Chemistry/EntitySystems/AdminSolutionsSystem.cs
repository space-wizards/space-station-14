using System.Collections.Generic;
using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Ghost.Roles
{
    /// <summary>
    ///     This class exists to handle the "manage solutions" admin UI's.
    /// </summary>
    /// <remarks>
    ///     If SolutionContainerSystem is moved to server from shred, maybe this should be merged
    /// </remarks>
    [UsedImplicitly]
    public class AdminSolutionsSystem : EntitySystem
    {
        [Dependency] private readonly EuiManager _euiManager = default!;

        private readonly Dictionary<IPlayerSession, EditSolutionsEui> _openUis = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<SolutionContainerManagerComponent, SolutionChangedEvent>(OnSolutionChanged);
        }

        private void OnSolutionChanged(EntityUid uid, SolutionContainerManagerComponent component, SolutionChangedEvent args)
        {
            foreach (var eui in _openUis.Values)
            {
                if (eui.Target == uid)
                    eui.StateDirty();
            }
        }

        public void OpenEui(IPlayerSession session, EntityUid uid)
        {
            if (session.AttachedEntity == null)
                return;

            if (_openUis.ContainsKey(session))
                _openUis[session].Close();

            var eui = _openUis[session] = new EditSolutionsEui(uid);
            _euiManager.OpenEui(eui, session);
            eui.StateDirty();
        }

        public void CloseEui(IPlayerSession session)
        {
            if (_openUis.Remove(session, out var eui))
            {
                eui?.Close();
            }
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var session in _openUis.Keys)
            {
                CloseEui(session);
            }

            _openUis.Clear();
        }
    }
}
