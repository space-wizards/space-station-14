// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Backmen.Economy.WageConsole;

[Serializable, NetSerializable]
public enum WageUiKey : byte
{
    Key
}

public abstract class SharedWageConsoleSystem : EntitySystem
{

}

[Serializable, NetSerializable]
public sealed class UpdateWageConsoleUi : BoundUserInterfaceState
{
    public List<UpdateWageRow> Records = new();
}

[Serializable, NetSerializable]
public sealed class OpenWageRowMsg : BoundUserInterfaceMessage
{
    public readonly uint? Id;

    public OpenWageRowMsg(uint id)
    {
        Id = id;
    }
}

[Serializable, NetSerializable]
public sealed class SaveEditedWageRowMsg : BoundUserInterfaceMessage
{
    public readonly uint? Id;
    public readonly FixedPoint2 Wage;

    public SaveEditedWageRowMsg(uint id, FixedPoint2 wage)
    {
        Id = id;
        Wage = wage;
    }
}

[Serializable, NetSerializable]
public sealed class BonusWageRowMsg : BoundUserInterfaceMessage
{
    public readonly uint? Id;
    public readonly FixedPoint2 Wage;

    public BonusWageRowMsg(uint id, FixedPoint2 wage)
    {
        Id = id;
        Wage = wage;
    }
}

[Serializable, NetSerializable]
public sealed class OpenBonusWageMsg : BoundUserInterfaceMessage
{
    public readonly uint? Id;

    public OpenBonusWageMsg(uint id)
    {
        Id = id;
    }
}

[Serializable, NetSerializable]
public sealed class OpenBonusWageConsoleUi : BoundUserInterfaceState
{
    public UpdateWageRow? Row { get; set; }
}

[Serializable, NetSerializable]
public sealed class OpenEditWageConsoleUi : BoundUserInterfaceState
{
    public UpdateWageRow? Row { get; set; }
}

[Serializable, NetSerializable]
public sealed class UpdateWageRow
{
    public uint Id { get; set; }

    public NetEntity FromId { get; set; }
    public string FromName { get; set; } = default!;
    public string FromAccount { get; set; } = default!;

    public NetEntity ToId { get; set; }
    public string ToName { get; set; } = default!;
    public string ToAccount { get; set; } = default!;

    public FixedPoint2 Wage { get; set; }
}
