using Content.Server.DoAfter;
using Content.Shared.Wires;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Wires;

public abstract class BaseWireAction : IWireAction
{
    public IEntityManager EntityManager = default!;
    public WiresSystem WiresSystem = default!;

    public BaseWireAction()
    {
        // IoCManager.InjectDependencies(this);


    }

    public abstract object Identifier { get; }

    public abstract object StatusKey { get; }

    // ugly, but IoC doesn't work during deserialization
    public virtual void Initialize(Wire wire)
    {
        EntityManager = IoCManager.Resolve<IEntityManager>();

        WiresSystem = EntitySystem.Get<WiresSystem>();
    }

    public abstract bool Cut(EntityUid user, Wire wire);
    public abstract bool Mend(EntityUid user, Wire wire);
    public abstract bool Pulse(EntityUid user, Wire wire);
    public abstract StatusLightData GetStatusLightData(Wire wire);
}
