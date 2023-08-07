using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Blob;

[RegisterComponent, NetworkedComponent]
public sealed class BlobObserverComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsProcessingMoveEvent;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Core = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool CanMove = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public BlobChemType SelectedChemId = BlobChemType.ReactiveSpines;
}

[Serializable, NetSerializable]
public sealed class BlobChemSwapComponentState : ComponentState
{
    public BlobChemType SelectedChem;
}

[Serializable, NetSerializable]
public sealed class BlobChemSwapBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly Dictionary<BlobChemType, Color> ChemList;
    public readonly BlobChemType SelectedChem;

    public BlobChemSwapBoundUserInterfaceState(Dictionary<BlobChemType, Color> chemList, BlobChemType selectedId)
    {
        ChemList = chemList;
        SelectedChem = selectedId;
    }
}

[Serializable, NetSerializable]
public sealed class BlobChemSwapPrototypeSelectedMessage : BoundUserInterfaceMessage
{
    public readonly BlobChemType SelectedId;

    public BlobChemSwapPrototypeSelectedMessage(BlobChemType selectedId)
    {
        SelectedId = selectedId;
    }
}

[Serializable, NetSerializable]
public enum BlobChemSwapUiKey : byte
{
    Key
}


public sealed class BlobCreateFactoryActionEvent : WorldTargetActionEvent
{

}

public sealed class BlobCreateResourceActionEvent : WorldTargetActionEvent
{

}

public sealed class BlobCreateNodeActionEvent : WorldTargetActionEvent
{

}

public sealed class BlobCreateBlobbernautActionEvent : WorldTargetActionEvent
{

}

public sealed class BlobSplitCoreActionEvent : WorldTargetActionEvent
{

}

public sealed class BlobSwapCoreActionEvent : WorldTargetActionEvent
{

}

public sealed class BlobToCoreActionEvent : InstantActionEvent
{

}

public sealed class BlobToNodeActionEvent : InstantActionEvent
{

}

public sealed class BlobHelpActionEvent : InstantActionEvent
{

}

public sealed class BlobSwapChemActionEvent : InstantActionEvent
{

}

