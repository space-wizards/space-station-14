#nullable enable
using Content.Server.Administration;
using Content.Server.Eui;
using Content.Server.GameObjects.Components.GUI;
using Content.Shared.Administration;
using Content.Shared.Chemistry;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSolutionContainerComponent))]
    public class SolutionContainerComponent : SharedSolutionContainerComponent
    {
        /// <summary>
        ///     Transfers solution from the held container to the target container.
        /// </summary>
        [Verb]
        private sealed class FillTargetVerb : Verb<SolutionContainerComponent>
        {
            protected override void GetData(IEntity user, SolutionContainerComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) ||
                    !user.TryGetComponent<HandsComponent>(out var hands) ||
                    hands.GetActiveHand == null ||
                    hands.GetActiveHand.Owner == component.Owner ||
                    !hands.GetActiveHand.Owner.TryGetComponent<SolutionContainerComponent>(out var solution) ||
                    !solution.CanRemoveSolutions ||
                    !component.CanAddSolutions)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                var heldEntityName = hands.GetActiveHand.Owner?.Prototype?.Name ?? "<Item>";
                var myName = component.Owner.Prototype?.Name ?? "<Item>";

                var locHeldEntityName = Loc.GetString(heldEntityName);
                var locMyName = Loc.GetString(myName);

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Transfer liquid from [{0}] to [{1}].", locHeldEntityName, locMyName);
            }

            protected override void Activate(IEntity user, SolutionContainerComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands) || hands.GetActiveHand == null)
                {
                    return;
                }

                if (!hands.GetActiveHand.Owner.TryGetComponent<SolutionContainerComponent>(out var handSolutionComp) ||
                    !handSolutionComp.CanRemoveSolutions ||
                    !component.CanAddSolutions)
                {
                    return;
                }

                var transferQuantity = ReagentUnit.Min(component.MaxVolume - component.CurrentVolume,
                    handSolutionComp.CurrentVolume, ReagentUnit.New(10));

                if (transferQuantity <= 0)
                {
                    return;
                }

                var transferSolution = handSolutionComp.SplitSolution(transferQuantity);
                component.TryAddSolution(transferSolution);
            }
        }

        /// <summary>
        ///     Transfers solution from a target container to the held container.
        /// </summary>
        [Verb]
        private sealed class EmptyTargetVerb : Verb<SolutionContainerComponent>
        {
            protected override void GetData(IEntity user, SolutionContainerComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) ||
                    !user.TryGetComponent<HandsComponent>(out var hands) ||
                    hands.GetActiveHand == null ||
                    hands.GetActiveHand.Owner == component.Owner ||
                    !hands.GetActiveHand.Owner.TryGetComponent<SolutionContainerComponent>(out var solution) ||
                    !solution.CanAddSolutions ||
                    !component.CanRemoveSolutions)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                var heldEntityName = hands.GetActiveHand.Owner?.Prototype?.Name ?? "<Item>";
                var myName = component.Owner.Prototype?.Name ?? "<Item>";

                var locHeldEntityName = Loc.GetString(heldEntityName);
                var locMyName = Loc.GetString(myName);

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Transfer liquid from [{0}] to [{1}].", locMyName, locHeldEntityName);
                return;
            }

            protected override void Activate(IEntity user, SolutionContainerComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands) || hands.GetActiveHand == null)
                {
                    return;
                }

                if (!hands.GetActiveHand.Owner.TryGetComponent<SolutionContainerComponent>(out var handSolutionComp) ||
                    !handSolutionComp.CanAddSolutions ||
                    !component.CanRemoveSolutions)
                {
                    return;
                }

                var transferQuantity = ReagentUnit.Min(handSolutionComp.MaxVolume - handSolutionComp.CurrentVolume,
                    component.CurrentVolume, ReagentUnit.New(10));

                if (transferQuantity <= 0)
                {
                    return;
                }

                var transferSolution = component.SplitSolution(transferQuantity);
                handSolutionComp.TryAddSolution(transferSolution);
            }
        }

        [Verb]
        private sealed class AdminAddReagentVerb : Verb<SolutionContainerComponent>
        {
            private const AdminFlags ReqFlags = AdminFlags.Fun;

            protected override void GetData(IEntity user, SolutionContainerComponent component, VerbData data)
            {
                data.Text = Loc.GetString("Add Reagent...");
                data.CategoryData = VerbCategories.Debug;
                data.Visibility = VerbVisibility.Invisible;

                var adminManager = IoCManager.Resolve<IAdminManager>();

                if (user.TryGetComponent<IActorComponent>(out var player))
                {
                    if (adminManager.HasAdminFlag(player.playerSession, ReqFlags))
                    {
                        data.Visibility = VerbVisibility.Visible;
                    }
                }
            }

            protected override void Activate(IEntity user, SolutionContainerComponent component)
            {
                var groupController = IoCManager.Resolve<IAdminManager>();
                if (user.TryGetComponent<IActorComponent>(out var player))
                {
                    if (groupController.HasAdminFlag(player.playerSession, ReqFlags))
                        OpenAddReagentMenu(player.playerSession, component);
                }
            }

            private static void OpenAddReagentMenu(IPlayerSession player, SolutionContainerComponent comp)
            {
                var euiMgr = IoCManager.Resolve<EuiManager>();
                euiMgr.OpenEui(new AdminAddReagentEui(comp), player);
            }

            private sealed class AdminAddReagentEui : BaseEui
            {
                private readonly SolutionContainerComponent _target;
                [Dependency] private readonly IAdminManager _adminManager = default!;

                public AdminAddReagentEui(SolutionContainerComponent target)
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
                    return new AdminAddReagentEuiState
                    {
                        CurVolume = _target.CurrentVolume,
                        MaxVolume = _target.MaxVolume
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

                            _target.TryAddReagent(doAdd.ReagentId, doAdd.Amount, out _);
                            StateDirty();

                            if (doAdd.CloseAfter)
                                Close();

                            break;
                    }
                }
            }
        }
    }
}
