using Content.Shared.Emp;
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

public abstract partial class SharedSurveillanceCameraSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceCameraComponent, GetVerbsEvent<AlternativeVerb>>(AddVerbs);
        SubscribeLocalEvent<SurveillanceCameraComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<SurveillanceCameraComponent, EmpDisabledRemovedEvent>(OnEmpDisabledRemoved);

        InitializeCollide();
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

    protected void UpdateVisuals(EntityUid uid, SurveillanceCameraComponent? component = null, AppearanceComponent? appearance = null)
    {
        Log.Debug("Resolving");
        // Don't log missing, because otherwise tests fail.
        if (!Resolve(uid, ref component, ref appearance, false))
        {
            return;
        }

        var key = SurveillanceCameraVisuals.Disabled;

        if (component.Active)
        {
            key = SurveillanceCameraVisuals.Active;
        }

        if (component.ActiveViewers.Count > 0 || component.ActiveMonitors.Count > 0)
        {
            key = SurveillanceCameraVisuals.InUse;
        }

        var ev = new SurveillanceCameraGetOverrideAppearanceEvent();
        RaiseLocalEvent(uid, ref ev);

        if (ev.State != null)
            key = ev.State.Value;

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

/// <summary>
/// Raised on a camera entity to find a state to override its visuals with.
/// </summary>
/// <param name="State">The state to override the appearance with.</param>
[ByRefEvent]
public record struct SurveillanceCameraGetOverrideAppearanceEvent(SurveillanceCameraVisuals? State = null);
