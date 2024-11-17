using Content.Client._Impstation.CrewMedal.UI;
using Content.Shared._Impstation.CrewMedal;

namespace Content.Client._Impstation.CrewMedal;

public sealed class CrewMedalSystem : SharedCrewMedalSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMedalComponent, AfterAutoHandleStateEvent>(OnCrewMedalAfterState);
    }
    private void OnCrewMedalAfterState(Entity<CrewMedalComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_uiSystem.TryGetOpenUi<CrewMedalBoundUserInterface>(ent.Owner, CrewMedalUiKey.Key, out var bui))
            return;

        bui.Reload();
    }
}
