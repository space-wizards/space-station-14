using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

public abstract partial class StylesheetFactory
{
    public StyleRule[] GetSheetletRules<TSheetTy>(Type sheetletTy, StylesheetManager man)
    {
        ISheetlet<TSheetTy>? sheetlet = null;
        try
        {
            if (sheetletTy.ContainsGenericParameters)
            {
                if (SandboxHelper.CreateInstance(sheetletTy.MakeGenericType(typeof(TSheetTy))) is ISheetlet<TSheetTy>
                    sheetlet1)
                    sheetlet = sheetlet1;
            }
            else if (SandboxHelper.CreateInstance(sheetletTy) is ISheetlet<TSheetTy> sheetlet2)
            {
                sheetlet = sheetlet2;
            }
        }
        // thrown when `sheetletTy.MakeGenericType` is given a type that does not satisfy the type constraints of
        // `sheetletTy`
        catch (ArgumentException) { }

        if (sheetlet is not null)
        {
            man.UnusedSheetlets.Remove(sheetletTy);
            return sheetlet.GetRules(TODO, (TSheetTy)(object)this);
        }
        else
            return [];
    }

    public StyleRule[] GetAllSheetletRules<TSheetTy, TAttrib>(StylesheetManager man)
        where TAttrib : Attribute
    {
        var tys = ReflectionManager.FindTypesWithAttribute<TAttrib>();
        var rules = new List<StyleRule>();

        foreach (var ty in tys)
        {
            rules.AddRange(GetSheetletRules<TSheetTy>(ty, man));
        }

        return rules.ToArray();
    }
}
