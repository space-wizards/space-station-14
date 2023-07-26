using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.Blob;

[RegisterComponent, NetworkedComponent]
public sealed class BlobObserverComponent : Component
{
    public EntityUid? Core = default!;
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

