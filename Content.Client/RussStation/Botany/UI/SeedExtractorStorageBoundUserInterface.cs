using Content.Shared.RussStation.Botany;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.RussStation.Botany.UI;

[UsedImplicitly]
public sealed class SeedExtractorStorageBoundUserInterface : BoundUserInterface
{
    private SeedExtractorStorageMenu? _menu;

    public SeedExtractorStorageBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindowCenteredRight<SeedExtractorStorageMenu>();
        _menu.OnTakePressed += groupKey =>
        {
            SendMessage(new SeedExtractorStorageTakeSeedMessage(groupKey));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SeedExtractorStorageUpdateState seedState)
            _menu?.Populate(seedState.Seeds);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _menu?.Dispose();
    }
}
