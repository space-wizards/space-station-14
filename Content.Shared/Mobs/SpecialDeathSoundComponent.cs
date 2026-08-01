// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;

namespace Content.Shared.Mobs;

/// <summary>
///     Заменяет стандартный звук deathgasp владельца кастомным звуком,
///     пока сущность с этим компонентом надета в слоте маски.
/// </summary>
[RegisterComponent]
public sealed partial class SpecialDeathSoundComponent : Component
{
    [DataField("sound", required: true)]
    public SoundSpecifier Sound = default!;
}
