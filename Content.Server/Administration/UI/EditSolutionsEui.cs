using Content.Server.Administration.Systems;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Systems;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Administration.UI
{
    /// <summary>
    ///     Admin Eui for displaying and editing the reagents in a solution.
    /// </summary>
    [UsedImplicitly]
    public sealed class EditSolutionsEui : BaseEui
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        private readonly SharedSolutionSystem _solutionSystem = default!;
        public readonly EntityUid Target;

        public EditSolutionsEui(EntityUid entity)
        {
            IoCManager.InjectDependencies(this);
            _solutionSystem = _entityManager.System<SharedSolutionSystem>();
            Target = entity;
        }

        public override void Opened()
        {
            base.Opened();
            StateDirty();
        }

        public override void Closed()
        {
            base.Closed();
            _entityManager.System<AdminVerbSystem>().OnEditSolutionsEuiClosed(Player, this);
        }

        public override EuiStateBase GetNewState()
        {
            List<(string Name, NetEntity Solution)>? netSolutions;

            if (_entityManager.TryGetComponent(Target, out SolutionHolderComponent? holder) && holder.SolutionIds.Count > 0)
            {
                netSolutions = new();
                foreach (var (solEnt, solComp) in _solutionSystem.EnumerateSolutions((Target, holder)))
                {
                    if (!_entityManager.TryGetNetEntity(solEnt, out var netSolution))
                        continue;

                    netSolutions.Add((solComp.Name, netSolution.Value));
                }
            }
            else
                netSolutions = null;

            return new EditSolutionsEuiState(_entityManager.GetNetEntity(Target), netSolutions, _gameTiming.CurTick);
        }
    }
}
