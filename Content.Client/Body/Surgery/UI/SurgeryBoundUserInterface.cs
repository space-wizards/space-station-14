using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery.Components;
using Content.Shared.Body.Surgery.Operation;
using Content.Shared.Body.Surgery.Systems;
using Content.Shared.Body.Surgery.UI;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Linq;

namespace Content.Client.Body.Surgery.UI;

public sealed class SurgeryBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IEntitySystemManager _systemMan = default!;
    private OperationSystem _operation = default!;

    private SurgeryWindow? _window;
    private EntityUid _target = EntityUid.Invalid;
    private EntityUid _user = EntityUid.Invalid;

    public SurgeryBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new SurgeryWindow(_entMan);
        _window.OnClose += Close;
        _window.OpenCentered();
        _window.OperationSelected += OnOperationSelected;

        _operation = _systemMan.GetEntitySystem<OperationSystem>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not SurgeryUIState uiState)
        {
            return;
        }

        _target = uiState.Target;
        _user = uiState.User;

        var targets = new Dictionary<EntityUid, List<SurgeryOperationPrototype>>();

        foreach (var uid in uiState.Entities)
        {
            if (!_entMan.TryGetComponent<BodyPartComponent>(uid, out var part))
            {
                Logger.WarningS("surgery",
                    $"Server sent entity {uid} without a {nameof(BodyPartComponent)} in {nameof(SurgeryUIState)}");
                continue;
            }

            // TODO: possible surgeries *on part*
            targets.Add(uid, _operation.PossibleSurgeries(part.PartType).ToList());
        }

        _window.SetTargets(targets);
    }

    private void OnOperationSelected(EntityUid part, string operation)
    {
        var msg = new OperationSelectedMessage(_target, _user, part, operation);
        SendMessage(msg);
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
