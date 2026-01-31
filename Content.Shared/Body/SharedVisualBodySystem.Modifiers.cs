using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Body;

public abstract partial class SharedVisualBodySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    private void InitializeModifiers()
    {
        SubscribeLocalEvent<VisualBodyComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        Subs.BuiEvents<VisualBodyComponent>(HumanoidMarkingModifierKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnModifiersOpened);
                subs.Event<HumanoidMarkingModifierMarkingSetMessage>(OnSetModifiers);
            });
    }

    private void OnGetVerbs(Entity<VisualBodyComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!_admin.HasAdminFlag(args.User, AdminFlags.Fun))
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = "Modify markings",
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Customization/reptilian_parts.rsi"), "tail_smooth"),
            Act = () =>
            {
                _userInterface.OpenUi(ent.Owner, HumanoidMarkingModifierKey.Key, user);
            }
        });
    }

    /// <summary>
    /// Gathers all the markings-relevant data from this entity
    /// </summary>
    /// <param name="ent">The entity to sample</param>
    /// <param name="filter">If set, only returns data concerning the given layers</param>
    /// <param name="profiles">The profiles for the various organs</param>
    /// <param name="markings">The marking parameters for the various organs</param>
    /// <param name="applied">The markings that are applied to the entity</param>
    public bool TryGatherMarkingsData(Entity<VisualBodyComponent?> ent,
        HashSet<HumanoidVisualLayers>? filter,
        [NotNullWhen(true)] out Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>? profiles,
        [NotNullWhen(true)] out Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData>? markings,
        [NotNullWhen(true)] out Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>? applied)
    {
        if (!Resolve(ent, ref ent.Comp))
        {
            profiles = null;
            markings = null;
            applied = null;
            return false;
        }

        profiles = new();
        markings = new();
        applied = new();

        var organContainer = _container.EnsureContainer<Container>(ent, BodyComponent.ContainerID);

        foreach (var organ in organContainer.ContainedEntities)
        {
            if (!TryComp<OrganComponent>(organ, out var organComp) || organComp.Category is not { } category)
                continue;

            if (TryComp<VisualOrganComponent>(organ, out var visualOrgan))
            {
                profiles.TryAdd(category, visualOrgan.Profile);
            }

            if (TryComp<VisualOrganMarkingsComponent>(organ, out var visualOrganMarkings))
            {
                markings.TryAdd(category, visualOrganMarkings.MarkingData);
                if (filter is not null)
                    applied.TryAdd(category, visualOrganMarkings.Markings.Where(kvp => filter.Contains(kvp.Key)).ToDictionary());
                else
                    applied.TryAdd(category, visualOrganMarkings.Markings);
            }
        }

        return true;
    }

    private void OnModifiersOpened(Entity<VisualBodyComponent> ent, ref BoundUIOpenedEvent args)
    {
        TryGatherMarkingsData(ent.AsNullable(), null, out var profiles, out var markings, out var applied);

        _userInterface.SetUiState(ent.Owner, HumanoidMarkingModifierKey.Key, new HumanoidMarkingModifierState(applied!, markings!, profiles!));
    }

    private void OnSetModifiers(Entity<VisualBodyComponent> ent, ref HumanoidMarkingModifierMarkingSetMessage args)
    {
        var markingsEvt = new ApplyOrganMarkingsEvent(args.Markings);
        RaiseLocalEvent(ent, ref markingsEvt);
    }

    public void ApplyMarkings(EntityUid ent, Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings)
    {
        var markingsEvt = new ApplyOrganMarkingsEvent(markings);
        RaiseLocalEvent(ent, ref markingsEvt);
    }

    private void ApplyAppearanceTo(Entity<VisualBodyComponent?> ent, HumanoidCharacterAppearance appearance, Sex sex)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ApplyProfile(ent,
            new()
        {
            Sex = sex,
            SkinColor = appearance.SkinColor,
            EyeColor = appearance.EyeColor,
        });

        var markingsEvt = new ApplyOrganMarkingsEvent(appearance.Markings);
        RaiseLocalEvent(ent, ref markingsEvt);
    }

    public void ApplyProfileTo(Entity<VisualBodyComponent?> ent, HumanoidCharacterProfile profile)
    {
        ApplyAppearanceTo(ent, profile.Appearance, profile.Sex);
    }

    public void ApplyProfile(EntityUid ent, OrganProfileData profile)
    {
        var profileEvt = new ApplyOrganProfileDataEvent(profile, null);
        RaiseLocalEvent(ent, ref profileEvt);
    }

    public void ApplyProfiles(EntityUid ent, Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> profiles)
    {
        var profileEvt = new ApplyOrganProfileDataEvent(null, profiles);
        RaiseLocalEvent(ent, ref profileEvt);
    }
}
