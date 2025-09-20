using Content.Shared.Silicons.StationAi;
using Robust.Client.GameObjects;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiFixerConsoleSystem : SharedStationAiFixerConsoleSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiFixerConsoleComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<StationAiFixerConsoleComponent> ent, ref AppearanceChangeEvent args)
    {
        if (_userInterface.TryGetOpenUi(ent.Owner, StationAiFixerConsoleUiKey.Key, out var bui))
        {
            bui?.Update<StationAiFixerConsoleBoundUserInterfaceState>();
        }
    }
}
