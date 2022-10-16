using Content.Shared.Singularity.EntitySystems;

namespace Content.Shared.Singularity.Components;

[RegisterComponent]
public abstract class SharedEventHorizonComponent : Component
{
    /// <summary>
    ///     The radius of the event horizon within which it will destroy all entities and tiles.
    ///     If < 0.0 this behavior will not be active.
    /// </summary>
    [Access(friends:typeof(SharedEventHorizonSystem))]
    public float _radius;

    /// <summary>
    ///     Whether the event horizon can consume/destroy the devices built to contain it.
    /// </summary>
    [Access(friends:typeof(SharedEventHorizonSystem))]
    public bool _canBreachContainment = false;

    /// <summary>
    ///     The public getter/setter for _radius.
    ///     Delegates setting to SharedEventHorizonSystem.
    /// </summary>
    [DataField("radius")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Radius
    {
        get => _radius;
        set { IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedEventHorizonSystem>().SetRadius(Owner, value, eventHorizon: this); }
    }

    /// <summary>
    ///     The public getter/setter for _radius.
    ///     Delegates setting to SharedEventHorizonSystem.
    /// </summary>
    [DataField("canBreachContainment")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanBreachContainment
    {
        get => _canBreachContainment;
        set { IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedEventHorizonSystem>().SetCanBreachContainment(Owner, value, eventHorizon: this); }
    }

    /// <summary>
    ///     The ID of the fixture used to detect if the event horizon has collided with any physics objects.
    ///     Can be set to null, in which case no such fixture is used.
    /// </summary>
    [DataField("horizonFixtureId")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? HorizonFixtureId = SharedEventHorizonSystem.DefaultHorizonFixtureId;
}
