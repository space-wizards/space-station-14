using System.Linq;
using Content.Shared._EinsteinEngine.Language.Components.Translators;
using Content.Shared.Examine;
using Content.Shared.Toggleable;

namespace Content.Shared._EinsteinEngine.Language.Systems;

public abstract class SharedTranslatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldTranslatorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, HandheldTranslatorComponent component, ExaminedEvent args)
    {
        var understoodLanguageNames = component.UnderstoodLanguages
            .Select(it => Loc.GetString($"language-{it}-name"));
        var spokenLanguageNames = component.SpokenLanguages
            .Select(it => Loc.GetString($"language-{it}-name"));
        var requiredLanguageNames = component.RequiredLanguages
            .Select(it => Loc.GetString($"language-{it}-name"));

        args.PushMarkup(Loc.GetString("translator-examined-langs-understood", ("languages", string.Join(", ", understoodLanguageNames))));
        args.PushMarkup(Loc.GetString("translator-examined-langs-spoken", ("languages", string.Join(", ", spokenLanguageNames))));

        args.PushMarkup(Loc.GetString(component.RequiresAllLanguages ? "translator-examined-requires-all" : "translator-examined-requires-any",
            ("languages", string.Join(", ", requiredLanguageNames))));

        args.PushMarkup(Loc.GetString(component.Enabled ? "translator-examined-enabled" : "translator-examined-disabled"));
    }

    protected void OnAppearanceChange(EntityUid translator, HandheldTranslatorComponent? comp = null)
    {
        if (comp == null && !TryComp(translator, out comp))
            return;

        _appearance.SetData(translator, ToggleVisuals.Toggled, comp.Enabled);
    }
}
