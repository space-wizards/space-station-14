using Robust.Client.GameObjects;
using Content.Shared.Smoking;

namespace Content.Client.Smoking;

public sealed class BurnStateVisualizerSystem : VisualizerSystem<BurnStateVisualsComponent>
{

    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BurnStateVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, BurnStateVisualsComponent component, ComponentInit args)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void OnAppearanceChange(EntityUid uid, BurnStateVisualsComponent component, ref AppearanceChangeEvent args)
    {

        if (!_entMan.TryGetComponent(uid, out SpriteComponent? sprite))
            return;

        if (!_appearance.TryGetData<SmokableState>(uid, SmokingVisuals.Smoking, out var burnState))
            return;

        var state = burnState switch
        {
            SmokableState.Lit => component._litIcon,
                SmokableState.Burnt => component._burntIcon,
                _ => component._unlitIcon
        };

        sprite?.LayerSetState(0, state);
    }
}

