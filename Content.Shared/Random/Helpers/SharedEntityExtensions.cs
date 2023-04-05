using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Random.Helpers
{
    public static class SharedEntityExtensions
    {
        public static void RandomOffset(this EntityUid entity, float minX, float maxX, float minY, float maxY)
        {
            DebugTools.AssertNotNull(entity);
            DebugTools.Assert(minX <= maxX, $"Minimum X value ({minX}) must be smaller than or equal to the maximum X value ({maxX})");
            DebugTools.Assert(minY <= maxY, $"Minimum Y value ({minY}) must be smaller than or equal to the maximum Y value ({maxY})");

            var random = IoCManager.Resolve<IRobustRandom>();
            var randomX = random.NextFloat() * (maxX - minX) + minX;
            var randomY = random.NextFloat() * (maxY - minY) + minY;
            var offset = new Vector2(randomX, randomY);

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity).LocalPosition += offset;
        }

        public static void RandomOffset(this EntityUid entity, float min, float max)
        {
            DebugTools.AssertNotNull(entity);
            DebugTools.Assert(min <= max, $"Minimum value ({min}) must be smaller than or equal to the maximum value ({max})");

            entity.RandomOffset(min, max, min, max);
        }

        public static void RandomOffset(this EntityUid entity, float value)
        {
            value = Math.Abs(value);
            entity.RandomOffset(-value, value);
        }
    }
}
