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
            comp.CurrentColor = comp.DayColor;
            _entityManager.TryGetComponent<MapLightComponent>(uid, out var lightColor);
            if (lightColor != null)
                comp.MapLightComponent = lightColor;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var (mapLight, dayTime) in EntityManager.EntityQuery<MapLightComponent, DayTimeComponent>())
            {
                dayTime.Timer += frameTime;
                if (dayTime.Timer >= dayTime.Time)
                {
                    dayTime.Timer = 0f;
                    dayTime.CurrentColor = ColorMoveTowards(dayTime.CurrentColor, dayTime.TargetColor, 0.01f);
                    //dayTime.CurrentColor = Color.InterpolateBetween(dayTime.CurrentColor, dayTime.TargetColor,0.1f);
                    mapLight.AmbientLightColor = dayTime.CurrentColor;
                    dayTime.color1 = ColorToVector4(dayTime.CurrentColor);
                    _entityManager.Dirty(mapLight);
                }
            }
        }

        private Color ColorMoveTowards(Color color1, Color color2, float delta)
        {
            var colRes = new Color
            (
                ColorMakeThings(color1.R, color2.R, delta),
                ColorMakeThings(color1.G, color2.G, delta),
                ColorMakeThings(color1.B, color2.B, delta),
                ColorMakeThings(color1.A, color2.A, delta)
            );

            // var col1 = ColorToVector4(color1);
            // var col2 = ColorToVector4(color2);
            // var colRes = new Vector4
            // {
            //     X = ColorMakeThings(col1.X, col2.X, delta),
            //     Y = ColorMakeThings(col1.Y, col2.Y, delta),
            //     Z = ColorMakeThings(col1.Z, col2.Z, delta),
            //     W = ColorMakeThings(col1.W, col2.W, delta)
            // };

            return colRes;
        }

        private float ColorMakeThings(float col1, float col2, float delta)
        {
            var colRes = col1;
            if(col1 < col2 + 0.0001f && col1 > col2 - 0.0001f)
                return col2;
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

        private Vector4 ColorToVector4 (Color col)
        {
            return new Vector4
            {
                X = col.R,
                Y = col.G,
                Z = col.B,
                W = col.A
            };
        }

    }
}
