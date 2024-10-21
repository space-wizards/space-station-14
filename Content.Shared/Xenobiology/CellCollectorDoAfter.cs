using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[Serializable, NetSerializable]
public sealed partial class CellCollectorDoAfter : SimpleDoAfterEvent
{
    public readonly CellCollectorDirection Direction;

    public CellCollectorDoAfter(CellCollectorDirection direction)
    {
        Direction = direction;
    }

    public CellCollectorDoAfter(CellCollectorDoAfter doAfter)
    {
        Direction = doAfter.Direction;
    }

    public override DoAfterEvent Clone()
    {
        return new CellCollectorDoAfter(this);
    }
}

[Serializable, NetSerializable]
public enum CellCollectorDirection
{
    Collection,
    Transfer,
}
