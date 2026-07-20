using System.Linq;
using System.Numerics;
using Content.Shared.EntityShapes.Shapes;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityShapes;

public sealed partial class EntityShapeSystem
{
    /// <summary>
    /// Gets positions of an <see cref="EntityShape"/>.
    /// </summary>
    /// <param name="shape">The shape to calculate.</param>
    /// <param name="center">Amount to offset the final result.</param>
    /// <param name="rand">The randomizer to use. <br/> Defaults to the instance <see cref="IoCManager"/> provides.</param>
    public IEnumerable<Vector2> GetShape(
        EntityShape? shape,
        Vector2? center = null,
        IRobustRandom? rand = null)
    {
        if (shape == null)
            return Enumerable.Empty<Vector2>();

        rand ??= _random;
        center ??= Vector2.Zero;
        return shape.Accept(
            GetEntityShapeVisitor.Instance,
            new GetEntityShapeVisitor.Args(ProtoMan, rand, center)
        );
    }
}

sealed file class GetEntityShapeVisitor :
    IEntityShapeVisitor<GetEntityShapeVisitor.Args, IEnumerable<Vector2>>
{
    private GetEntityShapeVisitor() { }
    public static readonly GetEntityShapeVisitor Instance = new();

    public record struct Args(
        IPrototypeManager ProtoMan,
        IRobustRandom Rand,
        Vector2? Center = null,
        int? Size = null,
        float? StepSize = null
    );

    private static void ApplyOverrides(EntityShape shape, Args args)
    {
        // We take values by these priorities:
        // 1. YAML DataFields
        // 2. Arguments passed from the parent
        // 3. Default value.
        shape.Offset = shape.DefaultOffset ?? args.Center ?? shape.Offset;
        shape.Size = shape.DefaultSize ?? args.Size ?? shape.Size;
        shape.StepSize = shape.DefaultStepSize  ?? args.StepSize ?? shape.StepSize;
    }

    public IEnumerable<Vector2> VisitAllShape(AllEntityShape shape, Args args)
    {
        ApplyOverrides(shape, args);

        var result = new List<Vector2>();
        foreach (var child in shape.Children)
        {
            Vector2? offset = shape.Offset;
            int? size = shape.Size;
            float? stepSize = shape.StepSize;

            if (shape.Options != null
                && child.OverrideGroup != null
                && shape.Options.TryGetValue(child.OverrideGroup, out var options))
            {
                offset = options.Offset;
                size = options.GroupSize;
                stepSize = options.GroupStepSize;
            }

            var newArgs = args with { Center = offset, Size = size, StepSize = stepSize };
            result.AddRange(child.Accept(this, newArgs));
        }

        return result.Distinct().ToList();
    }

    public IEnumerable<Vector2> VisitGroupShape(GroupEntityShape shape, Args args)
    {
        ApplyOverrides(shape, args);

        var validWeightedChildren = shape.Children
            .Where(child => child.Weight >= float.Epsilon)
            .ToDictionary(child => child, child => child.Weight);

        if (validWeightedChildren.Count == 0)
            return Enumerable.Empty<Vector2>();

        var child = SharedRandomExtensions.Pick(validWeightedChildren, args.Rand);
        var newArgs = args with { Center = shape.Offset, Size = shape.Size, StepSize = shape.StepSize };
        return child.Accept(this, newArgs);
    }

    public IEnumerable<Vector2> VisitNestedShape(NestedEntityShape shape, Args args)
    {
        ApplyOverrides(shape, args);
        var newArgs = args with { Center = shape.Offset, Size = shape.Size, StepSize = shape.StepSize };
        return args.ProtoMan.Index(shape.Id).Shape.Accept(this, newArgs);
    }

    public IEnumerable<Vector2> VisitNoneShape(NoneEntityShape shape, Args args)
    {
        return Enumerable.Empty<Vector2>();
    }

    public IEnumerable<Vector2> VisitSingleShape(SingleEntityShape shape, Args args)
    {
        ApplyOverrides(shape, args);
        return new List<Vector2> { shape.Offset };
    }

    public IEnumerable<Vector2> VisitRandomShape(RandomEntityShape shape, Args args)
    {
        ApplyOverrides(shape, args);

        var newArgs = args with { Center = shape.Offset, Size = shape.Size, StepSize = shape.StepSize };
        var shapeRefs = shape.Accept(this, newArgs).ToList();

        if (shape.FilledChance != null)
        {
            var temp = new List<Vector2>(shapeRefs);
            foreach (var pos in temp)
            {
                if (!args.Rand.Prob(shape.FilledChance.Value))
                    shapeRefs.Remove(pos);
            }
        }

        if (shape.Amount != null)
        {
            var temp = new List<Vector2>(shape.Amount.Value);
            for (int i = 0; i < shape.Amount.Value; i++)
            {
                temp.Add(args.Rand.Pick(temp));
            }
            shapeRefs = temp;
        }

        return shapeRefs;
    }

    public IEnumerable<Vector2> VisitBoxShape(BoxEntityShape shape, Args args)
    {
        ApplyOverrides(shape, args);

        return shape.Hollow ? MakeBoxHollow(shape.Offset, shape.Size, shape.StepSize) : MakeBoxFilled(shape.Offset, shape.Size, shape.StepSize);
    }

    private static IEnumerable<Vector2> MakeBoxFilled(Vector2 center, int range, float stepSize = 1)
    {
        if (range <= 0)
            yield break;

        if (stepSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(stepSize), "stepSize must be greater than zero.");

        if (range == 1)
        {
            yield return center;
            yield break;
        }

        var half = (range - 1) / 2f;
        var startPoint = center - new Vector2(half, half);

        for (var y = 0f; y < range; y += stepSize)
        {
            for (var x = 0f; x < range; x += stepSize)
            {
                yield return startPoint + new Vector2(x, y);
            }
        }
    }

    private static IEnumerable<Vector2> MakeBoxHollow(Vector2 center, int range, float stepSize = 1)
    {
        if (range <= 0)
            yield break;

        if (stepSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(stepSize), "stepSize must be greater than zero.");

        if (range == 1)
        {
            yield return center;
            yield break;
        }

        var bottomLeft = center - new Vector2(range, range);
        var topLeft = center - new Vector2(range, -range);
        var topRight = center - new Vector2(-range, -range);
        var bottomRight = center - new Vector2(-range, range);

        // for example 0.999 should be considered as 1 so the loop works correctly
        var side = (int) MathF.Round(range, 2);

        // Left side
        for (var i = 0f; i < side; i += stepSize)
        {
            yield return bottomLeft + Vector2.UnitY * i;
        }
        // Top side
        for (var i = 0f; i < side; i += stepSize)
        {
            yield return topLeft + Vector2.UnitX * i;
        }
        // Right side
        for (var i = 0f; i < side; i += stepSize)
        {
            yield return topRight + -Vector2.UnitY * i;
        }
        // Bottom side
        for (var i = 0f; i < side; i += stepSize)
        {
            yield return bottomRight + -Vector2.UnitX * i;
        }
    }

    public IEnumerable<Vector2> VisitLineShape(LineEntityShape shape, Args args)
    {
        ApplyOverrides(shape, args);

        yield return shape.Offset;

        if (shape.Direction == Vector2.Zero)
            yield break;

        var curStep = shape.Offset;
        for (int i = 0; i < shape.Size; i++)
        {
            curStep += shape.Direction;
            yield return curStep;
        }
    }

    public IEnumerable<Vector2> VisitCrossShape(CrossEntityShape shape, Args args)
    {
        ApplyOverrides(shape, args);

        yield return shape.Offset;

        if (shape.Size <= 0)
            yield break;

        for (var i = 1f; i < shape.Size; i += shape.StepSize)
        {
            yield return shape.Offset with { X = shape.Offset.X + i };
            yield return shape.Offset with { Y = shape.Offset.Y + i };
            yield return shape.Offset with { X = shape.Offset.X - i };
            yield return shape.Offset with { Y = shape.Offset.Y - i };
        }
    }

    public IEnumerable<Vector2> VisitDiagonalCrossShape(DiagonalCrossEntityShape shape, Args args)
    {
        ApplyOverrides(shape, args);

        yield return shape.Offset;

        if (shape.Size <= 0)
            yield break;

        for (var i = 1f; i < shape.Size; i += shape.StepSize)
        {
            yield return new Vector2(shape.Offset.X + i, shape.Offset.Y + i);
            yield return new Vector2(shape.Offset.X + i, shape.Offset.Y - i);
            yield return new Vector2(shape.Offset.X - i, shape.Offset.Y + i);
            yield return new Vector2(shape.Offset.X - i, shape.Offset.Y - i);
        }
    }
}
