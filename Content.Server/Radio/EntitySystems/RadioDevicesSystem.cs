using Content.Server.Chat.V2;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.Components;
using Content.Server.Speech.Components;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioDevicesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private void SetMicrophoneEnabled(EntityUid uid, EntityUid? user, bool enabled, bool quiet = false, RadioMicrophoneComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.PowerRequired && !this.IsPowered(uid, EntityManager))
            return;

        component.Enabled = enabled;

        if (!quiet && user != null)
        {
            var state = Loc.GetString(component.Enabled ? "handheld-radio-component-on-state" : "handheld-radio-component-off-state");
            var message = Loc.GetString("handheld-radio-component-on-use", ("radioState", state));
            _popup.PopupEntity(message, user.Value, user.Value);
        }

        _appearance.SetData(uid, RadioDeviceVisuals.Broadcasting, component.Enabled);

        if (component.Enabled)
            EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(uid);
    }

    private void SetSpeakerEnabled(EntityUid uid, EntityUid user, bool enabled, bool quiet = false, RadioSpeakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Enabled = enabled;

        if (!quiet)
        {
            var state = Loc.GetString(component.Enabled ? "handheld-radio-component-on-state" : "handheld-radio-component-off-state");
            var message = Loc.GetString("handheld-radio-component-on-use", ("radioState", state));
            _popup.PopupEntity(message, user, user);
        }

        _appearance.SetData(uid, RadioDeviceVisuals.Speaker, component.Enabled);
    }
}
