namespace Content.Shared.Roles.RoleCodeword;

public abstract class SharedRoleCodewordSystem : EntitySystem
{
    public void SetRoleCodewords(Entity<RoleCodewordComponent> ent, string key, List<string> codewords, Color color)
    {
        var data = new CodewordsData(color, codewords);
        ent.Comp.RoleCodewords[key] = data;
        Dirty(ent);
    }
}
