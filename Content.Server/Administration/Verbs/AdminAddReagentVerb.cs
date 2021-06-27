using Content.Server.Administration.Managers;
using Content.Server.Chemistry.Components;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.Eui;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

#nullable enable

namespace Content.Server.Administration.Verbs
{
    [GlobalVerb]
    internal sealed class AdminAddReagentVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        private const AdminFlags ReqFlags = AdminFlags.Fun;

        private static void OpenAddReagentMenu(IPlayerSession player, IEntity target)
        {
            var euiMgr = IoCManager.Resolve<EuiManager>();
            euiMgr.OpenEui(new AdminAddReagentEui(target), player);
        }

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            // ISolutionInteractionsComponent doesn't exactly have an interface for "admin tries to refill this", so...
            // Still have a path for SolutionContainerComponent in case it doesn't allow direct refilling.
            if (!target.HasComponent<SolutionContainerComponent>()
                && !(target.TryGetComponent(out ISolutionInteractionsComponent? interactions)
                     && interactions.CanInject))
            {
                data.Visibility = VerbVisibility.Invisible;
                return;
            }

            data.Text = Loc.GetString("admin-add-reagent-verb-get-data-text");
            data.CategoryData = VerbCategories.Debug;
            data.Visibility = VerbVisibility.Invisible;

            var adminManager = IoCManager.Resolve<IAdminManager>();

            if (user.TryGetComponent<ActorComponent>(out var player))
            {
                if (adminManager.HasAdminFlag(player.PlayerSession, ReqFlags))
                {
                    data.Visibility = VerbVisibility.Visible;
                }
            }
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IAdminManager>();
            if (user.TryGetComponent<ActorComponent>(out var player))
            {
                if (groupController.HasAdminFlag(player.PlayerSession, ReqFlags))
                    OpenAddReagentMenu(player.PlayerSession, target);
            }
        }

        private sealed class AdminAddReagentEui : BaseEui
        {
            private readonly IEntity _target;
            [Dependency] private readonly IAdminManager _adminManager = default!;

            public AdminAddReagentEui(IEntity target)
            {
                _target = target;

                IoCManager.InjectDependencies(this);
            }

            public override void Opened()
            {
                StateDirty();
            }

            public override EuiStateBase GetNewState()
            {
                if (_target.TryGetComponent(out SolutionContainerComponent? container))
                {
                    return new AdminAddReagentEuiState
                    {
                        CurVolume = container.CurrentVolume,
                        MaxVolume = container.MaxVolume
                    };
                }

                if (_target.TryGetComponent(out ISolutionInteractionsComponent? interactions))
                {
                    return new AdminAddReagentEuiState
                    {
                        // We don't exactly have an absolute total volume so good enough.
                        CurVolume = ReagentUnit.Zero,
                        MaxVolume = interactions.InjectSpaceAvailable
                    };
                }

                return new AdminAddReagentEuiState
                {
                    CurVolume = ReagentUnit.Zero,
                    MaxVolume = ReagentUnit.Zero
                };
            }

            public override void HandleMessage(EuiMessageBase msg)
            {
                switch (msg)
                {
                    case AdminAddReagentEuiMsg.Close:
                        Close();
                        break;
                    case AdminAddReagentEuiMsg.DoAdd doAdd:
                        // Double check that user wasn't de-adminned in the mean time...
                        // Or the target was deleted.
                        if (!_adminManager.HasAdminFlag(Player, ReqFlags) || _target.Deleted)
                        {
                            Close();
                            return;
                        }

                        var id = doAdd.ReagentId;
                        var amount = doAdd.Amount;

                        if (_target.TryGetComponent(out SolutionContainerComponent? container))
                        {
                            container.TryAddReagent(id, amount, out _);
                        }
                        else if (_target.TryGetComponent(out ISolutionInteractionsComponent? interactions))
                        {
                            var solution = new Solution(id, amount);
                            interactions.Inject(solution);
                        }

                        StateDirty();

                        if (doAdd.CloseAfter)
                            Close();

                        break;
                }
            }
        }
    }
}
