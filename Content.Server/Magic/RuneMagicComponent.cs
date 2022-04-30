using System.Threading;
using Newtonsoft.Json;

namespace Content.Server.Magic;

[RegisterComponent]
public sealed class RuneMagicComponent : Component
{

    //todo:
    //add a bool check to see if it's a collide type
    //add more datafields to specify specific parameters

    /// <summary>
    /// Cancel token for runes that have an active time.
    /// </summary>
    [ViewVariables]
    public CancellationTokenSource? CancelToken;
}
