using Content.Shared.Administration.Managers.Bwoink;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers.Bwoink;

public sealed partial class ServerBwoinkManager : SharedBwoinkManager
{
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeMessages();
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }

    /// <summary>
    /// Validates that the protoId given is actually a real prototype. Needed since clients can just send whatever as the ID.
    /// </summary>
    private bool IsPrototypeReal(ProtoId<BwoinkChannelPrototype> channel)
    {
        // If this fails, Resolve will log an error.
        return PrototypeManager.Resolve<BwoinkChannelPrototype>(channel, out _);
    }
}
