using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Content.Shared.Atmos;

/// <summary>
/// Helper class for solving the ideal gas law.
/// </summary>
/// <remarks>I got tired of looking this shit up.
/// Aerospace engineering student btw.</remarks>
public static class IdealGasHelper
{
    private const float R = Atmospherics.R;

    [PublicAPI]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SolvePressure(float moles, float vol, float temp = Atmospherics.T20C)
    {
        return moles * R * temp / vol;
    }

    [PublicAPI]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SolveVolume(float moles, float pressure, float temp = Atmospherics.T20C)
    {
        return moles * R * temp / pressure;
    }

    [PublicAPI]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SolveMoles(float pressure, float vol, float temp = Atmospherics.T20C)
    {
        return pressure * vol / (R * temp);
    }

    [PublicAPI]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SolveTemp(float pressure, float vol, float moles)
    {
        return pressure * vol / (moles * R);
    }
}
