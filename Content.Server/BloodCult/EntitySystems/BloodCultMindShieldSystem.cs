using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.BloodCult;

namespace Content.Server.BloodCult.EntitySystems;

/// <summary>
/// Server-only: logs blood cult deconversions for admin/antag tracking.
/// Deconversion logic runs in <see cref="Content.Shared.BloodCult.Systems.BloodCultMindShieldSystem"/> so the client can predict it.
/// </summary>
public sealed class BloodCultMindShieldSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodCultDeconvertedEvent>(OnDeconverted);
    }

    private void OnDeconverted(BloodCultDeconvertedEvent ev)
    {
        _adminLogManager.Add(LogType.Mind, LogImpact.Medium,
            $"{ToPrettyString(ev.Entity)} was deconverted from Blood Cult.");
    }
}
