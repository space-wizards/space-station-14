using Content.Shared.Examine;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Singularity.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.EntitySystems;

public abstract partial class SharedEmitterSystem : EntitySystem
{
    [Dependency] protected BatteryWeaponFireModesSystem FireMode = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitterComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EmitterComponent, EmitterToggleActiveMessage>(OnToggleActive);
    }

    private void OnToggleActive(EntityUid uid, EmitterComponent component, EmitterToggleActiveMessage args)
    {
        if (TryComp(uid, out LockComponent? lockComp) && lockComp.Locked)
        {
            Popup.PopupEntity(Loc.GetString("comp-emitter-access-locked",
                ("target", uid)), uid, args.Actor);
            return;
        }

        ToggleActive(uid, component, args.Actor);
    }

    private void OnExamined(EntityUid uid, EmitterComponent component, ExaminedEvent args)
    {
        if (!FireMode.TryGetFireMode((uid, null), out var fireMode))
            return;

        var proto = ProtoMan.Index<EntityPrototype>(fireMode.Prototype);
        args.PushMarkup(Loc.GetString("emitter-component-current-type", ("type", proto.Name)));
    }

    protected virtual void ToggleActive(EntityUid uid, EmitterComponent component, EntityUid user)
    {

    }
}

[Serializable, NetSerializable]
public enum EmitterModesUiKey
{
    Key
}

