using Content.Shared.Emp;
using Content.Shared.Light.Components;
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

public abstract class SharedSurveillanceCameraSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceCameraComponent, GetVerbsEvent<AlternativeVerb>>(AddVerbs);
        SubscribeLocalEvent<SurveillanceCameraComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<SurveillanceCameraComponent, EmpDisabledRemovedEvent>(OnEmpDisabledRemoved);
    }

    private void AddVerbs(EntityUid uid, SurveillanceCameraComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanComplexInteract)
            return;

        if (component.NameSet && component.NetworkSet)
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("surveillance-camera-setup"),
            Act = () => OpenSetupInterface(uid, args.User, component)
        };
        args.Verbs.Add(verb);
    }

    private void OnEmpPulse(EntityUid uid, SurveillanceCameraComponent component, ref EmpPulseEvent args)
    {
        if (component.Active)
        {
            args.Affected = true;
            args.Disabled = true;
            SetActive(uid, false);
        }
    }

    public void UpdateVisuals(EntityUid uid, SurveillanceCameraComponent? component = null, AppearanceComponent? appearance = null)
    {
        Log.Debug("Resolving");
        // Don't log missing, because otherwise tests fail.
        if (!Resolve(uid, ref component, ref appearance, false))
        {
            return;
        }

        var key = SurveillanceCameraVisuals.Disabled;

        Log.Debug("Checking active");
        if (component.Active)
        {
            key = SurveillanceCameraVisuals.Active;
            Log.Debug("Checking active");
        }

        Log.Debug("Checking in use");
        if (component.ActiveViewers.Count > 0 || component.ActiveMonitors.Count > 0 || TryComp<CameraLightOnCollideComponent>(uid, out var light) && light.Enabled)
        {
            key = SurveillanceCameraVisuals.InUse;
            Log.Debug("In use");
        }

        Log.Debug($"Setting {key} to {appearance}");
        _appearance.SetData(uid, SurveillanceCameraVisualsKey.Key, key, appearance);
    }

    private void OnEmpDisabledRemoved(EntityUid uid, SurveillanceCameraComponent component, ref EmpDisabledRemovedEvent args)
    {
        SetActive(uid, true);
    }

    // TODO: predict the rest of the server side system
    public virtual void SetActive(EntityUid camera, bool setting, SurveillanceCameraComponent? component = null) { }

    protected virtual void OpenSetupInterface(EntityUid uid, EntityUid player, SurveillanceCameraComponent? camera = null) { }
}

[Serializable, NetSerializable]
public enum SurveillanceCameraVisualsKey : byte
{
    Key,
    Layer
}

[Serializable, NetSerializable]
public enum SurveillanceCameraVisuals : byte
{
    Active,
    InUse,
    Disabled,
    // Reserved for future use
    Xray,
    Emp
}
