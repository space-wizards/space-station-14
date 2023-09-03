using System.Linq;
using Content.Shared.Spider;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Spider;

public abstract partial class SharedSpiderSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderWebObjectComponent, ComponentStartup>(OnWebStartup);
        SubscribeLocalEvent<SpiderComponent, ComponentStartup>(OnSpiderStartup);
    }

    private void OnSpiderStartup(EntityUid uid, SpiderComponent component, ComponentStartup args)
    {
        var netAction = new InstantAction(_proto.Index<InstantActionPrototype>(component.WebActionName));
        _action.AddAction(uid, netAction, null);
    }

    private void OnWebStartup(EntityUid uid, SpiderWebObjectComponent component, ComponentStartup args)
    {
        _appearance.SetData(uid, SpiderWebVisuals.Variant, _robustRandom.Next(1, 3));
    }
}
