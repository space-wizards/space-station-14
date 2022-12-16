using Content.Server.Power.Components;
using Content.Shared.Tag;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Mining;

[RegisterComponent]
public class ElectromagnetComponent : Component
{
    [DataField("range")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range = 2f;

    [DataField("force")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Force = 300f;

    [DataField("minDist")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MinDist = 0.5f;
}

public class ElectromagnetSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        foreach (var comp in EntityManager.EntityQuery<ElectromagnetComponent>())
        {
            if (!(TryComp<ApcPowerReceiverComponent>(comp.Owner, out var apc) && apc.Powered))
            {
                // Not powered, don't do anything
                continue;
            }

            var xformQuery = GetEntityQuery<TransformComponent>();
            var physQuery = GetEntityQuery<PhysicsComponent>();
            if (!xformQuery.TryGetComponent(comp.Owner, out var myXform))
            {
                // We need our own transform in order to move things
                continue;
            }

            foreach (var uid in _lookup.GetEntitiesInRange(comp.Owner, comp.Range))
            {
                if (_tagSystem.HasTag(uid, "Metal"))
                {
                    if (!xformQuery.TryGetComponent(uid, out var xform))
                        continue;

                    if (!physQuery.TryGetComponent(uid, out var phys))
                        continue;

                    var dx = myXform.WorldPosition - xform.WorldPosition;
                    var dir = dx.Normalized;
                    var dist = dx.Length;
                    if (dist > comp.MinDist)
                    {
                        var force = dir * (comp.Force / MathF.Pow(dist, 2)); // falls off inverse squared wrt distance
                        _physics.ApplyForce(uid, force);
                    }
                    else
                    {
                        _physics.SetLinearVelocity(uid, Vector2.Zero);
                    }
                }
            }
        }
    }
}
