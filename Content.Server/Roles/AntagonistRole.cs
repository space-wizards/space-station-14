using Content.Shared.Roles;

namespace Content.Server.Roles;

public abstract class AntagonistRole : Role
{
    public AntagPrototype Prototype { get; }

    public override string Name { get; }

    public override bool Antagonist { get; }

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="mind">A mind (player)</param>
    /// <param name="antagPrototype">Antagonist prototype</param>
    protected AntagonistRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind)
    {
        Prototype = antagPrototype;
        Name = Loc.GetString(antagPrototype.Name);
        Antagonist = antagPrototype.Antagonist;
    }
}
