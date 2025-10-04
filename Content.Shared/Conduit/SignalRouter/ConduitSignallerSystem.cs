using Content.Shared.DeviceLinking;

namespace Content.Shared.Conduit.SignalRouter;

/// <summary>
/// This handles the emission of signals when entities in conduits
/// pass through a one with a <see cref="ConduitSignallerComponent"/>.
/// </summary>
public sealed class ConduitSignallerSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceLinkSystem _link = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ConduitSignallerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ConduitSignallerComponent, GetConduitNextDirectionEvent>(OnGetNextDirection, after: new[] { typeof(SharedConduitSystem) });
    }

    private void OnInit(Entity<ConduitSignallerComponent> ent, ref ComponentInit args)
    {
        _link.EnsureSourcePorts(ent, ent.Comp.Port);
    }

    private void OnGetNextDirection(Entity<ConduitSignallerComponent> ent, ref GetConduitNextDirectionEvent args)
    {
        _link.InvokePort(ent, ent.Comp.Port);
    }
}
