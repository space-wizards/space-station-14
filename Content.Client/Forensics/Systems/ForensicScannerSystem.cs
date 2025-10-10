using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Systems;


namespace Content.Client.Forensics.Systems;

public sealed class ForensicScannerSystem : SharedForensicScannerSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ForensicScannerComponent, AfterAutoHandleStateEvent>(OnScannerUpdate);
    }

    protected override void UpdateUi(Entity<ForensicScannerComponent> ent)
    {
        if (_uiSystem.TryGetOpenUi(ent.Owner, ForensicScannerUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }

    private void OnScannerUpdate(Entity<ForensicScannerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }
}
