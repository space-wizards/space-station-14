using Content.Shared.Maths;

namespace Content.Shared.Temperature
{
    public static class TemperatureHelpers
    {
        public static float CelsiusToKelvin(float celsius)
        {
            return celsius + PhysicalConstants.ZERO_CELCIUS;
        }

        public static float CelsiusToFahrenheit(float celsius)
        {
            return celsius * 9 / 5 + 32;
        }

        public static float KelvinToCelsius(float kelvin)
        {
            return kelvin - PhysicalConstants.ZERO_CELCIUS;
        }

        public static float KelvinToFahrenheit(float kelvin)
        {
            var celsius = KelvinToCelsius(kelvin);
            return CelsiusToFahrenheit(celsius);
        }

        public static float FahrenheitToCelsius(float fahrenheit)
        {
            return (fahrenheit - 32) * 5 / 9;
        }

        public static float FahrenheitToKelvin(float fahrenheit)
        {
            var celsius = FahrenheitToCelsius(fahrenheit);
            return CelsiusToKelvin(celsius);
        }
    }
}
