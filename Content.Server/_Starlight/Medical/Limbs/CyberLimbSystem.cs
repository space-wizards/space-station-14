using Content.Server.Actions;
using Content.Server.Administration.Systems;
using Content.Server.Hands.Systems;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;

namespace Content.Server._Starlight.Medical.Limbs;
public sealed partial class CyberLimbSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly StarlightEntitySystem _slEnt = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly LimbSystem _limb = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeLimbWithItems();
        InitializeToggleable();
    }
}
