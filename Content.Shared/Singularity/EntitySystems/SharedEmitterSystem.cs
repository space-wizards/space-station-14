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

    [SubscribeLocalEvent]
    private void OnToggleActive(Entity<EmitterComponent> ent, ref EmitterToggleActiveMessage message)
    {
        if (TryComp(ent, out LockComponent? lockComp) && lockComp.Locked)
        {
            Popup.PopupEntity(Loc.GetString("comp-emitter-access-locked",
                ("target", ent.Owner)), ent, message.Actor);
            return;
        }

        ToggleActive(ent, message.Actor);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<EmitterComponent> ent, ref ExaminedEvent args)
    {
        if (!FireMode.TryGetFireMode((ent, null), out var fireMode))
            return;

        var proto = ProtoMan.Index<EntityPrototype>(fireMode.Prototype);
        args.PushMarkup(Loc.GetString("emitter-component-current-type", ("type", proto.Name)));
    }

    protected virtual void ToggleActive(Entity<EmitterComponent> ent, EntityUid user)
    {

    }
}

[Serializable, NetSerializable]
public enum EmitterModesUiKey
{
    Key
}

