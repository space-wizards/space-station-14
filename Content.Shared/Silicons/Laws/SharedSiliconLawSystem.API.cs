using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Overlays;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws;

public abstract partial class SharedSiliconLawSystem
{
    /// <summary>
    /// Gives the mind the subverted silicon mindrole.
    /// </summary>
    /// <param name="mindId">The ID of the mind.</param>
    protected void EnsureSubvertedSiliconRole(EntityUid mindId)
    {
        if (!_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindAddRole(mindId, "MindRoleSubvertedSilicon", silent: true);
    }

    /// <summary>
    /// Removes the subverted silicon role from a mind.
    /// </summary>
    /// <param name="mindId">The ID of the mind.</param>
    protected void RemoveSubvertedSiliconRole(EntityUid mindId)
    {
        if (_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindRemoveRole<SubvertedSiliconRoleComponent>(mindId);
    }

    /// <summary>
    /// Refreshes the laws of target entity and tries to link their <see cref="SiliconLawBoundComponent"/> to a <see cref="SiliconLawProviderComponent"/>
    /// </summary>
    /// <param name="ent">The entity to fetch the lawset for.</param>
    public void FetchLawset(Entity<SiliconLawBoundComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var ev = new GetSiliconLawsEvent(ent);

        // First, we check if our own entity can supply the lawset.
        RaiseLocalEvent(ent, ref ev);
        if (ev.Handled)
        {
            LinkToProvider(ent, ev.LinkedEntity ?? ent);
            return;
        }

        var xform = Transform(ent);

        // If our entity cannot supply the lawset, we see if the station can.
        if (_station.GetOwningStation(ent, xform) is { } station)
        {
            RaiseLocalEvent(station, ref ev);
            if (ev.Handled)
            {
                LinkToProvider(ent, ev.LinkedEntity ?? station);
                return;
            }
        }

        // If the station cannot, we check the grid.
        if (xform.GridUid is { } grid)
        {
            RaiseLocalEvent(grid, ref ev);
            if (ev.Handled)
            {
                LinkToProvider(ent, ev.LinkedEntity ?? grid);
                return;
            }
        }

        // If all else fails, we broadcast in hopes of finding a lawset.
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

    /// <summary>
    /// Sets the laws of a law provider to a new list.
    /// Updates the laws of all linked LawBound entities.
    /// </summary>
    /// <param name="ent">The law provider.</param>
    /// <param name="newLaws">List of new laws.</param>
    /// <param name="silent">Whether to play a notification for all linked LawBounds.</param>
    /// <param name="cue">The sound the notification should be. If null, uses the default sound.</param>
    public void SetProviderLaws(Entity<SiliconLawProviderComponent?> ent, List<SiliconLaw> newLaws, bool silent = false, SoundSpecifier? cue = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        cue ??= ent.Comp.LawUploadSound;

        ent.Comp.Lawset.Laws = newLaws;
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"The provider laws {ent} have been set to {ent.Comp.Lawset.LoggingString() ?? "Empty"}.");
        SyncToLawBound(ent, silent ? null : cue);
    }

    /// <summary>
    /// Refreshes the laws of an entity based on the linked provider.
    /// Does nothing if no provider is linked.
    /// </summary>
    public void UpdateLaws(Entity<SiliconLawBoundComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!TryComp<SiliconLawProviderComponent>(ent.Comp.LawsetProvider, out var provider))
            return;

        ent.Comp.Subverted = provider.Subverted;

        if (ent.Comp.Lawset.Laws != provider.Lawset.Laws)
        {
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"The silicon laws of {ent} have been updated to {provider.Lawset.LoggingString() ?? "Empty"}");
        }

        ent.Comp.Lawset = provider.Lawset.Clone();

        if (TryComp<ShowCrewIconsComponent>(ent, out var crewIcons))
        {
            crewIcons.UncertainCrewBorder = ent.Comp.Subverted;
            Dirty(ent, crewIcons);
        }

        Dirty(ent);
    }

    /// <summary>
    /// Links a lawbound entity to a new law provider.
    /// Updates laws based on the new provider.
    /// </summary>
    /// <param name="lawboundEnt">The lawbound entity.</param>
    /// <param name="providerEnt">The provider to link it to.</param>
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

    /// <summary>
    /// Unlinks a lawbound entity from the provided law provider.
    /// Leaves the laws as they were before unlinking.
    /// </summary>
    /// <param name="lawboundEnt">The lawbound entity.</param>
    /// <param name="providerEnt">The provider to unlink from.</param>
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

    /// <summary>
    /// Unlinks the lawbound entity from itss provider.
    /// Leaves the laws as they were before unlinking.
    /// </summary>
    /// <param name="ent">The lawbound entity.</param>
    public void UnlinkFromProvider(Entity<SiliconLawBoundComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        // If our provider is valid, clear the references in it as well.
        if (TryComp<SiliconLawProviderComponent>(ent.Comp.LawsetProvider, out var provider))
        {
            provider.ExternalLawsets.Remove(ent);
            Dirty(ent.Comp.LawsetProvider.Value, provider);
        }

        ent.Comp.LawsetProvider = null;

        Dirty(ent);
    }

    /// <summary>
    /// Synchronizes the laws of the law provider to all the entities bound to it.
    /// </summary>
    /// <param name="ent">The law provider.</param>
    /// <param name="cue">The notification sound to play to lawbound entities. If null, no sound is used.</param>
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

    /// <summary>
    /// Notifies the provided entity its laws have been updated.
    /// </summary>
    /// <param name="uid">The entity to notify.</param>
    /// <param name="cue">The sound to play. Will play no sound if null.</param>
    public virtual void NotifyLawsChanged(EntityUid uid, SoundSpecifier? cue = null)
    {

    }

    public virtual void NotifyLaws(EntityUid uid, SoundSpecifier? cue = null)
    {

    }
}
