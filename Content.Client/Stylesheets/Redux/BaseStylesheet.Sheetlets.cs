using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets.Redux;

public abstract partial class BaseStylesheet
{
    public StyleRule[] GetSheetletRules<TSheetTy>(Type sheetletTy)
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

        return sheetlet is not null ? sheetlet.GetRules((TSheetTy)(object)this, _config) : [];
    }

    public StyleRule[] GetAllSheetletRules<TSheetTy, TAttrib>()
        where TAttrib : Attribute
    {
        var tys = ReflectionManager.FindTypesWithAttribute<TAttrib>();
        var rules = new List<StyleRule>();

        foreach (var ty in tys)
        {
            rules.AddRange(GetSheetletRules<TSheetTy>(ty));
        }

        return rules.ToArray();
    }
}
