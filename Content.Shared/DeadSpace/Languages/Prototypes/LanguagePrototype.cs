// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.DeadSpace.Languages.Prototypes;

[Prototype("language")]
public sealed partial class LanguagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public SpriteSpecifier? Icon = null;

    [DataField]
    public List<string> Lexicon = new();

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public bool GenerateTTSForLexicon = true;

    [DataField]
    public SoundSpecifier? LexiconSound;
}
