using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.EntitySystems;

public abstract class SharedRadioDeviceSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioMicrophoneComponent, ExaminedEvent>(OnExamine);
    }

    #region Toggling
    public void ToggleRadioMicrophone(EntityUid uid, EntityUid user, bool quiet = false, RadioMicrophoneComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetMicrophoneEnabled(uid, user, !component.Enabled, quiet, component);
    }

    public virtual void SetMicrophoneEnabled(EntityUid uid, EntityUid? user, bool enabled, bool quiet = false, RadioMicrophoneComponent? component = null) { }

    public void ToggleRadioSpeaker(EntityUid uid, EntityUid user, bool quiet = false, RadioSpeakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetSpeakerEnabled(uid, user, !component.Enabled, quiet, component);
    }

    public void SetSpeakerEnabled(EntityUid uid, EntityUid? user, bool enabled, bool quiet = false, RadioSpeakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Enabled = enabled;
        Dirty(uid, component);

        if (!quiet && user != null)
        {
            var state = Loc.GetString(component.Enabled ? "handheld-radio-component-on-state" : "handheld-radio-component-off-state");
            var message = Loc.GetString("handheld-radio-component-on-use", ("radioState", state));
            _popup.PopupEntity(message, user.Value, user.Value);
        }

        _appearance.SetData(uid, RadioDeviceVisuals.Speaker, component.Enabled);
        if (component.Enabled)
            EnsureComp<ActiveRadioComponent>(uid).Channels.UnionWith(component.Channels);
        else
            RemCompDeferred<ActiveRadioComponent>(uid);
    }
    #endregion

    private void OnExamine(Entity<RadioMicrophoneComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var proto = _protoMan.Index(ent.Comp.BroadcastChannel);

        using (args.PushGroup(nameof(RadioMicrophoneComponent)))
        {
            args.PushMarkup(Loc.GetString("radio-microphone-component-examine",
                ("color", proto.Color),
                ("channel", proto.LocalizedName),
                ("frequency", proto.Frequency)));
        }
    }
}

