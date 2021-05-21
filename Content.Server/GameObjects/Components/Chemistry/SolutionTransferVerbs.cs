using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration;
using Content.Server.Eui;
using Content.Server.GameObjects.Components.GUI;
using Content.Shared.Administration;
using Content.Shared.Chemistry;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

#nullable enable

namespace Content.Server.GameObjects.Components.Chemistry
{
    internal abstract class SolutionTransferVerbBase : GlobalVerb
    {
        protected static bool GetHeldSolution(
            IEntity holder,
            [NotNullWhen(true)]
            out IEntity? held,
            [NotNullWhen(true)]
            out ISolutionInteractionsComponent? heldSolution)
        {
            if (!holder.TryGetComponent(out HandsComponent? hands)
                || hands.GetActiveHand == null
                || !hands.GetActiveHand.Owner.TryGetComponent(out heldSolution))
            {
                held = null;
                heldSolution = null;
                return false;
            }

            held = heldSolution.Owner;
            return true;
        }
    }

    /// <summary>
    ///     Transfers solution from the held container to the target container.
    /// </summary>
    [GlobalVerb]
    internal sealed class SolutionFillTargetVerb : SolutionTransferVerbBase
    {
        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            if (!target.TryGetComponent(out ISolutionInteractionsComponent? targetSolution) ||
                !ActionBlockerSystem.CanInteract(user) ||
                !GetHeldSolution(user, out var source, out var sourceSolution) ||
                source != target ||
                !sourceSolution.CanDrain ||
                !targetSolution.CanRefill)
            {
                data.Visibility = VerbVisibility.Invisible;
                return;
            }

            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("Transfer liquid from [{0}] to [{1}].", source.Name, target.Name);
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!GetHeldSolution(user, out _, out var handSolutionComp))
            {
                return;
            }

            if (!handSolutionComp.CanDrain ||
                !target.TryGetComponent(out ISolutionInteractionsComponent? targetComp) ||
                !targetComp.CanRefill)
            {
                return;
            }

            var transferQuantity = ReagentUnit.Min(
                targetComp.RefillSpaceAvailable,
                handSolutionComp.DrainAvailable,
                ReagentUnit.New(10));

            if (transferQuantity <= 0)
            {
                return;
            }

            var transferSolution = handSolutionComp.Drain(transferQuantity);
            targetComp.Refill(transferSolution);
        }
    }

    /// <summary>
    ///     Transfers solution from a target container to the held container.
    /// </summary>
    [GlobalVerb]
    internal sealed class SolutionDrainTargetVerb : SolutionTransferVerbBase
    {
        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            if (!target.TryGetComponent(out ISolutionInteractionsComponent? sourceSolution) ||
                !ActionBlockerSystem.CanInteract(user) ||
                !GetHeldSolution(user, out var held, out var targetSolution) ||
                !sourceSolution.CanDrain ||
                !targetSolution.CanRefill)
            {
                data.Visibility = VerbVisibility.Invisible;
                return;
            }

            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("Transfer liquid from [{0}] to [{1}].", held.Name, target.Name);
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!GetHeldSolution(user, out _, out var targetComp))
            {
                return;
            }

            if (!targetComp.CanRefill ||
                !target.TryGetComponent(out ISolutionInteractionsComponent? sourceComp) ||
                !sourceComp.CanDrain)
            {
                return;
            }

            var transferQuantity = ReagentUnit.Min(
                targetComp.RefillSpaceAvailable,
                sourceComp.DrainAvailable,
                ReagentUnit.New(10));

            if (transferQuantity <= 0)
            {
                return;
            }

            var transferSolution = sourceComp.Drain(transferQuantity);
            targetComp.Refill(transferSolution);
        }
    }

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

            data.Text = Loc.GetString("Add Reagent...");
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
