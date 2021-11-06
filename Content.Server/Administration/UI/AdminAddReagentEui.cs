using Content.Server.Administration.Managers;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Eui;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.UI
{
    public sealed class AdminAddReagentEui : BaseEui
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
            if (EntitySystem.Get<SolutionContainerSystem>()
                .TryGetSolution(_target.Uid, "default", out var container))
            {
                return new AdminAddReagentEuiState
                {
                    CurVolume = container.CurrentVolume,
                    MaxVolume = container.MaxVolume
                };
            }

            return new AdminAddReagentEuiState
            {
                CurVolume = FixedPoint2.Zero,
                MaxVolume = FixedPoint2.Zero
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
                    if (!_adminManager.HasAdminFlag(Player, AdminFlags.Fun) || _target.Deleted)
                    {
                        Close();
                        return;
                    }

                    var id = doAdd.ReagentId;
                    var amount = doAdd.Amount;
                    var solutionsSys = EntitySystem.Get<SolutionContainerSystem>();

                    if (_target.TryGetComponent(out InjectableSolutionComponent? injectable)
                        && solutionsSys.TryGetSolution(_target.Uid, injectable.Name, out var targetSolution))
                    {
                        var solution = new Solution(id, amount);
                        solutionsSys.Inject(_target.Uid, targetSolution, solution);
                    }
                    else
                    {
                        //TODO decide how to find the solution
                        if (solutionsSys.TryGetSolution(_target.Uid, "default", out var solution))
                        {
                            solutionsSys.TryAddReagent(_target.Uid,solution, id, amount, out _);
                        }
                    }

                    StateDirty();

                    if (doAdd.CloseAfter)
                        Close();

                    break;
            }
        }
    }
}
