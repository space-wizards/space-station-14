using Content.Client.Light.Components;
using Content.Client.HUD;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.Graphics;
using Content.Shared.Actions;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using System.Threading;
using Content.Shared.MobState.State;
using Content.Shared.Doors.Components;
using Content.Shared.Light.Component;

namespace Content.Client.Light;

[UsedImplicitly]
public sealed class GraphicTogglesSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly ILightManager _lightingManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GraphicTogglesComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<GraphicTogglesComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<GraphicTogglesComponent, PlayerAttachedEvent>(OnComponentPlayerAttach);
        SubscribeLocalEvent<GraphicTogglesComponent, PlayerDetachedEvent>(OnComponentPlayerDetach);

        SubscribeLocalEvent<GraphicTogglesComponent, ToggleFoVActionEvent>(OnToggleFoVActionEvent);
        SubscribeLocalEvent<GraphicTogglesComponent, ToggleShadowsActionEvent>(OnToggleShadowsActionEvent);
        SubscribeLocalEvent<GraphicTogglesComponent, ToggleLightingActionEvent>(OnToggleLightingActionEvent);
    }
    private void OnComponentStartup(EntityUid uid, GraphicTogglesComponent component, ComponentStartup args)
    {
        if (component.ToggleFoV != null && component.FoVAction == true)
            _actionsSystem.AddAction(uid, component.ToggleFoV, null);

        if (component.ToggleShadows != null && component.ShadowsAction == true)
            _actionsSystem.AddAction(uid, component.ToggleShadows, null);

        if (component.ToggleLighting != null && component.LightingAction == true)
            _actionsSystem.AddAction(uid, component.ToggleLighting, null);
    }
    private void OnComponentShutdown(EntityUid uid, GraphicTogglesComponent component, ComponentShutdown args)
    {
        GraphicsTogglesChecks();
    }
    private void OnComponentPlayerAttach(EntityUid uid, GraphicTogglesComponent component, PlayerAttachedEvent playerAttachedEvent)
    {
    }
    private void OnComponentPlayerDetach(EntityUid uid, GraphicTogglesComponent component, PlayerDetachedEvent args)
    {
        GraphicsTogglesChecks();
    }

    private void OnToggleFoVActionEvent(EntityUid uid, GraphicTogglesComponent component, ToggleFoVActionEvent args)
    {
        if (args.Handled)
            return;

        _eyeManager.CurrentEye.DrawFov = !_eyeManager.CurrentEye.DrawFov;
        args.Handled = true;
    }
    private void OnToggleShadowsActionEvent(EntityUid uid, GraphicTogglesComponent component, ToggleShadowsActionEvent args)
    {
        if (args.Handled)
            return;

    _lightingManager.DrawShadows = !_lightingManager.DrawShadows;
        args.Handled = true;
    }
    private void OnToggleLightingActionEvent(EntityUid uid, GraphicTogglesComponent component, ToggleLightingActionEvent args)
    {
        if (args.Handled)
            return;

        _lightingManager.Enabled = !_lightingManager.Enabled;
        args.Handled = true;
    }
    private void GraphicsTogglesChecks()
    {
        if (_eyeManager.CurrentEye.DrawFov == false || _lightingManager.DrawShadows == false || _lightingManager.Enabled == false)
        {
            _eyeManager.CurrentEye.DrawFov = true;
            _lightingManager.DrawShadows = true;
            _lightingManager.Enabled = true;
        }
    }
}
