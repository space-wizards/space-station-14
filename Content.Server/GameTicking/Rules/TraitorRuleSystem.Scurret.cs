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

    private void AfterScurretSpawned(Entity<GeneralScurretMayhemComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Scurret = ent.Owner;
    }

    private void HandleMakingScurretsAntags(Entity<TraitorRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var query = EntityQuery<GeneralScurretMayhemComponent>().ToList();
        foreach (var scurret in _random.GetItems(query, query.Count, false))
        {
            if (scurret.Scurret == null)
                continue;

            if (!_mind.TryGetMind(scurret.Scurret.Value, out EntityUid id, out var mind))
                continue;

            if (_role.MindIsAntagonist(id))
                continue;

            if (_random.NextFloat() > scurret.ChanceOfMayhem)
                continue;

            _antag.ForceMakeAntag<AutoTraitorComponent>(mind.Session, "TraitorWawa");
        }
    }
}
