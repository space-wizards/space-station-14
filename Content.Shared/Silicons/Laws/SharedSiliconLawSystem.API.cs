using Content.Shared.Emag.Systems;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// This handles getting and displaying the laws for silicons.
/// </summary>
public abstract partial class SharedSiliconLawSystem
{
    protected void EnsureSubvertedSiliconRole(EntityUid mindId)
    {
        if (!_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindAddRole(mindId, "MindRoleSubvertedSilicon", silent: true);
    }

    protected void RemoveSubvertedSiliconRole(EntityUid mindId)
    {
        if (_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindRemoveRole<SubvertedSiliconRoleComponent>(mindId);
    }

    /// <summary>
    /// Refreshes the laws of target entity and tries to link their <see cref="SiliconLawBoundComponent"/> to a <see cref="SiliconLawProviderComponent"/>
    /// </summary>
    /// <param name="ent"></param>
    public void FetchLawset(Entity<SiliconLawBoundComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var ev = new GetSiliconLawsEvent(ent);

        RaiseLocalEvent(ent, ref ev);
        if (ev.Handled)
        {
            LinkToProvider(ent, ev.LinkedEntity ?? ent);
            return;
        }

        var xform = Transform(ent);

        if (_station.GetOwningStation(ent, xform) is { } station)
        {
            RaiseLocalEvent(station, ref ev);
            if (ev.Handled)
            {
                LinkToProvider(ent, ev.LinkedEntity ?? station);
                return;
            }
        }

        if (xform.GridUid is { } grid)
        {
            RaiseLocalEvent(grid, ref ev);
            if (ev.Handled)
            {
                LinkToProvider(ent, ev.LinkedEntity ?? grid);
                return;
            }
        }

        RaiseLocalEvent(ref ev);
        if (ev.Handled)
        {
            LinkToProvider(ent, ev.LinkedEntity ?? ent);
        }
    }

    /// <summary>
    /// Get the current laws of this silicon.
    /// </summary>
    /// <param name="ent">The silicon to get the laws of.</param>
    /// <returns>The lawset.</returns>
    public SiliconLawset GetProviderLaws(Entity<SiliconLawProviderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new SiliconLawset();

        return ent.Comp.Lawset;
    }

    /// <summary>
    /// Get the current laws of this silicon.
    /// </summary>
    /// <param name="ent">The silicon to get the laws of.</param>
    /// <returns>The lawset.</returns>
    public SiliconLawset GetBoundLaws(Entity<SiliconLawBoundComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new SiliconLawset();

        return ent.Comp.Lawset;
    }

    /// <summary>
    /// Extract all the laws from a lawset's prototype ids.
    /// </summary>
    public SiliconLawset GetLawset(ProtoId<SiliconLawsetPrototype>? lawset)
    {
        if (!_prototype.TryIndex(lawset, out var proto))
            return new SiliconLawset();

        var laws = new SiliconLawset()
        {
            Laws = new List<SiliconLaw>(proto.Laws.Count)
        };
        foreach (var law in proto.Laws)
        {
            laws.Laws.Add(_prototype.Index<SiliconLawPrototype>(law).ShallowClone());
        }
        laws.ObeysTo = proto.ObeysTo;

        return laws;
    }

    public void SetProviderLaws(Entity<SiliconLawProviderComponent?> ent, List<SiliconLaw> newLaws, bool silent = false, SoundSpecifier? cue = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        cue ??= ent.Comp.LawUploadSound;

        ent.Comp.Lawset.Laws = newLaws;
        SyncToLawBound(ent, silent ? null : cue);
    }

    /// <summary>
    /// Set the laws of a silicon entity while notifying the player.
    /// </summary>
    public void UpdateLaws(Entity<SiliconLawBoundComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!TryComp<SiliconLawProviderComponent>(ent.Comp.LawsetProvider, out var provider))
            return;

        ent.Comp.Lawset = provider.Lawset.Clone();
        ent.Comp.Subverted = provider.Subverted;
        Dirty(ent);
    }

    public void LinkToProvider(Entity<SiliconLawBoundComponent?> lawboundEnt,
        Entity<SiliconLawProviderComponent?> providerEnt)
    {
        if (!Resolve(providerEnt, ref providerEnt.Comp, false))
            return;

        if (!Resolve(lawboundEnt, ref lawboundEnt.Comp, false))
            return;

        UnlinkFromProvider(lawboundEnt);

        var ev = new SiliconLawProviderChanged(providerEnt, lawboundEnt.Comp.LawsetProvider);
        RaiseLocalEvent(lawboundEnt, ref ev);

        providerEnt.Comp.ExternalLawsets.Add(lawboundEnt.Owner);
        lawboundEnt.Comp.LawsetProvider = providerEnt;
        UpdateLaws(lawboundEnt);
        Dirty(providerEnt);
    }

    public void UnlinkFromProvider(Entity<SiliconLawBoundComponent?> lawboundEnt,
        Entity<SiliconLawProviderComponent?> providerEnt)
    {
        if (!Resolve(providerEnt, ref providerEnt.Comp, false))
            return;

        if (!Resolve(lawboundEnt, ref lawboundEnt.Comp, false))
            return;

        if (lawboundEnt.Comp.LawsetProvider != null)
        {
            var ev = new SiliconLawProviderUnlinked(lawboundEnt.Comp.LawsetProvider.Value);
            RaiseLocalEvent(lawboundEnt, ref ev);
        }

        providerEnt.Comp.ExternalLawsets.Remove(lawboundEnt.Owner);
        lawboundEnt.Comp.LawsetProvider = null;
        Dirty(lawboundEnt);
        Dirty(providerEnt);
    }

    public void UnlinkFromProvider(Entity<SiliconLawBoundComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (TryComp<SiliconLawProviderComponent>(ent.Comp.LawsetProvider, out var provider))
        {
            provider.ExternalLawsets.Remove(ent);
            Dirty(ent.Comp.LawsetProvider.Value, provider);
        }

        ent.Comp.LawsetProvider = null;

        Dirty(ent);
    }

    private void SyncToLawBound(Entity<SiliconLawProviderComponent?> ent, SoundSpecifier? cue = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // We don't wanna iterate on the pure external lawsets cause we remove them in iteration.
        var iteratedEntities = ent.Comp.ExternalLawsets;

        foreach (var lawboundEnt in iteratedEntities)
        {
            if (!TryComp<SiliconLawBoundComponent>(lawboundEnt, out var lawboundComp))
            {
                UnlinkFromProvider((lawboundEnt, lawboundComp));
                continue;
            }

            lawboundComp.LawsetProvider = ent.Owner;
            UpdateLaws((lawboundEnt, lawboundComp));
            NotifyLawsChanged(lawboundEnt, cue);
        }

        Dirty(ent);
    }

    public virtual void NotifyLawsChanged(EntityUid uid, SoundSpecifier? cue = null)
    {

    }

    public virtual void NotifyLaws(EntityUid uid, SoundSpecifier? cue = null)
    {

    }
}
