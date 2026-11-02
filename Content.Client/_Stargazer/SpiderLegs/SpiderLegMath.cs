// Content.Client/_Stargazer/SpiderLegs/SpiderLegMath.cs
using System.Numerics;

namespace Content.Client._Stargazer.SpiderLegs;

/// <summary>
/// Строит кривую ноги между корнем и стопой. Кривая всегда имеет заданную длину:
/// чем ближе стопа к корню, тем сильнее выгибается колено. Затем кривая режется
/// на N сегментов одинаковой длины.
/// </summary>
public static class SpiderLegMath
{
    private const int Samples = 24;   // точность аппроксимации длины дуги
    private const int Iterations = 12; // итерации бисекции для подбора высоты колена

    // Скретч-буферы. Обновление идёт в одном потоке, поэтому static безопасен.
    private static readonly Vector2[] Pts = new Vector2[Samples + 1];
    private static readonly float[] Cum = new float[Samples + 1];

    public static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        var u = 1f - t;
        return u * u * u * p0
               + 3f * u * u * t * p1
               + 3f * u * t * t * p2
               + t * t * t * p3;
    }

    private static float ArcLength(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var len = 0f;
        var prev = p0;
        for (var i = 1; i <= Samples; i++)
        {
            var p = Bezier(p0, p1, p2, p3, i / (float) Samples);
            len += Vector2.Distance(prev, p);
            prev = p;
        }

        return len;
    }

    /// <summary>
    /// Заполняет joints[0..segments] точками вдоль ноги (массив может быть длиннее).
    /// </summary>
    /// <param name="length">Желаемая длина дуги (длина ноги плюс подъём во время шага).</param>
    /// <param name="side">Сторона выгиба колена, 1 или -1.</param>
    /// <param name="offsetA">Насколько первая контрольная точка сдвигается вбок (доля от высоты колена).
    /// 0 — нога выходит из корпуса прямо, без изгиба у основания.</param>
    /// <param name="offsetB">То же для второй контрольной точки.</param>
    public static void Solve(
        Vector2 root,
        Vector2 foot,
        float length,
        int side,
        float controlA,
        float controlB,
        float offsetA,
        float offsetB,
        int segments,
        Vector2[] joints)
    {
        var d = foot - root;
        var dist = d.Length();
        var dir = dist > 1e-5f ? d / dist : new Vector2(1, 0);

        // Перпендикуляр к хорде: в эту сторону выгибается колено.
        var normal = new Vector2(-dir.Y, dir.X) * (side >= 0 ? 1f : -1f);

        var a = Vector2.Lerp(root, foot, controlA);
        var b = Vector2.Lerp(root, foot, controlB);

        // Подбираем высоту h так, чтобы длина дуги совпала с length.
        // Длина дуги монотонно растёт с h, поэтому бисекция сходится всегда.
        // Если нога растянута (dist >= length), остаётся прямая линия (h = 0).
        var h = 0f;
        if (length > dist)
        {
            var lo = 0f;
            var hi = length;
            for (var i = 0; i < Iterations; i++)
            {
                var mid = (lo + hi) * 0.5f;
                var len = ArcLength(root, a + normal * (mid * offsetA), b + normal * (mid * offsetB), foot);
                if (len < length)
                    lo = mid;
                else
                    hi = mid;
            }

            h = (lo + hi) * 0.5f;
        }

        var c1 = a + normal * (h * offsetA);
        var c2 = b + normal * (h * offsetB);

        // Таблица "параметр t → накопленная длина".
        Pts[0] = root;
        Cum[0] = 0f;
        for (var i = 1; i <= Samples; i++)
        {
            Pts[i] = Bezier(root, c1, c2, foot, i / (float) Samples);
            Cum[i] = Cum[i - 1] + Vector2.Distance(Pts[i - 1], Pts[i]);
        }

        // Пересэмплирование с равным шагом по длине дуги.
        // Без этого сегменты у колена растягивались бы, а у концов сжимались.
        var total = Cum[Samples];
        segments = Math.Clamp(segments, 1, joints.Length - 1);
        var k = 0;
        for (var i = 0; i <= segments; i++)
        {
            var target = total * i / segments;
            while (k < Samples - 1 && Cum[k + 1] < target)
                k++;

            var span = Cum[k + 1] - Cum[k];
            var t = span > 1e-6f ? (target - Cum[k]) / span : 0f;
            joints[i] = Vector2.Lerp(Pts[k], Pts[k + 1], Math.Clamp(t, 0f, 1f));
        }
    }
}
