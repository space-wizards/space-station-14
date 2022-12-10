using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.NPC;

public abstract class SharedNPCSteeringSystem : EntitySystem
{
    public const byte InterestDirections = 12;

    /// <summary>
    /// How many radians between each interest direction.
    /// </summary>
    public const double InterestRadians = MathF.Tau / InterestDirections;

    /// <summary>
    /// How many degrees between each interest direction.
    /// </summary>
    public const float InterestDegrees = 360f / InterestDirections;
}

[Serializable, NetSerializable]
public readonly record struct NPCSteeringContext()
{
    // Can also not use danger map though its whole purpose is to have a permanent subtraction value from the weights.

    [ViewVariables]
    public readonly float[] Interest = new float[SharedNPCSteeringSystem.InterestDirections];

    [ViewVariables]
    public readonly float[] Danger = new float[SharedNPCSteeringSystem.InterestDirections];

    public void Clear()
    {
        for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
        {
            Interest[i] = 0f;
            Danger[i] = 0f;
        }
    }

    public void Seek(Vector2 direction, float weight)
    {
        if (direction == Vector2.Zero || weight == 0f)
            return;

        var directionNorm = direction.Normalized;

        for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
        {
            var angle = i * SharedNPCSteeringSystem.InterestRadians;
            var dot = Vector2.Dot(new Angle(angle).ToVec(), directionNorm);
            Interest[i] = Mathdot * weight;
        }
    }

    public void Avoid(Vector2 direction, float weight)
    {
        if (direction == Vector2.Zero || weight == 0f)
            return;

        var directionRadians = (float) direction.ToAngle();

        for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
        {
            var angle = i * SharedNPCSteeringSystem.InterestRadians;
            var dot = MathF.Cos(directionRadians - angle);
            dot = (dot + 1f) * 0.5f;
            Danger[i] += dot * weight;
        }
    }

    public Vector2 GetDesiredNormal()
    {
        var idx = GetMax();

        if (idx == -1)
            return Vector2.Zero;

        var normal = GetNormal(idx);
        return new Vector2(MathF.Cos(normal), MathF.Sin(normal));
    }

    private float GetNormal(int index)
    {
        return SharedNPCSteeringSystem.InterestRadians * index;
    }

    // Alternatively you can average all inputs instead.

    public int GetMax()
    {
        var idx = -1;
        var maxValue = 0f;

        for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
        {
            var value = Interest[i] - Danger[i];

            if (value < maxValue)
                continue;

            maxValue = value;
            idx = i;
        }

        return idx;
    }
}
