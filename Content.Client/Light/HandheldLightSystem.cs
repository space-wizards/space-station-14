using Content.Client.Items;
using Content.Client.Light.Components;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Toggleable;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Light;

public sealed class HandheldLightSystem : SharedHandheldLightSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldLightComponent, ItemStatusCollectMessage>(OnGetStatusControl);
        SubscribeLocalEvent<HandheldLightComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private static void OnGetStatusControl(EntityUid uid, HandheldLightComponent component, ItemStatusCollectMessage args)
    {
        args.Controls.Add(new HandheldLightStatus(component));
    }

    private void OnAppearanceChange(EntityUid uid, HandheldLightComponent? component, ref AppearanceChangeEvent args)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        if (!_appearance.TryGetData<bool>(uid, ToggleableLightVisuals.Enabled, out var enabled, args.Component))
        {
            return;
        }

        if (!_appearance.TryGetData<HandheldLightPowerStates>(uid, HandheldLightVisuals.Power, out var state, args.Component))
        {
            return;
        }

        if (TryComp<LightBehaviourComponent>(uid, out var lightBehaviour))
        {
            // Reset any running behaviour to reset the animated properties back to the original value, to avoid conflicts between resets
            if (lightBehaviour.HasRunningBehaviours())
            {
                lightBehaviour.StopLightBehaviour(resetToOriginalSettings: true);
            }

            if (!enabled)
            {
                return;
            }

            switch (state)
            {
                case HandheldLightPowerStates.FullPower:
                    break; // We just needed to reset all behaviours
                case HandheldLightPowerStates.LowPower:
                    lightBehaviour.StartLightBehaviour(component.RadiatingBehaviourId);
                    break;
                case HandheldLightPowerStates.Dying:
                    lightBehaviour.StartLightBehaviour(component.BlinkingBehaviourId);
                    break;
            }
        }
    }
}
