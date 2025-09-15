// Adapted from https://bottosson.github.io/posts/gamutclipping/
// Licensed under MIT, license text:
// Copyright (c) 2021 Björn Ottosson

// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Numerics;

namespace Content.Shared.Humanoid;

public static partial class ColorHelper
{
    // Finds the maximum saturation possible for a given hue that fits in sRGB
    // Saturation here is defined as S = C/L
    // a and b must be normalized so a^2 + b^2 == 1
    private static float ComputeMaxSaturation(float a, float b)
    {
        // Max saturation will be when one of r, g or b goes below zero.

        // Select different coefficients depending on which component goes below zero first
        float k0, k1, k2, k3, k4, wl, wm, ws;

        if (-1.88170328f * a - 0.80936493f * b > 1)
        {
            // Red component
            k0 = +1.19086277f; k1 = +1.76576728f; k2 = +0.59662641f; k3 = +0.75515197f; k4 = +0.56771245f;
            wl = +4.0767416621f; wm = -3.3077115913f; ws = +0.2309699292f;
        }
        else if (1.81444104f * a - 1.19445276f * b > 1)
        {
            // Green component
            k0 = +0.73956515f; k1 = -0.45954404f; k2 = +0.08285427f; k3 = +0.12541070f; k4 = +0.14503204f;
            wl = -1.2684380046f; wm = +2.6097574011f; ws = -0.3413193965f;
        }
        else
        {
            // Blue component
            k0 = +1.35733652f; k1 = -0.00915799f; k2 = -1.15130210f; k3 = -0.50559606f; k4 = +0.00692167f;
            wl = -0.0041960863f; wm = -0.7034186147f; ws = +1.7076147010f;
        }

        // Approximate max saturation using a polynomial:
        float S = k0 + k1 * a + k2 * b + k3 * a * a + k4 * a * b;

        // Do one step Halley's method to get closer
        // this gives an error less than 10e6, except for some blue hues where the dS/dh is close to infinite
        // this should be sufficient for most applications, otherwise do two/three steps

        float k_l = +0.3963377774f * a + 0.2158037573f * b;
        float k_m = -0.1055613458f * a - 0.0638541728f * b;
        float k_s = -0.0894841775f * a - 1.2914855480f * b;

        {
            float l_ = 1f + S * k_l;
            float m_ = 1f + S * k_m;
            float s_ = 1f + S * k_s;

            float l = l_ * l_ * l_;
            float m = m_ * m_ * m_;
            float s = s_ * s_ * s_;

            float l_dS = 3f * k_l * l_ * l_;
            float m_dS = 3f * k_m * m_ * m_;
            float s_dS = 3f * k_s * s_ * s_;

            float l_dS2 = 6f * k_l * k_l * l_;
            float m_dS2 = 6f * k_m * k_m * m_;
            float s_dS2 = 6f * k_s * k_s * s_;

            float f = wl * l + wm * m + ws * s;
            float f1 = wl * l_dS + wm * m_dS + ws * s_dS;
            float f2 = wl * l_dS2 + wm * m_dS2 + ws * s_dS2;

            S = S - f * f1 / (f1 * f1 - 0.5f * f * f2);
        }

        return S;
    }

    private static Vector2 FindCusp(float a, float b)
    {
        // First, find the maximum saturation (saturation S = C/L)
        float S_cusp = ComputeMaxSaturation(a, b);

        // Convert to linear sRGB to find the first point where at least one of r,g or b >= 1:
        Vector3 rgb_at_max = OklabToLinearSrgb(new Vector3(1, S_cusp * a, S_cusp * b));
        float L_cusp = (float)Math.Cbrt(1f / Math.Max(Math.Max(rgb_at_max.X, rgb_at_max.Y), rgb_at_max.Z));
        float C_cusp = L_cusp * S_cusp;

        return new Vector2(L_cusp, C_cusp);
    }

    // Finds intersection of the line defined by
    // L = L0 * (1 - t) + t * L1;
    // C = t * C1;
    // a and b must be normalized so a^2 + b^2 == 1
    private static float FindGamutIntersection(float a, float b, float L1, float C1, float L0)
    {
        // Find the cusp of the gamut triangle
        Vector2 cusp = FindCusp(a, b);

        // Find the intersection for upper and lower half seprately
        float t;
        if (((L1 - L0) * cusp.Y - (cusp.X - L0) * C1) <= 0f)
        {
            // Lower half

            t = cusp.Y * L0 / (C1 * cusp.X + cusp.Y * (L0 - L1));
        }
        else
        {
            // Upper half

            // First intersect with triangle
            t = cusp.Y * (L0 - 1f) / (C1 * (cusp.X - 1f) + cusp.Y * (L0 - L1));

            // Then one step Halley's method
            {
                float dL = L1 - L0;
                float dC = C1;

                float k_l = +0.3963377774f * a + 0.2158037573f * b;
                float k_m = -0.1055613458f * a - 0.0638541728f * b;
                float k_s = -0.0894841775f * a - 1.2914855480f * b;

                float l_dt = dL + dC * k_l;
                float m_dt = dL + dC * k_m;
                float s_dt = dL + dC * k_s;


                // If higher accuracy is required, 2 or 3 iterations of the following block can be used:
                {
                    float L = L0 * (1f - t) + t * L1;
                    float C = t * C1;

                    float l_ = L + C * k_l;
                    float m_ = L + C * k_m;
                    float s_ = L + C * k_s;

                    float l = l_ * l_ * l_;
                    float m = m_ * m_ * m_;
                    float s = s_ * s_ * s_;

                    float ldt = 3 * l_dt * l_ * l_;
                    float mdt = 3 * m_dt * m_ * m_;
                    float sdt = 3 * s_dt * s_ * s_;

                    float ldt2 = 6 * l_dt * l_dt * l_;
                    float mdt2 = 6 * m_dt * m_dt * m_;
                    float sdt2 = 6 * s_dt * s_dt * s_;

                    float r = 4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s - 1;
                    float r1 = 4.0767416621f * ldt - 3.3077115913f * mdt + 0.2309699292f * sdt;
                    float r2 = 4.0767416621f * ldt2 - 3.3077115913f * mdt2 + 0.2309699292f * sdt2;

                    float u_r = r1 / (r1 * r1 - 0.5f * r * r2);
                    float t_r = -r * u_r;

                    float g = -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s - 1;
                    float g1 = -1.2684380046f * ldt + 2.6097574011f * mdt - 0.3413193965f * sdt;
                    float g2 = -1.2684380046f * ldt2 + 2.6097574011f * mdt2 - 0.3413193965f * sdt2;

                    float u_g = g1 / (g1 * g1 - 0.5f * g * g2);
                    float t_g = -g * u_g;

                    float b0 = -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s - 1;
                    float b1 = -0.0041960863f * ldt - 0.7034186147f * mdt + 1.7076147010f * sdt;
                    float b2 = -0.0041960863f * ldt2 - 0.7034186147f * mdt2 + 1.7076147010f * sdt2;

                    float u_b = b1 / (b1 * b1 - 0.5f * b0 * b2);
                    float t_b = -b0 * u_b;

                    t_r = u_r >= 0f ? t_r : float.MaxValue;
                    t_g = u_g >= 0f ? t_g : float.MaxValue;
                    t_b = u_b >= 0f ? t_b : float.MaxValue;

                    t += Math.Min(t_r, Math.Min(t_g, t_b));
                }
            }
        }

        return t;
    }

    public static Vector3 GamutClipPreserveChroma(Vector3 rgb)
    {
        if (rgb.X < 1 && rgb.Y < 1 && rgb.Z < 1 && rgb.X > 0 && rgb.Y > 0 && rgb.Z > 0)
            return rgb;

        Vector3 lab = LinearSrgbToOklab(rgb);

        float L = lab.X;
        float eps = 0.00001f;
        float C = Math.Max(eps, (float)Math.Sqrt(lab.Y * lab.Y + lab.Z * lab.Z));
        float a_ = lab.Y / C;
        float b_ = lab.Z / C;

        float L0 = Math.Clamp(L, 0f, 1f);

        float t = FindGamutIntersection(a_, b_, L, C, L0);
        float L_clipped = L0 * (1 - t) + t * L;
        float C_clipped = t * C;

        return OklabToLinearSrgb(new Vector3(L_clipped, C_clipped * a_, C_clipped * b_));
    }
}
