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
        SubscribeLocalEvent<BorgChassisComponent, GotEmaggedEvent>(OnChassisEmagged); // Inability to emag brain directly is intentional.
    }

    private void OnChassisEmagged(Entity<BorgChassisComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction)
            || _emag.CheckFlag(ent, EmagType.Interaction))
            return;

        if (!TryComp<SiliconLawBoundComponent>(ent, out var lawboundComp)
            || !TryComp<EmagSiliconLawComponent>(ent, out var emagLawcomp))
            return;

        // If we want to affect the provider, and the chassis has no brain inserted, nothing to modify.
        if (emagLawcomp.AffectProvider || !HasComp<SiliconLawProviderComponent>(ent.Comp.BrainContainer.ContainedEntity))
        {
            _popup.PopupClient(Loc.GetString("law-emag-cannot-emag-chassis-no-provider"), ent, args.UserUid);
            return;
        }

        // prevent self-emagging
        if (ent.Owner == args.UserUid)
        {
            _popup.PopupClient(Loc.GetString("law-emag-cannot-emag-self"), ent, args.UserUid);
            return;
        }

        if (emagLawcomp.RequireOpenPanel &&
            TryComp<WiresPanelComponent>(ent, out var panel) &&
            !panel.Open)
        {
            _popup.PopupClient(Loc.GetString("law-emag-require-panel"), ent, args.UserUid);
            return;
        }

        var newLaws = GetEmaggedLaws(lawboundComp.Lawset.Laws, args.UserUid, lawboundComp.Lawset.ObeysTo);

        if (emagLawcomp.AffectProvider && TryComp<SiliconLawProviderComponent>(lawboundComp.LawsetProvider, out var lawProvider))
        {
            lawProvider.Subverted = true;
            SetProviderLaws((lawboundComp.LawsetProvider.Value, lawProvider), newLaws, cue: emagLawcomp.EmaggedSound);
            Dirty(lawboundComp.LawsetProvider.Value, lawProvider);
        }
        else
        {
            EnsureComp<SiliconLawProviderComponent>(ent, out var ensuredProvider);
            ensuredProvider.Subverted = true;
            SetProviderLaws((ent.Owner, ensuredProvider), newLaws, cue: emagLawcomp.EmaggedSound);
            LinkToProvider((ent, lawboundComp), (ent, ensuredProvider));
        }

        emagLawcomp.OwnerName = Name(args.UserUid);

        if(_mind.TryGetMind(ent, out var mindId, out _))
            EnsureSubvertedSiliconRole(mindId);

        _stunSystem.TryUpdateParalyzeDuration(ent, emagLawcomp.StunTime);

        args.Handled = true;
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
