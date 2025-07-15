// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.DeadSpace.SignatureOnPaper.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SignatureToolComponent : Component
{
    [DataField]
    public SoundSpecifier? Sound { get; private set; } = new SoundCollectionSpecifier("PaperScribbles", AudioParams.Default.WithVariation(0.1f));
}
