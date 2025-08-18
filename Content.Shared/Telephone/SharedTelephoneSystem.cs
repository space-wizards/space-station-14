using System.Linq;

namespace Content.Shared.Telephone;

public abstract class SharedTelephoneSystem : EntitySystem
{
    public bool IsTelephoneEngaged(Entity<TelephoneComponent> entity)
    {
        return entity.Comp.LinkedTelephones.Any();
    }

    public bool IsSourceInRangeOfReceiver(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver)
    {
        // Check if the source and receiver have compatible transmision / reception bandwidths
        if (!source.Comp.CompatibleRanges.Contains(receiver.Comp.TransmissionRange))
            return false;

        var sourceXform = Transform(source);
        var receiverXform = Transform(receiver);

        // Check if we should ignore a device thats on the same grid
        if (source.Comp.IgnoreTelephonesOnSameGrid &&
            source.Comp.TransmissionRange != TelephoneRange.Grid &&
            receiverXform.GridUid == sourceXform.GridUid)
            return false;

        switch (source.Comp.TransmissionRange)
        {
            case TelephoneRange.Grid:
                return sourceXform.GridUid == receiverXform.GridUid;

            case TelephoneRange.Map:
                return sourceXform.MapID == receiverXform.MapID;

            case TelephoneRange.Unlimited:
                return true;
        }

        return false;
    }

    public string GetFormattedCallerIdForEntity(string? presumedName, string? presumedJob, Color fontColor, string fontType = "Default", int fontSize = 12)
    {
        var callerId = Loc.GetString("chat-telephone-unknown-caller",
            ("color", fontColor),
            ("fontType", fontType),
            ("fontSize", fontSize));

        if (presumedName == null)
            return callerId;

        if (presumedJob != null)
            callerId = Loc.GetString("chat-telephone-caller-id-with-job",
                ("callerName", presumedName),
                ("callerJob", presumedJob),
                ("color", fontColor),
                ("fontType", fontType),
                ("fontSize", fontSize));

        else
            callerId = Loc.GetString("chat-telephone-caller-id-without-job",
                ("callerName", presumedName),
                ("color", fontColor),
                ("fontType", fontType),
                ("fontSize", fontSize));

        return callerId;
    }

    public string GetFormattedDeviceIdForEntity(string? deviceName, Color fontColor, string fontType = "Default", int fontSize = 12)
    {
        if (deviceName == null)
        {
            return Loc.GetString("chat-telephone-unknown-device",
                ("color", fontColor),
                ("fontType", fontType),
                ("fontSize", fontSize));
        }

        return Loc.GetString("chat-telephone-device-id",
            ("deviceName", deviceName),
            ("color", fontColor),
            ("fontType", fontType),
            ("fontSize", fontSize));
    }
}
