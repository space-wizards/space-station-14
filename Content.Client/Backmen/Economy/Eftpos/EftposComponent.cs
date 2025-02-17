// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Backmen.Economy.Eftpos;

namespace Content.Client.Backmen.Economy.Eftpos;

[RegisterComponent]
[Access(typeof(EftposSystem))]
public sealed partial class EftposComponent : SharedEftposComponent 
{

}
