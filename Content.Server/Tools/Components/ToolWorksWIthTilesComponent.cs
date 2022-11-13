using System.Threading;
using Content.Shared.Tools;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tools.Components;

[RegisterComponent]
public sealed class ToolWorksWithTilesComponent : Component
{
    [ViewVariables]
    [DataField("requiresUnobstructed")]
    public bool RequiresUnobstructed = false;

    [ViewVariables]
    [DataField("delay")]
    public float Delay = 0.25f;

    [ViewVariables]
    [DataField("adminLog")]
    public bool AdminLog = false;

    /// <summary>
    /// Used for do_afters.
    /// </summary>
    public CancellationTokenSource? CancelTokenSource = null;
}
