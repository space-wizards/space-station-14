using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Popups;

namespace Content.Shared.Nutrition.EntitySystems;

public abstract partial class SharedSmokingSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private OpenableSystem _openable = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeSharedCigars();
    }
}
