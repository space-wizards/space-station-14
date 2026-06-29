using Content.Shared.EntityTable.EntitySelectors;
using JetBrains.Annotations;

namespace Content.Shared.EntityTable;

/// <summary>
/// <a href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor</a> for <see cref="EntityTableSelector"/>s.
/// </summary>
/// <typeparam name="TArgs">The type of arguments passed to visitation</typeparam>
/// <typeparam name="TResult">The type of the visitation result</typeparam>
[PublicAPI]
public interface IEntityTableVisitor<in TArgs, out TResult>
{
    /// <summary>
    /// Alias of <see cref="EntityTableSelector.Accept{TContext, TResult}(IEntityTableVisitor{TContext, TResult}, TContext)"/>.
    /// </summary>
    [PublicAPI]
    TResult Visit(EntityTableSelector selector, TArgs args) => selector.Accept(this, args);

    /// <summary>
    /// Visit an <see cref="AllSelector"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitAllSelector(AllSelector selector, TArgs args);

    /// <summary>
    /// Visit an <see cref="EntSelector"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitEntSelector(EntSelector selector, TArgs args);

    /// <summary>
    /// Visit a <see cref="GroupSelector"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitGroupSelector(GroupSelector selector, TArgs args);

    /// <summary>
    /// Visit a <see cref="NestedSelector"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitNestedSelector(NestedSelector selector, TArgs args);

    /// <summary>
    /// Visit a <see cref="NoneSelector"/>.
    /// </summary>
    [PublicAPI]
    TResult VisitNoneSelector(NoneSelector selector, TArgs args);
}
