using Content.Shared.BarSign;
using Content.Shared.Power;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Client.BarSign;

public sealed class BarSignSystem : VisualizerSystem<BarSignComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BarSignComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, BarSignComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BarSignComponentState state)
            return;

        component.CurrentSign = state.CurrentSign;
        UpdateAppearance(component);
    }

    protected override void OnAppearanceChange(EntityUid uid, BarSignComponent component, ref AppearanceChangeEvent args)
    {
        UpdateAppearance(component, args.Component, args.Sprite);
    }

    private void UpdateAppearance(BarSignComponent sign, AppearanceComponent? appearance = null, SpriteComponent? sprite = null)
    {
        if (!Resolve(sign.Owner, ref appearance, ref sprite))
            return;

        AppearanceSystem.TryGetData<bool>(sign.Owner, PowerDeviceVisuals.Powered, out var powered, appearance);

        if (powered
            && sign.CurrentSign != null
            && _prototypeManager.TryIndex(sign.CurrentSign, out BarSignPrototype? proto))
        {
            sprite.LayerSetState(0, proto.Icon);
            sprite.LayerSetShader(0, "unshaded");
        }
        else
        {
            sprite.LayerSetState(0, "empty");
            sprite.LayerSetShader(0, null, null);
        }
    }
}
