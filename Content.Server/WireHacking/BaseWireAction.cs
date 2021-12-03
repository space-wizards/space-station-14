using Content.Server.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Wires;

[DataDefinition]
public abstract class BaseWireAction : IWireAction
{
    public IEntityManager EntityManager = default!;
    public DoAfterSystem DoAfterSystem = default!;
    public WiresSystem WiresSystem = default!;

    public BaseWireAction()
    {
        // IoCManager.InjectDependencies(this);


    }

    public abstract object Identifier { get; }

    // ugly, but IoC doesn't work during deserialization
    public virtual void Initialize(EntityUid uid, Wire wire)
    {
        EntityManager = IoCManager.Resolve<IEntityManager>();

        WiresSystem = EntitySystem.Get<WiresSystem>();
        DoAfterSystem = EntitySystem.Get<DoAfterSystem>();
    }

    public abstract bool Cut(EntityUid used, EntityUid user, Wire wire);
    public abstract bool Mend(EntityUid used, EntityUid user, Wire wire);
    public abstract bool Pulse(EntityUid used, EntityUid user, Wire wire);
}
