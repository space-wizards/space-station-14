using Robust.Shared.Serialization;

namespace Content.Shared.Info;

/// <summary>
///     A client request for server rules.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestRulesMessage : EntityEventArgs;

/// <summary>
///     A server response with server rules.
/// </summary>
[Serializable, NetSerializable]
public sealed class RulesMessage : EntityEventArgs
{
    public string Guide;

    public RulesMessage(string guide)
    {
        Guide = guide;
    }
}

