using Content.Shared.Dragon;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Dragon;

public sealed class DragonSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DragonRiftComponent, ComponentHandleState>(OnRiftHandleState);
    }

    private void OnRiftHandleState(EntityUid uid, DragonRiftComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not DragonRiftComponentState state)
            return;

        if (component.State == state.State) return;

        component.State = state.State;
        TryComp<SpriteComponent>(uid, out var sprite);
        TryComp<PointLightComponent>(uid, out var light);

        if (sprite == null && light == null)
            return;

        switch (state.State)
        {
            case DragonRiftState.Charging:
                sprite?.LayerSetColor(0, Color.FromHex("#569fff"));

                if (light != null)
                {
                    _lights.SetColor(uid, Color.FromHex("#366db5"), light);
                }
                break;
            case DragonRiftState.AlmostFinished:
                sprite?.LayerSetColor(0, Color.FromHex("#cf4cff"));

                if (light != null)
                {
                    _lights.SetColor(uid, Color.FromHex("#9e2fc1"), light);
                }
                break;
            case DragonRiftState.Finished:
                sprite?.LayerSetColor(0, Color.FromHex("#edbc36"));

                if (light != null)
                {
                    _lights.SetColor(uid, Color.FromHex("#cbaf20"), light);
                }
                break;
        }
    }
}
