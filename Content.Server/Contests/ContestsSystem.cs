using Robust.Shared.Physics.Components;

namespace Content.Server.Contests
{
    /// <summary>
    /// Standardized contests.
    /// A contest is figuring out, based on data in components on two entities,
    /// which one has an advantage in a situation. The advantage is expressed by a multiplier.
    /// 1 = No advantage to either party.
    /// &gt;1 = Advantage to roller
    /// &lt;1 = Advantage to target
    /// Roller should be the entity with an advantage from being bigger/healthier/more skilled, etc.
    /// </summary>
    ///
    public sealed class ContestsSystem : EntitySystem
    {
        /// <summary>
        /// Returns the roller's mass divided by the target's.
        /// </summary>
        public float MassContest(EntityUid roller, EntityUid target, PhysicsComponent? rollerPhysics = null, PhysicsComponent? targetPhysics = null)
        {
            if (!Resolve(roller, ref rollerPhysics, false) || !Resolve(target, ref targetPhysics, false))
                return 1f;

            if (targetPhysics.FixturesMass == 0)
                return 1f;

            return rollerPhysics.FixturesMass / targetPhysics.FixturesMass;
        }

        /// <summary>
        /// This softens out the huge advantages that damage contests would lead to otherwise.
        /// Once you are crit or near crit, we just let the massive advantages roll with what could be a 20x.
        /// </summary>
        public float DamageThresholdConverter(float score)
        {
            return score switch
            {
                // TODO: Should just be a curve
                <= 0 => 1f,
                <= 0.25f => 0.9f,
                <= 0.5f => 0.75f,
                <= 0.75f => 0.6f,
                <= 0.95f => 0.45f,
                _ => 0.05f
            };
        }
    }
}
