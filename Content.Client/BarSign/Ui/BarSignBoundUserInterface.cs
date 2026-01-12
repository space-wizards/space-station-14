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
        var allSigns = BarSignSystem.GetAllBarSigns(_prototype)
            .OrderBy(p => Loc.GetString(p.Name))
            .ToList();
        _menu = new(sign, allSigns);

        _menu.OnSignSelected += id =>
        {
            SendPredictedMessage(new SetBarSignMessage(id));
        };

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    public override void Update()
    {
        if (!EntMan.TryGetComponent<BarSignComponent>(Owner, out var signComp))
            return;

        if (_prototype.Resolve(signComp.Current, out var signPrototype))
            _menu?.UpdateState(signPrototype);
    }
}

