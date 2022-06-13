namespace Content.Server.Worldgen;

//TODO: This should use a dynamic tree internally for complex constraints.

/// <summary>
/// A constraint composed of boxes allowing for a system to check whether or not some arbitrary region contains a point.
/// Often used for constraining things to a region, it's namesake.
/// </summary>
public sealed class Constraint
{
    private readonly List<Box2Rotated> _allowBlocks = new();
    private readonly List<Box2Rotated> _denyBlocks = new();
    private readonly List<Circle> _allowCircles = new();
    private readonly List<Circle> _denyCircles = new();

    public bool ContainsPoint(Vector2 point)
    {
        var contains = false;
        foreach (var box in _allowBlocks)
        {
            contains |= box.Contains(point);
        }

        foreach (var circle in _allowCircles)
        {
            contains |= circle.Contains(point);
        }

        foreach (var box in _denyBlocks)
        {
            contains &= !box.Contains(point);
        }

        foreach (var circle in _denyCircles)
        {
            contains &= !circle.Contains(point);
        }

        return contains;
    }

    public Constraint(Box2 allowed)
    {
        _allowBlocks.Add(new Box2Rotated(allowed));
    }

    public Constraint(Box2Rotated allowed)
    {
        _allowBlocks.Add(allowed);
    }

    public void AddAllowedBox(Box2 allowed)
    {
        _allowBlocks.Add(new Box2Rotated(allowed));
    }

    public void AddAllowedBox(Box2Rotated allowed)
    {
        _allowBlocks.Add(allowed);
    }

    public void AddDeniedBox(Box2 allowed)
    {
        _denyBlocks.Add(new Box2Rotated(allowed));
    }

    public void AddDeniedBox(Box2Rotated allowed)
    {
        _denyBlocks.Add(allowed);
    }

    public void AddAllowedCircle(Circle allowed)
    {
        _allowCircles.Add(allowed);
    }

    public void AddDeniedCircle(Circle allowed)
    {
        _denyCircles.Add(allowed);
    }

}
