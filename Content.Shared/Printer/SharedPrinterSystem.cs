using Content.Shared.Printer;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Shared.Printer;

public sealed class SharedPrinterSystem : EntitySystem
{
    
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrinterPaperStuck>(OnPaperStuck);
        SubscribeLocalEvent<PrinterPaperInteractStuck>(OnPaperInteractStuck);
    }

    private void OnPaperStuck(PrinterPaperStuck args)
    {
        if(_playerManager.LocalSession == null)
            return;
        if(_playerManager.LocalSession.AttachedEntity == null)
            return;
        _popupSystem.PopupClient("Unstuck the paper first!", args.Printer, _playerManager.LocalSession.AttachedEntity.Value);
    }

    private void OnPaperInteractStuck(PrinterPaperInteractStuck args)
    {
        if(_playerManager.LocalSession == null)
            return;
        if(_playerManager.LocalSession.AttachedEntity == null)
            return;
        _popupSystem.PopupClient("You touched the tray and the paper got stuck!", args.Printer, _playerManager.LocalSession.AttachedEntity.Value);
    }
}