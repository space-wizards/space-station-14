using Content.Server.Administration.Logs;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Access.Systems
{
    public sealed class IdCardSystem : SharedIdCardSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

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
                    TryComp<TransformComponent>(uid, out TransformComponent? transformComponent);
                    if (transformComponent != null)
                    {
                        _popupSystem.PopupCoordinates(Loc.GetString("id-card-component-microwave-burnt", ("id", uid)),
                         transformComponent.Coordinates, Filter.Pvs(uid), PopupType.Medium);
                        EntityManager.SpawnEntity("FoodBadRecipe",
                            transformComponent.Coordinates);
                    }
                    EntityManager.QueueDeleteEntity(uid);
                    return;
                }
                // If they're unlucky, brick their ID
                if (randomPick <= 0.25f)
                {
                    _popupSystem.PopupEntity(Loc.GetString("id-card-component-microwave-bricked", ("id", uid)),
                        uid, Filter.Pvs(uid));
                    access.Tags.Clear();
                }
                else
                {
                    _popupSystem.PopupEntity(Loc.GetString("id-card-component-microwave-safe", ("id", uid)),
                        uid, Filter.Pvs(uid), PopupType.Medium);
                }

                // Give them a wonderful new access to compensate for everything
                var random = _random.Pick(_prototypeManager.EnumeratePrototypes<AccessLevelPrototype>().ToArray());
                access.Tags.Add(random.ID);
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

                if (jobTitle.Length > SharedIdCardConsoleComponent.MaxJobTitleLength)
                    jobTitle = jobTitle[..SharedIdCardConsoleComponent.MaxJobTitleLength];
            }
            else
            {
                jobTitle = null;
            }

            id.JobTitle = jobTitle;
            Dirty(id);
            UpdateEntityName(uid, id);

            if (player != null)
            {
                _adminLogger.Add(LogType.Identity, LogImpact.Low,
                    $"{ToPrettyString(player.Value):player} has changed the job title of {ToPrettyString(id.Owner):entity} to {jobTitle} ");
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
                if (fullName.Length > SharedIdCardConsoleComponent.MaxFullNameLength)
                    fullName = fullName[..SharedIdCardConsoleComponent.MaxFullNameLength];
            }
            else
            {
                fullName = null;
            }

            id.FullName = fullName;
            Dirty(id);
            UpdateEntityName(uid, id);

            if (player != null)
            {
                _adminLogger.Add(LogType.Identity, LogImpact.Low,
                    $"{ToPrettyString(player.Value):player} has changed the name of {ToPrettyString(id.Owner):entity} to {fullName} ");
            }
            return true;
        }

        /// <summary>
        /// Changes the <see cref="Entity.Name"/> of <see cref="Component.Owner"/>.
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
            EntityManager.GetComponent<MetaDataComponent>(id.Owner).EntityName = val;
        }
    }
}
