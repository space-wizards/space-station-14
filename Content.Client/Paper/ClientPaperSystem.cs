using Content.Client.Paper.UI;
using Content.Shared.Paper;
using Robust.Client.GameObjects;

namespace Content.Client.Paper;

public sealed partial class ClientPaperSystem : PaperSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    protected override void SignatureUpdateUI(Entity<PaperSignerComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, SignatureUiKey.Key, out var bui)
            && bui is SignatureBoundUserInterface cBui)
        {
            cBui.Reload();
        }
    }
}
