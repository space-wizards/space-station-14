using Content.Shared.Emp;
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

public abstract partial class SharedSurveillanceCameraSystem : EntitySystem
{
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

    private void OnEmpPulse(Entity<SurveillanceCameraComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.Active)
        {
            args.Affected = false; // We handle our own effect
            args.Disabled = true;
            SetActive(ent, false);
        }

        UpdateVisuals(ent, ent.Comp);
    }

    private void OnEmpDisabledRemoved(Entity<SurveillanceCameraComponent> ent, ref EmpDisabledRemovedEvent args)
    {
        SetActive(ent, true);
        UpdateVisuals(ent, ent.Comp);
    }

    // TODO: predict the rest of the server side system
    public virtual void SetActive(EntityUid camera, bool setting, SurveillanceCameraComponent? component = null) { }

    protected virtual void OpenSetupInterface(EntityUid uid, EntityUid player, SurveillanceCameraComponent? camera = null) { }

    protected virtual void UpdateVisuals(EntityUid uid, SurveillanceCameraComponent? component = null, AppearanceComponent? appearance = null) { }
}

[Serializable, NetSerializable]
public enum SurveillanceCameraVisualsKey : byte
{
    Key,
    Layer,
}

[Serializable, NetSerializable]
public enum SurveillanceCameraVisuals : byte
{
    Active,
    InUse,
    Disabled,
    Emp,
    // Reserved for future use
    Xray,
}

/// <summary>
/// Raised on a camera entity to find whether it is externally viewed by some entity.
/// This does not use the actual viewers or monitors camera has and is simply used to see whether the camera is "technically"
/// being looked through by somebody, such as the Station AI.
/// </summary>
[ByRefEvent]
public record struct SurveillanceCameraGetIsViewedExternallyEvent(bool Viewed = false);
