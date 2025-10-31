using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Weapons.Melee.EnergySword;

[Serializable, NetSerializable]
public enum EnergySwordColorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class EnergySwordColorMessage : BoundUserInterfaceMessage
{
    public readonly Color ChoosenColor;

    public EnergySwordColorMessage(Color color)
    {
        ChoosenColor = color;
    }
}
