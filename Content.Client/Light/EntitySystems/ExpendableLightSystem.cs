using Content.Client.Light.Components;
using Content.Shared.Light.Component;
using Robust.Client.GameObjects;

namespace Content.Client.Light.EntitySystems;

public sealed class ExpendableLightSystem : VisualizerSystem<ExpendableLightComponent>
{
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpendableLightComponent, ComponentShutdown>(OnLightShutdown);
    }

    private void OnLightShutdown(EntityUid uid, ExpendableLightComponent component, ComponentShutdown args)
    {
        component.PlayingStream?.Stop();
    }

    protected override void OnAppearanceChange(EntityUid uid, ExpendableLightComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, ExpendableLightVisuals.Behavior, out var lightBehaviourID, args.Component)
        &&  TryComp<LightBehaviourComponent>(uid, out var lightBehaviour))
        {
            lightBehaviour.StopLightBehaviour();

            if (!string.IsNullOrEmpty(lightBehaviourID))
            {
                lightBehaviour.StartLightBehaviour(lightBehaviourID);
            }
            else if (TryComp<PointLightComponent>(uid, out var light))
            {
                _pointLightSystem.SetEnabled(uid, false, light);
            }
        }

        if (!AppearanceSystem.TryGetData<ExpendableLightState>(uid, ExpendableLightVisuals.State, out var state, args.Component))
            return;

        switch (state)
        {
            case ExpendableLightState.Lit:
                comp.PlayingStream?.Stop();
                comp.PlayingStream = _audioSystem.PlayPvs(
                    comp.LoopedSound,
                    uid,
                    SharedExpendableLightComponent.LoopedSoundParams
                );
                if (!string.IsNullOrWhiteSpace(comp.IconStateLit))
                    args.Sprite.LayerSetState(ExpendableLightVisualLayers.Overlay, comp.IconStateLit);
                args.Sprite.LayerSetShader(ExpendableLightVisualLayers.Overlay, comp.SpriteShaderLit ?? "invalidshaderstatebecausethisprocdoesnotacceptnullvaluesandthisistheonlywaytoresetitbacktothedefaultshaderstate");
                args.Sprite.LayerSetVisible(ExpendableLightVisualLayers.Glow, true);
                if (comp.GlowColorLit.HasValue)
                {
                    args.Sprite.LayerSetColor(ExpendableLightVisualLayers.Overlay, comp.GlowColorLit.Value);
                    args.Sprite.LayerSetColor(ExpendableLightVisualLayers.Glow, comp.GlowColorLit.Value);
                }

                break;
            case ExpendableLightState.Dead:
                comp.PlayingStream?.Stop();
                if (!string.IsNullOrWhiteSpace(comp.IconStateSpent))
                    args.Sprite.LayerSetState(ExpendableLightVisualLayers.Overlay, comp.IconStateSpent);
                args.Sprite.LayerSetShader(ExpendableLightVisualLayers.Overlay, comp.SpriteShaderSpent ?? "ireallywishthatIdidn'thavetodothisbutI'mnotannoyedenoughtothinkaboutmakinganengineprjusttofixthis");

                args.Sprite.LayerSetVisible(ExpendableLightVisualLayers.Glow, false);
                break;
        }
    }
}
