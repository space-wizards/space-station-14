// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Shared.DeadSpace.Languages.Components;
using Content.Shared.Implants;

namespace Content.Server.DeadSpace.Languages;

public sealed class LearnLanguageImplantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LearnLanguageImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
    }

    private void OnImplantImplanted(Entity<LearnLanguageImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (TryComp<LanguageComponent>(args.Implanted, out var language))
        {
            language.KnownLanguages.UnionWith(ent.Comp.Language);
        }
        else
        {
            AddComp(args.Implanted, new LanguageComponent
            {
                KnownLanguages = ent.Comp.Language,
                SelectedLanguage = ent.Comp.Language.First()
            });
        }
    }
}
