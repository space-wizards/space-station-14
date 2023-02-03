using Content.Client.Light.Components;
using Content.Shared.Light.Component;
using Robust.Client.GameObjects;

namespace Content.Client.Light.Visualizers;

public sealed class ExpendableLightVisualizerSystem : VisualizerSystem<ExpendableLightVisualizerComponent>
{
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    protected override void OnAppearanceChange(EntityUid uid, ExpendableLightVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!TryComp<ExpendableLightComponent>(uid, out var expendableLight))
            return;

        if (AppearanceSystem.TryGetData(uid, ExpendableLightVisuals.Behavior, out string lightBehaviourID, args.Component))
        {
            if (TryComp<LightBehaviourComponent>(uid, out var lightBehaviour))
            {
                lightBehaviour.StopLightBehaviour();

                if (lightBehaviourID != string.Empty)
                {
                    lightBehaviour.StartLightBehaviour(lightBehaviourID);
                }
                else if (TryComp<PointLightComponent>(uid, out var light))
                {
                    _pointLightSystem.SetEnabled(uid, false, light);
                }
            }
        }

        if (!AppearanceSystem.TryGetData(uid, ExpendableLightVisuals.State, out ExpendableLightState state, args.Component))
            return;

        switch (state)
        {
            case ExpendableLightState.Lit:
                expendableLight.PlayingStream?.Stop();
                expendableLight.PlayingStream = _audioSystem.PlayPvs(
                    expendableLight.LoopedSound,
                    uid,
                    SharedExpendableLightComponent.LoopedSoundParams);
                if (!string.IsNullOrWhiteSpace(comp.IconStateLit))
                {
                    args.Sprite.LayerSetState(2, comp.IconStateLit);
                    args.Sprite.LayerSetShader(2, "shaded");
                }

                args.Sprite.LayerSetVisible(1, true);

                break;
            case ExpendableLightState.Dead:
                expendableLight.PlayingStream?.Stop();
                if (!string.IsNullOrWhiteSpace(comp.IconStateSpent))
                {
                    args.Sprite.LayerSetState(0, comp.IconStateSpent);
                    args.Sprite.LayerSetShader(0, "shaded");
                }

                args.Sprite.LayerSetVisible(1, false);
                break;
        }
    }
}
