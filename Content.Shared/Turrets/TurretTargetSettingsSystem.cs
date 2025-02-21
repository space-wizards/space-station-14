using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Turrets;

public sealed partial class TurretTargetSettingsSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    private ProtoId<AccessLevelPrototype> _accessLevelBorg = "Borg";
    private ProtoId<AccessLevelPrototype> _accessLevelBasicSilicon = "BasicSilicon";

    public override void Initialize()
    {
        base.Initialize();
    }

    public void SetAccessLevelExemption(Entity<TurretTargetSettingsComponent> ent, ProtoId<AccessLevelPrototype> exemption, bool enabled)
    {
        if (enabled)
            ent.Comp.ExemptAccessLevels.Add(exemption);
        else
            ent.Comp.ExemptAccessLevels.Remove(exemption);
    }

    public void SetAccessLevelExemptions(Entity<TurretTargetSettingsComponent> ent, ICollection<ProtoId<AccessLevelPrototype>> exemptions, bool enabled)
    {
        foreach (var exemption in exemptions)
            SetAccessLevelExemption(ent, exemption, enabled);
    }

    public void SyncAccessLevelExemptions(Entity<TurretTargetSettingsComponent> target, ICollection<ProtoId<AccessLevelPrototype>> exemptions)
    {
        target.Comp.ExemptAccessLevels.Clear();
        SetAccessLevelExemptions(target, exemptions, true);
    }

    public void SyncAccessLevelExemptions(Entity<TurretTargetSettingsComponent> target, Entity<TurretTargetSettingsComponent> source)
    {
        SyncAccessLevelExemptions(target, source.Comp.ExemptAccessLevels);
    }

    public bool HasAccessLevelExemption(Entity<TurretTargetSettingsComponent> ent, ProtoId<AccessLevelPrototype> exemption)
    {
        if (ent.Comp.ExemptAccessLevels.Count == 0)
            return false;

        return ent.Comp.ExemptAccessLevels.Contains(exemption);
    }

    public bool HasAnyAccessLevelExemption(Entity<TurretTargetSettingsComponent> ent, ICollection<ProtoId<AccessLevelPrototype>> exemptions)
    {
        if (ent.Comp.ExemptAccessLevels.Count == 0)
            return false;

        foreach (var exemption in exemptions)
        {
            if (HasAccessLevelExemption(ent, exemption))
                return true;
        }

        return false;
    }

    public bool EntityIsTargetForTurret(Entity<TurretTargetSettingsComponent> ent, EntityUid target)
    {
        var accessLevels = _accessReader.FindAccessTags(target);

        if (accessLevels.Contains(_accessLevelBorg))
            return !HasAccessLevelExemption(ent, _accessLevelBorg);

        if (accessLevels.Contains(_accessLevelBasicSilicon))
            return !HasAccessLevelExemption(ent, _accessLevelBasicSilicon);

        return !HasAnyAccessLevelExemption(ent, accessLevels);
    }
}
