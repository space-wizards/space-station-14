using Robust.Shared.Serialization;

namespace Content.Shared.PDA.Ringer;

[Serializable, NetSerializable]
public sealed class RingerPlayRingtoneMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RingerSetRingtoneMessage : BoundUserInterfaceMessage
{
    public Note[] Ringtone { get; }

    public RingerSetRingtoneMessage(Note[] ringTone)
    {
        Ringtone = ringTone;
    }
}
