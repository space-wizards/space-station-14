using System.Numerics;
using JetBrains.Annotations;

namespace Content.Shared.EntityShapes.Shapes;

/// <summary>
/// Represents a list of points that entities can be then spawned on.
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class EntityShape
{
    /// <summary>
    /// A weight used to pick between shapes.
    /// </summary>
    [DataField]
    public float Weight = 1;

    /// <summary>
    /// If specified, will add this shape into a shapes group,
    /// that can be customized via <see cref="AllEntityShape"/>.
    /// That way you can change size or offset for groups of tiles
    /// instead of individually changing values.
    /// </summary>
    [DataField("group")]
    public string? OverrideGroup;

    // All "DefaultX" are values that are specified in prototypes

    [DataField("offset")]
    public Vector2? DefaultOffset;

    [DataField("size")]
    public int? DefaultSize;

    [DataField("step")]
    public float? DefaultStepSize;

    [ViewVariables]
    public Vector2 Offset = Vector2.Zero;

    [ViewVariables]
    public int Size = 1;

    [ViewVariables]
    public float StepSize = 1;

    /// <summary>
    /// Accepts <paramref name="visitor"/>, passing <paramref name="args"/> to it, and returning the result. Basically
    /// an alias for invoking <c>visitor.Visit(this, args)</c>.
    /// <br/>
    /// </summary>
    /// <seealso cref="IEntityShapeVisitor{TArgs, TResult}"/>
    [Access(Other = AccessPermissions.Execute)]
    public abstract TResult Accept<TArgs, TResult>(IEntityShapeVisitor<TArgs, TResult> visitor, TArgs args);
}
