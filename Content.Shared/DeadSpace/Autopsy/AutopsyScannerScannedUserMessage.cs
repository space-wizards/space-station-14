// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Autopsy;

/// <summary>
///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class AutopsyScannerScannedUserMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;
    public TimeSpan? TimeOfDeath;

    public AutopsyScannerScannedUserMessage(NetEntity? targetEntity, TimeSpan? timeOfDeath)
    {
        TargetEntity = targetEntity;
        TimeOfDeath = timeOfDeath;
    }
}

