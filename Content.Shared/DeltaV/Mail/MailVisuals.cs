using Robust.Shared.Serialization;

namespace Content.Shared.DeltaV.Mail
{
    /// <summary>
    /// Stores the visuals for mail.
    /// </summary>
    [Serializable, NetSerializable]
    public enum MailVisuals : byte
    {
        IsLocked,
        IsTrash,
        IsBroken,
        IsFragile,
        IsPriority,
        IsPriorityInactive,
        JobIcon,
    }
}
