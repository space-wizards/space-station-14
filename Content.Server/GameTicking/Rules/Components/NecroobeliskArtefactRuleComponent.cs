// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(NecroobeliskArtefactRuleSystem))]
public sealed partial class NecroobeliskArtefactRuleComponent : Component
{
    public TimeSpan StartDuration = TimeSpan.FromMinutes(1);
    public TimeSpan TimeUntilStart = TimeSpan.Zero;
    public bool IsArtefactSended = false;
}
