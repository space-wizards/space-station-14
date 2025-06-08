using Content.Shared.Audio;
using Content.Shared.Doors.Components;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    public void SetAlarmTripped(Entity<DoorAlarmComponent> ent, bool value, EntityUid? user = null, bool predicted = false)
    {
        TrySetAlarmTripped(ent, value, user, predicted);
    }

    private bool TrySetAlarmTripped(
        Entity<DoorAlarmComponent> ent,
        bool value,
        EntityUid? user = null,
        bool predicted = false
    )
    {
        if (!_powerReceiver.IsPowered(ent.Owner))
            return false;
        if (ent.Comp.AlarmTripped == value)
            return false;

        ent.Comp.AlarmTripped = value;
        Dirty(ent, ent.Comp);
        return true;
    }

    public bool IsAlarmTripped(EntityUid uid, DoorAlarmComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return false;
        }

        return component.AlarmTripped;
    }
    public void EnableAlarmSound(Entity<DoorAlarmComponent> ent, EntityUid? uid = null, bool predicted = false)
    {
        if (TryComp<AmbientSoundComponent>(ent.Owner,out var curSound))
        {
            _ambient.SetAmbience(ent.Owner, true, curSound);
            _ambient.SetVolume(ent.Owner, ent.Comp.volume,curSound);
            _ambient.SetRange(ent.Owner, ent.Comp.range,curSound);
            return;
        }

        var sound = new AmbientSoundComponent();
        AddComp(ent.Owner, sound);
        _ambient.SetAmbience(ent.Owner, true, sound);
        _ambient.SetSound(ent,ent.Comp.AlarmSound,sound);
        _ambient.SetVolume(ent.Owner, ent.Comp.volume,sound);
        _ambient.SetRange(ent.Owner, ent.Comp.range,sound);

    }
    public void DisableAlarmSound(Entity<DoorAlarmComponent> ent, EntityUid? user = null, bool predicted = false)
    {
        if(!TryComp<AmbientSoundComponent>(ent.Owner,out var sound)) return;
        _ambient.SetAmbience(ent.Owner, false, sound);
        _ambient.SetVolume(ent,0,sound);
        _ambient.SetRange(ent,0,sound);

    }
}
