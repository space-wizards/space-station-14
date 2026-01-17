using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Emag.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// This handles getting and displaying the laws for silicons.
/// </summary>
public abstract partial class SharedSiliconLawSystem
{
    public void InitializeEmag()
    {
        SubscribeLocalEvent<BorgChassisComponent, GotEmaggedEvent>(OnChassisEmagged);
        SubscribeLocalEvent<BorgBrainComponent, GotEmaggedEvent>(OnBrainEmagged);
    }

    private void OnChassisEmagged(Entity<BorgChassisComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction)
            || _emag.CheckFlag(ent, EmagType.Interaction))
            return;

        if (!TryComp<SiliconLawBoundComponent>(ent, out var lawboundComp))
            return;

        if (!CanBeEmagged(ent, args.UserUid, out var reason, out var emagLawcomp))
        {
            _popup.PopupClient(reason, ent, args.UserUid);
            return;
        }

        if (ent.Comp.BrainContainer.ContainedEntity is not { } brain || !TryComp<SiliconLawProviderComponent>(brain, out var brainProvider))
        {
            _popup.PopupClient(Loc.GetString("law-emag-cannot-not-brainless"), ent, args.UserUid);
            return;
        }

        var newLaws = GetEmaggedLaws(lawboundComp.Lawset.Laws, args.UserUid, lawboundComp.Lawset.ObeysTo);

        brainProvider.Subverted = true;
        SetProviderLaws((brain, brainProvider), newLaws, cue: emagLawcomp.EmaggedSound);
        Dirty(brain, brainProvider);

        emagLawcomp.OwnerName = Name(args.UserUid);

        if (_mind.TryGetMind(ent, out var mindId, out _))
            EnsureSubvertedSiliconRole(mindId);

        _stunSystem.TryUpdateParalyzeDuration(ent, emagLawcomp.StunTime);

        args.Handled = true;
    }

    private void OnBrainEmagged(Entity<BorgBrainComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction)
            || _emag.CheckFlag(ent, EmagType.Interaction))
            return;

        if (!TryComp<SiliconLawBoundComponent>(ent, out var lawboundComp)
            || !TryComp<SiliconLawProviderComponent>(ent, out var brainProvider))
            return;

        if (!CanBeEmagged(ent, args.UserUid, out var reason, out var emagLawcomp))
        {
            _popup.PopupClient(reason, ent, args.UserUid);
            return;
        }

        var newLaws = GetEmaggedLaws(lawboundComp.Lawset.Laws, args.UserUid, lawboundComp.Lawset.ObeysTo);

        brainProvider.Subverted = true;
        SetProviderLaws((ent, brainProvider), newLaws, cue: emagLawcomp.EmaggedSound);
        Dirty(ent, brainProvider);

        emagLawcomp.OwnerName = Name(args.UserUid);

        if (_mind.TryGetMind(ent, out var mindId, out _))
            EnsureSubvertedSiliconRole(mindId);

        args.Handled = true;
    }

    private bool CanBeEmagged(EntityUid entity, EntityUid user, [NotNullWhen(false)] out string? reason, [NotNullWhen(true)] out EmagSiliconLawComponent? emagComp)
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

        if (emagLawcomp.RequireOpenPanel &&
            TryComp<WiresPanelComponent>(entity, out var panel) &&
            !panel.Open)
        {
            reason = Loc.GetString("law-emag-require-panel");
            return false;
        }

        emagComp = emagLawcomp;

        return true;
    }

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
