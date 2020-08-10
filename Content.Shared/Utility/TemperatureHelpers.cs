using Content.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.Utility
{
    public static class TemperatureHelpers
    {
        public static float CelsiusToKelvin(float celsius)
        {
            return celsius + PhysicalConstants.ZERO_CELCIUS;
        }

        public static float KelvinToCelsius(float kelvin)
        {
            return kelvin - PhysicalConstants.ZERO_CELCIUS;
        }
    }
}
