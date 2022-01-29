using Content.Server.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        InitializeHypospray();
        InitializeInjector();
    }
}
