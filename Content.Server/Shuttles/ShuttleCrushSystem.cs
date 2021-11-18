using System;
using System.Collections.Generic;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;

namespace Content.Server.Shuttles
{
    /// <summary>
    /// Indicates this entity can be crushed between shuttles.
    /// </summary>
    [RegisterComponent]
    public sealed class ShuttleCrushableComponent : Component
    {
        public override string Name => "ShuttleCrushable";
    }

    public sealed class ShuttleCrushSystem : EntitySystem
    {
        /*
         * Crush a mob under these scenarios:
         * 1. it is parented to the map
         * 2. It is between static bodies on at least 2 different grids (including invalid)
         * 3. the contact normal is sufficient
         */

        [Dependency] private readonly IPhysicsManager _physManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShuttleCrushableComponent, StartCollideEvent>(OnCollide);
        }

        private void OnCollide(EntityUid uid, ShuttleCrushableComponent component, StartCollideEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent) ||
                physicsComponent.ContactCount < 2) return;

            var ourGrid = EntityManager.GetComponent<TransformComponent>(uid).GridID;

            if (ourGrid != GridId.Invalid ||
                !EntityManager.TryGetComponent(uid, out DamageableComponent? damageableComponent)) return;

            var grids = new HashSet<GridId>();
            var squishBodies = new HashSet<PhysicsComponent>();

            var validContacts = new List<Contact>();

            // Get all contacts for us with static bodies to determine if we need crushing
            foreach (var contact in physicsComponent.Contacts)
            {
                if (!contact.Enabled || !contact.IsTouching) continue;

                var bodyA = contact.FixtureA?.Body;
                var bodyB = contact.FixtureB?.Body;

                // This shouldn't happen but paul will shank me if I don't handle this.
                if (bodyA == null || bodyB == null) continue;

                if (bodyB == physicsComponent)
                {
                    var body = bodyA;
                    bodyA = bodyB;
                    bodyB = body;
                }
                else if (bodyA != physicsComponent)
                {
                    continue;
                }

                // To perform a crush then there needs to be at least 2 grids involved, normally 3
                // (2 if we're on invalidgrid and there's another static body on invalidgrid we're getting crushed into)
                if (bodyB.BodyType != BodyType.Static)
                {
                    continue;
                }

                // To ensure we don't cook the contact normal we'll only pull 1 body from each grid for determining squish.
                var bGrid = EntityManager.GetComponent<TransformComponent>(bodyB.OwnerUid).GridID;

                if (!grids.Add(bGrid)) continue;

                squishBodies.Add(bodyB);
                validContacts.Add(contact);
            }

            if (validContacts.Count < 2) return;

            if (grids.Count < 3 && grids.Contains(GridId.Invalid))
            {
                var valid = false;

                // Check the 2 grid condition from above
                // we're on invalidgrid and we're getting crushed into another static body on invalidgrid
                foreach (var grid in grids)
                {
                    if (grid == GridId.Invalid)
                    {
                        valid = true;
                        break;
                    }
                }

                if (!valid) return;
            }
            else if (grids.Count < 2)
            {
                return;
            }

            // Go through every body with its map velocity and make absolutely sure it's heading towards the player
            // to be gibbed.
            foreach (var body in squishBodies)
            {
                var mapVel = body.MapLinearVelocity;
            }

            if (EntityManager.TryGetComponent(uid, out SharedBodyComponent? bodyComponent))
            {
                bodyComponent.Gib();
            }
            else
            {
                // UHHHHH
                throw new NotImplementedException();
            }
        }
    }
}
