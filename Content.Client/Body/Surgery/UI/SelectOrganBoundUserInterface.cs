using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery;
using Content.Shared.Body.Surgery.Components;
using Content.Shared.Body.Surgery.Systems;
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

public sealed class SelectOrganBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IEntitySystemManager _systemMan = default!;

    private SelectOrganWindow? _window;
    private EntityUid _target = EntityUid.Invalid;

    public SelectOrganBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new SelectOrganWindow(_entMan);
        _window.OnClose += Close;
        _window.OpenCentered();
        _window.OrganSelected += OnOrganSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not SelectOrganUiState uiState)
        {
            return;
        }

        _target = uiState.Target;

        _window.SetOrgans(uiState.Organs.ToList());
    }

    private void OnOrganSelected(EntityUid organ)
    {
        var msg = new OrganSelectedMessage(_target, organ);
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
