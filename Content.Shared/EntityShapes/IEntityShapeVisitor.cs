using Content.Shared.EntityShapes.Shapes;
using JetBrains.Annotations;

namespace Content.Shared.EntityShapes;

/// <summary>
/// <a href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor</a> for <see cref="EntityShape"/>s.
/// </summary>
/// <typeparam name="TArgs">The type of arguments passed to visitation.</typeparam>
/// <typeparam name="TResult">The type of the visitation result.</typeparam>
[PublicAPI]
public interface IEntityShapeVisitor<in TArgs, out TResult>
{
    /// <summary>
    /// Alias of <see cref="EntityShape.Accept{TContext, TResult}(IEntityShapeVisitor{TContext, TResult}, TContext)"/>.
    /// </summary>
    [PublicAPI]
    TResult Visit(EntityShape selector, TArgs args) => selector.Accept(this, args);

    /// <summary>
    /// Visit an <see cref="AllEntityShape"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitAllShape(AllEntityShape shape, TArgs args);

    /// <summary>
    /// Visit a <see cref="GroupEntityShape"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitGroupShape(GroupEntityShape shape, TArgs args);

    /// <summary>
    /// Visit a <see cref="NestedEntityShape"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitNestedShape(NestedEntityShape shape, TArgs args);

    /// <summary>
    /// Visit a <see cref="NoneEntityShape"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitNoneShape(NoneEntityShape shape, TArgs args);

    /// <summary>
    /// Visit an <see cref="SingleEntityShape"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitSingleShape(SingleEntityShape shape, TArgs args);

    /// <summary>
    /// Visit an <see cref="RandomEntityShape"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitRandomShape(RandomEntityShape shape, TArgs args);

    /// <summary>
    /// Visit an <see cref="BoxEntityShape"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitBoxShape(BoxEntityShape shape, TArgs args);

    /// <summary>
    /// Visit a <see cref="LineEntityShape"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitLineShape(LineEntityShape shape, TArgs args);

    /// <summary>
    /// Visit a <see cref="CrossEntityShape"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitCrossShape(CrossEntityShape shape, TArgs args);

    /// <summary>
    /// Visit a <see cref="DiagonalCrossEntityShape"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitDiagonalCrossShape(DiagonalCrossEntityShape shape, TArgs args);
}
