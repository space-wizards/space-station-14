using Content.Server.Singularity.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.PushHorn;
using Content.Shared.Singularity.Components;


namespace Content.Server.PushHorn
{
    internal sealed class PushHornSystem : SharedPushHornSystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<PushHornComponent, UseInHandEvent>(UseInHand);
        }

        public override void UseInHand(Entity<PushHornComponent> ent, ref UseInHandEvent args)
        {
            if (TryComp<GravityWellComponent>(ent, out var gravityWell))
            {
                gravityWell.BaseRadialAcceleration = -40;
                gravityWell.BaseTangentialAcceleration = 0;

                ent.Comp.ToggleTime = 1.4;  //how long it'll take for the gravity well to be turned off
                Update(1);
            }
        }


        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<PushHornComponent>();
            while (query.MoveNext(out var uid, out var pushHorn))
            {
                if (TryComp<SingularityDistortionComponent>(uid, out var singularityDistortion))
                {
                    if (pushHorn.ToggleTime is null)
                    {
                        if (TryComp<GravityWellComponent>(uid, out var gravityWell))
                        {
                            gravityWell.BaseRadialAcceleration = 0;
                            gravityWell.BaseTangentialAcceleration = 0;

                            singularityDistortion.Intensity = 0;
                            Dirty(uid, singularityDistortion);  //needed i thinkie
                        }
                        return;
                    }

                    pushHorn.ToggleTime = pushHorn.ToggleTime - frameTime;

                    singularityDistortion.Intensity = singularityDistortion.Intensity + 4;
                    Dirty(uid, singularityDistortion);

                    if (pushHorn.ToggleTime.Value <= 0.0)
                    {
                        pushHorn.ToggleTime = null;
                    }
                }
            }
        }
    }
}

