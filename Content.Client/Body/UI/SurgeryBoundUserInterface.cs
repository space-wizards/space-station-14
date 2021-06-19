using System.Collections.Generic;
using Content.Shared.Body.Surgery.Operation;
using Content.Shared.Body.Surgery.Target;
using Content.Shared.Body.Surgery.UI;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.Body.UI
{
    [UsedImplicitly]
    public class SurgeryBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables] private SurgeryWindow? _window;

        public SurgeryBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) { }

        protected override void Open()
        {
            base.Open();

            _window = new SurgeryWindow();
            _window.OnClose += Close;
            _window.OpenCentered();
            _window.OperationSelected += OnOperationSelected;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_window == null || state is not SurgeryUIState uiState)
            {
                return;
            }

            var targets = new Dictionary<IEntity, IEnumerable<SurgeryOperationPrototype>>();

            foreach (var id in uiState.Entities)
            {
                if (!_entityManager.TryGetEntity(id, out var entity))
                {
                    continue;
                }

                if (!entity.TryGetComponent(out SurgeryTargetComponent? target))
                {
                    continue;
                }

                targets.Add(entity, target.PossibleSurgeries);
            }

            _window.SetTargets(targets);
        }

        private void OnOperationSelected(IEntity target, string operationId)
        {
            var msg = new SurgeryOpPartSelectUIMsg(target.Uid, operationId);
            SendMessage(msg);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}
