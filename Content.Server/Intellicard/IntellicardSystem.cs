using Content.Server.Ghost.Roles;
using Content.Server.RandomMetadata;
using Content.Shared.Intellicard;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Verbs;

namespace Content.Server.Intellicard;

/// <summary>
/// System for handling the behaviour of intellicards.
/// </summary>
public sealed class IntellicardSystem : SharedIntellicardSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ToggleableGhostRoleSystem  _ghostRole = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly RandomMetadataSystem  _randomMeta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntellicardComponent, GetVerbsEvent<ActivationVerb>>(AddIntellicardWipeVerb);
    }

    private void AddIntellicardWipeVerb(Entity<IntellicardComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<StationAiHolderComponent>(ent.Owner, out var aiHolder))
            return;

        if (!_stationAi.TryGetHeldFromHolder((ent.Owner, aiHolder), out var held))
            return;

        var user = args.User;

        ActivationVerb verb = new()
        {
            Text = Loc.GetString(ent.Comp.WipeVerb),
            Act = () =>
            {
                _ghostRole.Wipe(held);
                _popup.PopupEntity(Loc.GetString(ent.Comp.WipePopup), user, user, PopupType.Large);
                QueueDel(held);
            }
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Gives the provided entity a random name from It's RandomMetadataComponent.
    /// </summary>
    protected override void RandomizeAiName(EntityUid uid)
    {
        if (!TryComp<RandomMetadataComponent>(uid, out var metadata))
            return;

        if (metadata.NameSegments != null)
        {
            _metaData.SetEntityName(uid, _randomMeta.GetRandomFromSegments(metadata.NameSegments, metadata.NameSeparator));
        }
    }
}
