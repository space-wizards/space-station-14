using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

public abstract partial class BaseStylesheet
{
    public StyleRule[] GetSheetletRules<TSheetTy>(Type sheetletTy, StylesheetManager man)
    {
        Sheetlet<TSheetTy>? sheetlet = null;
        try
        {
            if (sheetletTy.ContainsGenericParameters)
            {
                if (SandboxHelper.CreateInstance(sheetletTy.MakeGenericType(typeof(TSheetTy))) is Sheetlet<TSheetTy>
                    sheetlet1)
                    sheetlet = sheetlet1;
            }
            else if (SandboxHelper.CreateInstance(sheetletTy) is Sheetlet<TSheetTy> sheetlet2)
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
            return sheetlet.GetRules((TSheetTy)(object)this, _config);
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
