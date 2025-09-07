using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Magic;
using Content.Shared.RCD.Systems;
using Content.Shared.RCD;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Antags.TerrorSpider;

#region Components

[RegisterComponent]
public sealed partial class StealthOnWebComponent : Component
{
    [DataField]
    public int Collisions = 0;
}
[RegisterComponent]
public sealed partial class EggHolderComponent : Component
{
    [DataField]
    public int Counter = 0;
}
[RegisterComponent]
public sealed partial class HasEggHolderComponent : Component
{
}
[RegisterComponent]
public sealed partial class TerrorPrincessComponent : Component
{
}

#endregion

#region BUI

[Serializable, NetSerializable]
public enum EggsLayingUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class EggsLayingBuiMsg : BoundUserInterfaceMessage
{
    public EntProtoId Egg { get; set; }
}
[Serializable, NetSerializable]
public sealed class EggsLayingBuiState : BoundUserInterfaceState { }

#endregion

#region Events

public sealed partial class EggInjectionEvent : EntityTargetActionEvent { }

[Serializable, NetSerializable]
public sealed partial class EggInjectionDoAfterEvent : SimpleDoAfterEvent {}

public sealed partial class EggsLayingEvent : InstantActionEvent { }

public sealed partial class EMPScreamEvent : InstantActionEvent
{
    [DataField]
    public float Power = 2.5f;

    [DataField]
    public SoundSpecifier? ScreamSound = new SoundPathSpecifier("/Audio/Effects/changeling_shriek.ogg");
}

#endregion