namespace Content.Shared.Body;

public sealed partial class BodySystem
{
    [Obsolete("Use an event-relay based approach instead")]
    public bool TryGetOrgansWithComponent<TComp>(Entity<BodyComponent?> ent, out List<Entity<TComp>> organs) where TComp : Component
    {
        organs = new();
        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return false;

        foreach (var organ in ent.Comp.Organs?.ContainedEntities ?? [])
        {
            if (TryComp<TComp>(organ, out var comp))
                organs.Add((organ, comp));
        }

        return organs.Count != 0;
    }
}
