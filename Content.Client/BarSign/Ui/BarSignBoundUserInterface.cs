using System.Linq;
using Content.Shared.BarSign;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Client.BarSign.Ui;

[UsedImplicitly]
public sealed class BarSignBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private BarSignMenu? _menu;

    protected override void Open()
    {
        base.Open();

        var sign = EntMan.GetComponentOrNull<BarSignComponent>(Owner)?.Current is { } current
            ? _prototype.Index(current)
            : null;
        var allSigns =Shared.BarSign.BarSignSystem.GetAllBarSigns(_prototype)
            .OrderBy(p => Loc.GetString(p.Name))
            .ToList();
        _menu = new(Owner, sign, allSigns);

        _menu.OnSignSelected += id =>
        {
            SendMessage(new SetBarSignMessage(id));
        };

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not BarSignBuiState buiState)
            return;

        if (_prototype.TryIndex(buiState.Sign, out var signPrototype))
            _menu?.UpdateState(signPrototype);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}

