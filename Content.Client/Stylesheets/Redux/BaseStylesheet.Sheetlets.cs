using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets.Redux;

public abstract partial class BaseStylesheet
{
    public StyleRule[] GetSheetletRules<TSheetTy>(Type sheetletTy)
    {
        var sheetlet = (Sheetlet<TSheetTy>) SandboxHelper.CreateInstance(sheetletTy);

        return sheetlet.GetRules((TSheetTy) (object) this, _config);
    }

    public StyleRule[] GetSheetletRules<T, TSheetTy>()
        where T : Sheetlet<TSheetTy>
    {
        return GetSheetletRules<TSheetTy>(typeof(T));
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
