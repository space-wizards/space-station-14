using Content.Shared.Stunnable;

namespace Content.Server.Stunnable
{
    public sealed class StunSystem : SharedStunSystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<KnockedDownComponent>();

            while (query.MoveNext(out var uid, out var knockedDown))
            {
                if (!knockedDown.AutoStand || knockedDown.DoAfter.HasValue || knockedDown.NextUpdate > GameTiming.CurTime)
                    continue;

                TryStanding(uid, out knockedDown.DoAfter);
            }
        }
    }
}
