using Robust.Shared.Map.Components;
using Content.Shared.DayTime;

namespace Content.Server.DayTime
{
    public sealed class DayTimeSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DayTimeComponent, ComponentStartup>(OnStartupComponent);
        }

        public void OnStartupComponent(EntityUid uid, DayTimeComponent comp, ComponentStartup args)
        {
            if (comp.TimeStage == null)
                return;
            comp.StageTimer = comp.TimeStage[comp.CurrentStage];
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var (mapLight, dayTime) in EntityManager.EntityQuery<MapLightComponent, DayTimeComponent>())
            {
                if (dayTime.TimeStage == null)
                    continue;
                if (dayTime.ColorStage == null)
                    continue;

                dayTime.ColorTimer += frameTime;
                dayTime.StageTimer += frameTime;

                if (dayTime.StageTimer >= dayTime.TimeStage[dayTime.CurrentStage])
                {
                    dayTime.StageTimer = 0f;
                    dayTime.CurrentStage += 1;
                    if (dayTime.CurrentStage >= dayTime.ColorStage.Length)
                    {
                        dayTime.CurrentStage = 0;
                        dayTime.ColorFrom = dayTime.ColorStage[dayTime.ColorStage.Length - 1];
                        dayTime.ColorTo = dayTime.ColorStage[dayTime.CurrentStage];
                        dayTime.ColorCurrent = dayTime.ColorStage[dayTime.ColorStage.Length - 1];
                    }
                    else
                    {
                        dayTime.ColorFrom = dayTime.ColorStage[dayTime.CurrentStage - 1];
                        dayTime.ColorTo = dayTime.ColorStage[dayTime.CurrentStage];
                        dayTime.ColorCurrent = dayTime.ColorStage[dayTime.CurrentStage - 1];
                    }
                }

                if (dayTime.ColorTimer >= 1f / dayTime.StepsPerSecond)
                {
                    dayTime.ColorTimer = 0f;
                    dayTime.ColorCurrent = ColorMoveTowards
                    (
                        dayTime.ColorFrom,
                        dayTime.ColorTo,
                        dayTime.ColorCurrent,
                        dayTime.StepsPerSecond * dayTime.TimeStage[dayTime.CurrentStage]
                    );
                    mapLight.AmbientLightColor = dayTime.ColorCurrent;
                    _entityManager.Dirty(mapLight);
                }
            }
        }

        private Color ColorMoveTowards(Color colFrom, Color colTo, Color colCurrent, float delta)
        {
            var colRes = new Color
            (
                ColorMakeThings(colCurrent.R, colTo.R, Math.Abs(colFrom.R - colTo.R) / delta),
                ColorMakeThings(colCurrent.G, colTo.G, Math.Abs(colFrom.G - colTo.G) / delta),
                ColorMakeThings(colCurrent.B, colTo.B, Math.Abs(colFrom.B - colTo.B) / delta),
                ColorMakeThings(colCurrent.A, colTo.A, Math.Abs(colFrom.A - colTo.A) / delta)
            );
            return colRes;
        }

        private float ColorMakeThings(float col1, float col2, float delta)
        {
            var colRes = col1;
            if (col1 < col2 + 1f / 256f && col1 > col2 - 1f / 256f)
                return col2;
            if (col1 == col2)
            {
                return col2;
            }
            if (col1 > col2)
            {
                colRes = col1 - delta;
                if (col1 < col2)
                {
                    colRes = col2;
                }
                return colRes;
            }
            if (col1 < col2)
            {
                colRes = col1 + delta;
                if (col1 > col2)
                {
                    colRes = col2;
                }
                return colRes;
            }
            return colRes;
        }
    }
}
