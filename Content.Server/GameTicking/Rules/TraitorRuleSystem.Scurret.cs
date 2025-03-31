using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Traitor.Components;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public partial class TraitorRuleSystem
{
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _role = default!;

    private void HandleMakingScurretsAntags(Entity<TraitorRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var query = EntityQueryEnumerator<GeneralScurretMayhemComponent>();
        while (query.MoveNext(out var uid, out var mayhemComp))
        {
            if (mayhemComp.Handled)
                continue;

            mayhemComp.Handled = true;

            if (!_mind.TryGetMind(uid, out var id, out var mind))
                continue;

            if (_role.MindIsAntagonist(id))
                continue;

            if (_random.NextFloat() > mayhemComp.ChanceOfMayhem)
                continue;

            _antag.ForceMakeAntag<AutoTraitorComponent>(mind.Session, "TraitorWawa");
        }
    }
}
