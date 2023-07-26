using Content.Shared.FootPrints;
using Robust.Shared.Timing;
using Content.Server.Decals;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Contest.Server.FootPrints
{
    public sealed class FootPrintsSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly DecalSystem _decals = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FootPrintsComponent, ComponentStartup>(OnStartupComponent);
        }

        public void OnStartupComponent(EntityUid uid, FootPrintsComponent comp, ComponentStartup args)
        {
            comp.Timer = _timing.CurTime;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var comp in EntityManager.EntityQuery<FootPrintsComponent>())
            {
                if (!EntityManager.TryGetComponent<TransformComponent>(comp.Owner, out var transform))
                    continue;
                //comp.Timer += TimeSpan.FromSeconds(frameTime);
                if (_timing.CurTime >= comp.Timer)
                {
                    comp.Timer = _timing.CurTime + comp.Time;
                    if(comp.PrintsColor.A > 0f)
                    {
                        var coords = new EntityCoordinates(comp.Owner, transform.LocalPosition + comp.OffsetCenter);
                        if(comp.RightStep)
                        {
                            coords = new EntityCoordinates(comp.Owner, transform.LocalPosition + comp.OffsetCenter + new Angle(Angle.FromDegrees(180f)+transform.LocalRotation).RotateVec(comp.OffsetPrint));
                        }
                        else
                        {
                            coords = new EntityCoordinates(comp.Owner, transform.LocalPosition + comp.OffsetCenter + new Angle(transform.LocalRotation).RotateVec(comp.OffsetPrint));
                        }
                        comp.RightStep = !comp.RightStep;
                        _decals.TryAddDecal("footprint", coords, out var dID, comp.PrintsColor, Math.Round(transform.LocalRotation, 1)+Angle.FromDegrees(180f), 0, true);
                        comp.PrintsColor = blendColor(comp.PrintsColor);
                        //comp.PrintsColor = Color.InterpolateBetween(comp.PrintsColor, Color.FromHex("#00000000"),0.2f);

                    }
                }
            }
        }

        private Color blendColor(Color col)
        {
            if (col.A - 0.1f > 0f)
            {
                Color res = new Color(col.R, col.G, col.B, col.A - 0.1f);
                return res;
            }
            else
            {
                return new Color(col.R, col.G, col.B, 0f);
            }


        }
    }
}

