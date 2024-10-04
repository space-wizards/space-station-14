using Content.Client.GameTicking.Managers;
using Content.Shared.Light.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client.LightCycle
{
    public sealed partial class LightCycleSystem : EntitySystem
    {
        [Dependency] private readonly ClientGameTicker _gameTicker = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LightCycleComponent, ComponentShutdown>(OnComponentShutdown);
        }

        private void OnComponentShutdown(EntityUid uid, LightCycleComponent cycle, ComponentShutdown args)
        {
            if (LifeStage(uid) >= EntityLifeStage.Terminating)
                return;
            if (_entityManager.TryGetComponent<MapLightComponent>(uid, out var map))
            {
                map.AmbientLightColor = Color.FromHex(cycle.OriginalColor);
            }
        }

        public override void FrameUpdate(float frameTime)
        {
            var mapQuery = EntityQueryEnumerator<MapLightComponent, LightCycleComponent>();
            while (mapQuery.MoveNext(out var uid, out var map, out var cycle))
            {
                if (cycle.OriginalColor != null && !cycle.OriginalColor.ToUpper().Equals("#0000FF"))
                {
                    var time = _gameTiming.CurTime.Subtract(cycle.Offset).Subtract(_gameTicker.RoundStartTimeSpan).TotalSeconds + cycle.InitialTime;
                    var color = GetColor((uid, cycle), Color.FromHex(cycle.OriginalColor), time);
                    if (!color.Equals(map.AmbientLightColor.ToHex()))
                    {
                        map.AmbientLightColor = color;
                    }
                }
                else
                {
                    cycle.OriginalColor = map.AmbientLightColor.ToHex();
                }
            }
        }

        // Decomposes the color into its components and multiplies each one by the individual color level as function of time, returning a new color.
        public static Color GetColor(Entity<LightCycleComponent> cycle, Color color, double time)
        {
            if (cycle.Comp.IsEnabled)
            {
                var lightLevel = CalculateLightLevel(cycle.Comp, time);
                var colorLevel = CalculateColorLevel(cycle.Comp, time);
                var rgb = new int[] { (int) Math.Min(255, color.RByte * colorLevel[0] * lightLevel),
                                      (int) Math.Min(255, color.GByte * colorLevel[1] * lightLevel),
                                      (int) Math.Min(255, color.BByte * colorLevel[2] * lightLevel) };
                var hex = "#";
                for (int i = 0, j = 0; i < 6; i++)
                {
                    if (i % 2 == 0)
                    {
                        hex += (rgb[j] / 16).ToString("X");
                    }
                    else
                    {
                        hex += (rgb[j] % 16).ToString("X");
                        j++;
                    }
                }
                return Color.FromHex(hex);
            }
            else
                return color;

        }

        // Calculates light intensity as a function of time.

        public static double CalculateLightLevel(LightCycleComponent comp, double time)
        {
            var wave_lenght = Math.Max(1, comp.CycleDuration);
            var crest = Math.Max(0, comp.MaxLightLevel);
            var shift = Math.Max(0, comp.MinLightLevel);
            return Math.Min(comp.ClipLight, CalculateCurve(time, wave_lenght, crest, shift, 6));
        }

        /// <summary>
        /// Returns a double vector with color levels, where 0 = Red, 1 = Green, 2 = Blue.
        /// It is important to note that each color must have a different exponent, to modify how early or late one color should stand out in relation to another.
        /// This "simulates" what the atmosphere does and is what generates the effect of dawn and dusk.
        /// The blue component must be a cosine function with half period, so that its minimum is at dawn and dusk, generating the "warm" color corresponding to these periods.
        /// As you can see in the values, the maximums of the function serve more to define the curve behavior,
        /// they must be "clipped" so as not to distort the original color of the lighting. In practice, the maximum values, in fact, are the clip thresholds.
        /// </summary>

        public static double[] CalculateColorLevel(LightCycleComponent comp, double time)
        {
            var wave_lenght = Math.Max(1, comp.CycleDuration);
            var color_level = new double[3];
            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        color_level[i] = Math.Min(comp.ClipRed, CalculateCurve(time, wave_lenght,
                        Math.Max(0, comp.MaxRedLevel), Math.Max(0, comp.MinRedLevel), 4));
                        break;
                    case 1:
                        color_level[i] = Math.Min(comp.ClipGreen, CalculateCurve(time, wave_lenght,
                        Math.Max(0, comp.MaxGreenLevel), Math.Max(0, comp.MinGreenLevel), 10));
                        break;
                    case 2:
                        color_level[i] = Math.Min(comp.ClipBlue, CalculateCurve(time, wave_lenght / 2,
                        Math.Max(0, comp.MaxBlueLevel), Math.Max(0, comp.MinBlueLevel), 2, wave_lenght / 4));
                        break;
                }
            }
            return color_level;
        }

        /// <summary>
        /// Generates a sinusoidal curve as a function of x (time). The other parameters serve to adjust the behavior of the curve.
        /// </summary>
        /// <param name="x"> It corresponds to the independent variable of the function, which in the context of this algorithm is the current time. </param>
        /// <param name="wave_lenght"> It's the wavelength of the function, it can be said to be the total duration of the light cycle. </param>
        /// <param name="crest"> It's the maximum point of the function, where it will have its greatest value. </param>
        /// <param name="shift"> It's the vertical displacement of the function, in practice it corresponds to the minimum value of the function. </param>
        /// <param name="exponent"> It is the exponent of the sine, serves to "flatten" the function close to its minimum points and make it "steeper" close to its maximum. </param>
        /// <param name="phase"> It changes the phase of the wave, like a "horizontal shift". It is important to transform the sinusoidal function into cosine, when necessary. </param>
        /// <returns> The result of the function. </returns>

        public static double CalculateCurve(double x, double wave_lenght, double crest, double shift, double exponent, double phase = 0)
        {
            var sen = Math.Pow(Math.Sin((Math.PI * (phase + x)) / wave_lenght), exponent);
            return (crest - shift) * sen + shift;
        }
    }
}
