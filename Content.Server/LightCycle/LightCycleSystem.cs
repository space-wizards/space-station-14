using Robust.Shared.Map.Components;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;
using Content.Server.GameTicking;


namespace Content.Server.LightCycle
{
    public sealed partial class DayCycleSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configuration = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        private double _deltaTime;
        private Dictionary<int, Color>? _mapColor;

        public override void Initialize()
        {
            base.Initialize();

            _deltaTime = 0;
            _mapColor = new Dictionary<int, Color>();
        }
        public override void Update(float frameTime)
        {

            // Prevents the light from being updated every tick, with an adjustable frequency.

            if (_deltaTime / frameTime >= _configuration.GetCVar(CCVars.CycleUpdateFrequency))
            {

                // Iterates over the entities with the DayCycle component, in case of multiple maps with it added.

                foreach (var comp in EntityQuery<LightCycleComponent>())
                {

                    // Checks whether the map has the MapLight component, which is essential to its operation, and, if so, it is provided.

                    if (EntityManager.TryGetComponent<MapLightComponent>(comp.Owner, out var mapLight))
                    {
                        if (comp.IsEnabled)
                        {

                            // The original color must be added to a dictionary, as a fixed reference, as the color SHOULD NOT change itself during the process.

                            if (!_mapColor!.TryGetValue(mapLight.Owner.Id, out var value))
                            {
                                _mapColor.Add(mapLight.Owner.Id, mapLight.AmbientLightColor);
                            }
                            else
                            {

                                // Performs color and luminosity calculations and sets the MapLight new color.

                                comp.CurrentTime = _gameTicker.RoundDuration().TotalSeconds + comp.InitialTime;
                                var lightLevel = CalculateLightLevel(comp);
                                var color = GetCycleColor(comp, value);
                                var red = (int) Math.Min(255, color.RByte * lightLevel);
                                var green = (int) Math.Min(255, color.GByte * lightLevel);
                                var blue = (int) Math.Min(255, color.BByte * lightLevel);
                                mapLight.AmbientLightColor = System.Drawing.Color.FromArgb(red, green, blue);
                                Dirty(mapLight.Owner, mapLight);
                            }
                        }
                    }
                }
                _deltaTime = 0;
            }
            _deltaTime += frameTime;
        }

        // Decomposes the color into its components and multiplies each one by the individual color level as function of time, returning a new color.
        public static Color GetCycleColor(LightCycleComponent comp, Color color)
        {
            if (comp.IsEnabled && comp.IsColorShiftEnabled)
            {
                var colorLevel = CalculateColorLevel(comp);
                var red = Math.Min(255, color.RByte * colorLevel[0]);
                var green = Math.Min(255, color.GByte * colorLevel[1]);
                var blue = Math.Min(255, color.BByte * colorLevel[2]);
                return System.Drawing.Color.FromArgb((int) red, (int) green, (int) blue);
            }
            else
                return color;

        }

        // Calculates light intensity as a function of time.

        public static double CalculateLightLevel(LightCycleComponent comp)
        {
            var wave_lenght = Math.Max(1, comp.CycleDuration);
            var crest = Math.Max(0, comp.MaxLightLevel);
            var shift = Math.Max(0, comp.MinLightLevel);
            return Math.Min(comp.ClipLight, CalculateCurve(comp.CurrentTime, wave_lenght, crest, shift, 6));
        }

        /// <summary>
        /// Returns a double vector with color levels, where 0 = Red, 1 = Green, 2 = Blue.
        /// It is important to note that each color must have a different exponent, to modify how early or late one color should stand out in relation to another.
        /// This "simulates" what the atmosphere does and is what generates the effect of dawn and dusk.
        /// The blue component must be a cosine function with half period, so that its minimum is at dawn and dusk, generating the "warm" color corresponding to these periods.
        /// As you can see in the values, the maximums of the function serve more to define the curve behavior,
        /// they must be "clipped" so as not to distort the original color of the lighting. In practice, the maximum values, in fact, are the clip thresholds.
        /// </summary>

        public static double[] CalculateColorLevel(LightCycleComponent comp)
        {
            var wave_lenght = Math.Max(1, comp.CycleDuration);
            var color_level = new double[3];
            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        color_level[i] = Math.Min(comp.ClipRed, CalculateCurve(comp.CurrentTime, wave_lenght,
                        Math.Max(0, comp.MaxRedLevel), Math.Max(0, comp.MinRedLevel), 4));
                        break;
                    case 1:
                        color_level[i] = Math.Min(comp.ClipGreen, CalculateCurve(comp.CurrentTime, wave_lenght,
                        Math.Max(0, comp.MaxGreenLevel), Math.Max(0, comp.MinGreenLevel), 10));
                        break;
                    case 2:
                        color_level[i] = Math.Min(comp.ClipBlue, CalculateCurve(comp.CurrentTime, wave_lenght / 2,
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
