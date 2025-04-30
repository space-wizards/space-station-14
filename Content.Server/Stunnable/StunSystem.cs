using Content.Shared.Stunnable;

namespace Content.Server.Stunnable
{
    public sealed class StunSystem : SharedStunSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<KnockedDownComponent, KnockedDownEvent>(OnSubsequentKnockdown);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<KnockedDownComponent>();

            while (query.MoveNext(out var uid, out var knockedDown))
            {
                if (!knockedDown.AutoStand || knockedDown.DoAfter.HasValue || knockedDown.NextUpdate > _gameTiming.CurTime)
                    continue;

                TryStanding(uid, out knockedDown.DoAfter);
            }
        }

        private void OnSubsequentKnockdown(Entity<KnockedDownComponent> ent, ref KnockedDownEvent args)
        {
            if (!ent.Comp.DoAfter.HasValue)
                return;

            _doAfter.Cancel(ent.Comp.DoAfter.Value);
            ent.Comp.DoAfter = null;
        }
    }
}
