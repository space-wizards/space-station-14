using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;

namespace Content.Shared.Silicons.Laws;

public abstract partial class SharedSiliconLawSystem
{
    public void InitializeEmag()
    {
        SubscribeLocalEvent<BorgChassisComponent, GotEmaggedEvent>(OnChassisEmagged);
        SubscribeLocalEvent<BorgBrainComponent, GotEmaggedEvent>(OnBrainEmagged);
    }

    private void OnChassisEmagged(Entity<BorgChassisComponent> ent, ref GotEmaggedEvent args)
    {
        // Is it the correct emag type?
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        // Determine the provider
        EntityUid providerUid;
        SiliconLawProviderComponent provider;

        // 1. Check if chassis is provider
        if (TryComp<SiliconLawProviderComponent>(ent, out var chassisProvider))
        {
            providerUid = ent.Owner;
            provider = chassisProvider;
        }
        // 2. Check if brain is provider
        else if (ent.Comp.BrainEntity is { } brain && TryComp<SiliconLawProviderComponent>(brain, out var brainProvider))
        {
            providerUid = brain;
            provider = brainProvider;
        }
        else
        {
            // If no brain and chassis is not provider
            if (ent.Comp.BrainEntity == null)
                _popup.PopupClient(Loc.GetString("law-emag-cannot-brainless", ("entity", ent)), ent, args.UserUid);

            return;
        }

        // Check if provider is already emagged
        if (_emag.CheckFlag(providerUid, EmagType.Interaction))
        {
            _popup.PopupClient(Loc.GetString("law-emag-already-emagged", ("entity", providerUid)), ent, args.UserUid);
            return;
        }

        // Check if provider has EmagSiliconLawComponent
        // We pass the chassis to check the panel
        if (!CanBeEmagged(providerUid, args.UserUid, out var reason, out var emagLawcomp, ent.Owner))
        {
            _popup.PopupClient(reason, ent, args.UserUid);
            return;
        }

        // Check for mind on the provider (Brain or Chassis)
        EntityUid? mindId = null;
        if (_mind.TryGetMind(ent.Owner, out var mind, out _)) // Check chassis for a mind first.
        {
            mindId = mind;
        }
        else if (ent.Comp.BrainEntity is { } brainId && _mind.TryGetMind(brainId, out mind, out _)) // Then check the brain.
        {
            mindId = mind;
        }

        if (mindId == null)
        {
            _popup.PopupClient(Loc.GetString("law-emag-require-mind", ("entity", providerUid)), ent, args.UserUid);
            return;
        }

        var newLaws = GetEmaggedLaws(provider.Lawset.Laws, args.UserUid, provider.Lawset.ObeysTo);

        provider.Subverted = true;
        SetProviderLaws((providerUid, provider), newLaws, cue: emagLawcomp.EmaggedSound);
        Dirty(providerUid, provider);

        emagLawcomp.OwnerName = Name(args.UserUid);

        EnsureSubvertedSiliconRole(mindId.Value);

        _stunSystem.TryUpdateParalyzeDuration(ent, emagLawcomp.StunTime);

        args.Handled = true;

        // If the provider is not the chassis (i.e. it's the brain), we don't want the chassis to get the EmaggedComponent.
        // But the brain should still get it.
        if (providerUid != ent.Owner)
        {
            args.Repeatable = true;
            EnsureComp<EmaggedComponent>(providerUid, out var emaggedComp);
            emaggedComp.EmagType |= EmagType.Interaction;
            Dirty(providerUid, emaggedComp);
        }
    }

    private void OnBrainEmagged(Entity<BorgBrainComponent> ent, ref GotEmaggedEvent args)
    {
        // Is it the correct emag type?
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        // Check if brain is already emagged
        if (_emag.CheckFlag(ent, EmagType.Interaction))
        {
            _popup.PopupClient(Loc.GetString("law-emag-already-emagged", ("entity", ent)), ent, args.UserUid);
            return;
        }

        if (!TryComp<SiliconLawBoundComponent>(ent, out var lawboundComp)
            || !TryComp<SiliconLawProviderComponent>(ent, out var brainProvider))
            return;

        if (!CanBeEmagged(ent, args.UserUid, out var reason, out var emagLawcomp))
        {
            _popup.PopupClient(reason, ent, args.UserUid);
            return;
        }

        // The brain must have a mind to be emagged.
        if (!_mind.TryGetMind(ent, out var mindId, out _))
        {
            _popup.PopupClient(Loc.GetString("law-emag-require-mind", ("entity", ent)), ent, args.UserUid);
            return;
        }

        var newLaws = GetEmaggedLaws(brainProvider.Lawset.Laws, args.UserUid, brainProvider.Lawset.ObeysTo);

        brainProvider.Subverted = true;
        SetProviderLaws((ent, brainProvider), newLaws, cue: emagLawcomp.EmaggedSound);
        Dirty(ent, brainProvider);

        emagLawcomp.OwnerName = Name(args.UserUid);

        EnsureSubvertedSiliconRole(mindId);

        args.Handled = true;
    }

    /// <summary>
    /// Basic checks for if a lawbound entity can be emagged.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="user">The person doing the emagging.</param>
    /// <param name="reason">The reason the emagging cannot be performed.</param>
    /// <param name="emagComp">The EmagSiliconLawComponent, for convenience.</param>
    /// <param name="chassis">The chassis entity, if any, to check for panel access.</param>
    /// <returns>True if the silicon can be emagged, false otherwise.</returns>
    private bool CanBeEmagged(EntityUid entity, EntityUid user, [NotNullWhen(false)] out string? reason, [NotNullWhen(true)] out EmagSiliconLawComponent? emagComp, EntityUid? chassis = null)
    {
        reason = null;
        emagComp = null;
        if (!TryComp<EmagSiliconLawComponent>(entity, out var emagLawcomp))
        {
            reason =  Loc.GetString("law-emag-cannot-not-emaggable", ("entity", entity));
            return false;
        }

        // prevent self-emagging
        if (entity == user)
        {
            reason = Loc.GetString("law-emag-cannot-emag-self");
            return false;
        }

        if (emagLawcomp.RequireOpenPanel)
        {
            var targetForPanel = chassis ?? entity;
            if (TryComp<WiresPanelComponent>(targetForPanel, out var panel) && !panel.Open)
            {
                reason = Loc.GetString("law-emag-require-panel");
                return false;
            }
        }

        emagComp = emagLawcomp;

        return true;
    }

    /// <summary>
    /// Constructs the emagged laws based on the provided SiliconLaw list.
    /// </summary>
    public List<SiliconLaw> GetEmaggedLaws(List<SiliconLaw> laws, EntityUid user, string obeysTo)
    {
        var lawsToSwap = laws;

        // Add the first emag law before the others
        lawsToSwap.Insert(0, new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-custom", ("name", Name(user)), ("title", Loc.GetString(obeysTo))),
            Order = 0
        });

        //Add the secrecy law after the others
        lawsToSwap.Add(new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-secrecy", ("faction", Loc.GetString(obeysTo))),
            Order = lawsToSwap.Max(law => law.Order) + 1
        });

        return lawsToSwap;
    }
}
