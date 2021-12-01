using Content.Server.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Wires;

public abstract class BaseWireAction : IWireAction
{
    [Dependency] public readonly IEntityManager EntityManager = default!;
    public DoAfterSystem DoAfterSystem = default!;
    public WiresSystem WiresSystem = default!;

    public BaseWireAction()
    {
        IoCManager.InjectDependencies(this);

        WiresSystem = EntitySystem.Get<WiresSystem>();
        DoAfterSystem = EntitySystem.Get<DoAfterSystem>();
    }

    public abstract object Identifier { get; }
    public abstract void Initialize(EntityUid uid, Wire wire);
    public abstract bool Cut(EntityUid used, EntityUid user, Wire wire);
    public abstract bool Mend(EntityUid used, EntityUid user, Wire wire);
    public abstract bool Pulse(EntityUid used, EntityUid user, Wire wire);
}
