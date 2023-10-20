using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Access.Systems
{
    public sealed class IdCardSystem : SharedIdCardSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly MetaDataSystem _metaSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<IdCardComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<IdCardComponent, BeingMicrowavedEvent>(OnMicrowaved);
        }

        private void OnMapInit(EntityUid uid, IdCardComponent id, MapInitEvent args)
        {
            UpdateEntityName(uid, id);
        }

        private void OnMicrowaved(EntityUid uid, IdCardComponent component, BeingMicrowavedEvent args)
        {
            if (TryComp<AccessComponent>(uid, out var access))
            {
                float randomPick = _random.NextFloat();
                // if really unlucky, burn card
                if (randomPick <= 0.15f)
                {
                    TryComp(uid, out TransformComponent? transformComponent);
                    if (transformComponent != null)
                    {
                        _popupSystem.PopupCoordinates(Loc.GetString("id-card-component-microwave-burnt", ("id", uid)),
                         transformComponent.Coordinates, PopupType.Medium);
                        EntityManager.SpawnEntity("FoodBadRecipe",
                            transformComponent.Coordinates);
                    }
                    _adminLogger.Add(LogType.Action, LogImpact.Medium,
                        $"{ToPrettyString(args.Microwave)} burnt {ToPrettyString(uid):entity}");
                    EntityManager.QueueDeleteEntity(uid);
                    return;
                }
                // If they're unlucky, brick their ID
                if (randomPick <= 0.25f)
                {
                    _popupSystem.PopupEntity(Loc.GetString("id-card-component-microwave-bricked", ("id", uid)), uid);

                    access.Tags.Clear();
                    Dirty(access);

                    _adminLogger.Add(LogType.Action, LogImpact.Medium,
                        $"{ToPrettyString(args.Microwave)} cleared access on {ToPrettyString(uid):entity}");
                }
                else
                {
                    _popupSystem.PopupEntity(Loc.GetString("id-card-component-microwave-safe", ("id", uid)), uid, PopupType.Medium);
                }

                // Give them a wonderful new access to compensate for everything
                var random = _random.Pick(_prototypeManager.EnumeratePrototypes<AccessLevelPrototype>().ToArray());

                access.Tags.Add(random.ID);
                Dirty(access);

                _adminLogger.Add(LogType.Action, LogImpact.Medium,
                        $"{ToPrettyString(args.Microwave)} added {random.ID} access to {ToPrettyString(uid):entity}");
            }
        }

        /// <summary>
        /// Attempts to change the job title of a card.
        /// Returns true/false.
        /// </summary>
        /// <remarks>
        /// If provided with a player's EntityUid to the player parameter, adds the change to the admin logs.
        /// </remarks>
        public bool TryChangeJobTitle(EntityUid uid, string? jobTitle, IdCardComponent? id = null, EntityUid? player = null)
        {
            if (!Resolve(uid, ref id))
                return false;

            if (!string.IsNullOrWhiteSpace(jobTitle))
            {
                jobTitle = jobTitle.Trim();

                if (jobTitle.Length > IdCardConsoleComponent.MaxJobTitleLength)
                    jobTitle = jobTitle[..IdCardConsoleComponent.MaxJobTitleLength];
            }
            else
            {
                jobTitle = null;
            }

            if (id.JobTitle == jobTitle)
                return true;
            id.JobTitle = jobTitle;
            Dirty(id);
            UpdateEntityName(uid, id);

            if (player != null)
            {
                _adminLogger.Add(LogType.Identity, LogImpact.Low,
                    $"{ToPrettyString(player.Value):player} has changed the job title of {ToPrettyString(uid):entity} to {jobTitle} ");
            }
            return true;
        }

        public bool TryChangeJobIcon(EntityUid uid, StatusIconPrototype jobIcon, IdCardComponent? id = null, EntityUid? player = null)
        {
            if (!Resolve(uid, ref id))
            {
                return false;
            }

            if (id.JobIcon == jobIcon.ID)
            {
                return true;
            }

            id.JobIcon = jobIcon.ID;
            Dirty(uid, id);

            if (player != null)
            {
                _adminLogger.Add(LogType.Identity, LogImpact.Low,
                    $"{ToPrettyString(player.Value):player} has changed the job icon of {ToPrettyString(uid):entity} to {jobIcon} ");
            }

            return true;
        }

        /// <summary>
        /// Attempts to change the full name of a card.
        /// Returns true/false.
        /// </summary>
        /// <remarks>
        /// If provided with a player's EntityUid to the player parameter, adds the change to the admin logs.
        /// </remarks>
        public bool TryChangeFullName(EntityUid uid, string? fullName, IdCardComponent? id = null, EntityUid? player = null)
        {
            if (!Resolve(uid, ref id))
                return false;

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                fullName = fullName.Trim();
                if (fullName.Length > IdCardConsoleComponent.MaxFullNameLength)
                    fullName = fullName[..IdCardConsoleComponent.MaxFullNameLength];
            }
            else
            {
                fullName = null;
            }

            if (id.FullName == fullName)
                return true;
            id.FullName = fullName;
            Dirty(id);
            UpdateEntityName(uid, id);

            if (player != null)
            {
                _adminLogger.Add(LogType.Identity, LogImpact.Low,
                    $"{ToPrettyString(player.Value):player} has changed the name of {ToPrettyString(uid):entity} to {fullName} ");
            }
            return true;
        }

        /// <summary>
        /// Changes the name of the id's owner.
        /// </summary>
        /// <remarks>
        /// If either <see cref="FullName"/> or <see cref="JobTitle"/> is empty, it's replaced by placeholders.
        /// If both are empty, the original entity's name is restored.
        /// </remarks>
        private void UpdateEntityName(EntityUid uid, IdCardComponent? id = null)
        {
            if (!Resolve(uid, ref id))
                return;

            var jobSuffix = string.IsNullOrWhiteSpace(id.JobTitle) ? string.Empty : $" ({id.JobTitle})";

            var val = string.IsNullOrWhiteSpace(id.FullName)
                ? Loc.GetString("access-id-card-component-owner-name-job-title-text",
                    ("jobSuffix", jobSuffix))
                : Loc.GetString("access-id-card-component-owner-full-name-job-title-text",
                    ("fullName", id.FullName),
                    ("jobSuffix", jobSuffix));
            _metaSystem.SetEntityName(uid, val);
        }
    }
}
