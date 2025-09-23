using Content.Shared._Starlight.Character.Info.Components;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Starlight.CCVar;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Character.Info;

/// <summary>
/// Handles reading/writing character info (like custom descriptions, secrets, etc.)
/// </summary>
public abstract class SLSharedCharacterInfoSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    private bool _characterWindowEnabled = false;
    private bool _flavorTextEnabled = false;
    private bool _exploitableSecretsEnabled = false;

    public override void Initialize()
    {
        _configManager.OnValueChanged(CCVars.FlavorText, b => { _flavorTextEnabled = b; }, true);
        _configManager.OnValueChanged(StarlightCCVars.ExploitableSecrets, b => { _exploitableSecretsEnabled = b; },
            true);
        _configManager.OnValueChanged(StarlightCCVars.CharacterInspectWindowEnabled, b => { _characterWindowEnabled = b; }, true);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<ActivateImplantEvent>(OnActivateImplant);

        SubscribeLocalEvent<ExploitableInfoComponent, GetVerbsEvent<ExamineVerb>>(OnExamineExploitableInfo);
        SubscribeLocalEvent<HumanoidAppearanceComponent, GetVerbsEvent<ExamineVerb>>(OnExamineCharacter);
        SubscribeLocalEvent<MindSecretsComponent, ComponentGetStateAttemptEvent>(AttemptSyncMindSecrets);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        ApplyCharacterInfo(ev.Mob, ev.Profile);
    }

    public void ApplyCharacterInfo(EntityUid Mob, HumanoidCharacterProfile Profile)
    {
        var character = Profile;
        var newMind = _mindSystem.GetMind(Mob);
        if (newMind != null && TryComp(newMind, out MindComponent? mindComp))
        {
            mindComp.Voice = character.Voice;
            mindComp.SiliconVoice = character.SiliconVoice;
            if (_configManager.GetCVar(CCVars.FlavorText))
            {
                var personalityDescription = new CharacterDescriptionComponent
                {
                    Description = character.PersonalityDescription,
                };
                AddComp(newMind.Value, personalityDescription, true);
            }

            if (_configManager.GetCVar(StarlightCCVars.OOCNotes))
            {
                var roleplayInfo = new RoleplayInfoComponent { OOCNotes = character.OOCNotes };
                AddComp(newMind.Value, roleplayInfo);

                //Setup mindInfo
                var mindSecrets = new MindSecretsComponent { PersonalNotes = character.PersonalNotes, };
                AddComp(newMind.Value, mindSecrets);
            }
        }

        if (_configManager.GetCVar(CCVars.FlavorText))
        {
            var charDescription = new CharacterDescriptionComponent { Description = character.PhysicalDescription, };
            AddComp(Mob, charDescription, true);
        }

        if (_configManager.GetCVar(StarlightCCVars.ExploitableSecrets))
        {
            var exploitable = new ExploitableInfoComponent() { Info = character.ExploitableInfo, };
            AddComp(Mob, exploitable, true);
        }

        if (_configManager.GetCVar(StarlightCCVars.ICSecrets))
        {
            //Setup playermob info
            var charSecrets = new CharacterSecretsComponent { Secrets = character.Secrets };
            AddComp(Mob, charSecrets, true);
        }
    }

    private void OnActivateImplant(ActivateImplantEvent ev)
    {
        if (!HasComp<DnaScrambleOnTriggerComponent>(ev.Action.Comp.Container))
            return;

        if (TryComp(ev.Performer, out ExploitableInfoComponent? exploitable))
        {
            exploitable.Info = string.Empty;
            Dirty(ev.Performer, exploitable);
        }

        if (TryComp(ev.Performer, out CharacterDescriptionComponent? characterDescription))
        {
            characterDescription.Description = string.Empty;
            Dirty(ev.Performer, characterDescription);
        }

        //Also scramble the personality/mind flavortext.
        //This isn't ideal but adding conditional disabling would be more complicated so this works for now
        if (!TryComp(ev.Performer, out MindContainerComponent? mindCont)
            || !mindCont.HasMind
            || !TryComp(mindCont.Mind, out CharacterDescriptionComponent? personalityDescription)) return;

        personalityDescription.Description = string.Empty;
        Dirty(mindCont.Mind.Value, personalityDescription);
    }

    private void AttemptSyncMindSecrets(EntityUid uid, MindSecretsComponent component,
        ref ComponentGetStateAttemptEvent args)
    {
        var playerMind = args.Player?.GetMind();
        if (playerMind == null)
            return;
        args.Cancelled = playerMind != uid;
    }

    private void OnExamineCharacter(Entity<HumanoidAppearanceComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;
        var user = args.User;

        var detailsRange = _examineSystem.IsInDetailsRange(args.User, ent);

        if (_characterWindowEnabled)
        {
            args.Verbs.Add(new ExamineVerb
            {
                Act = () =>
                {
                    OpenCharacterWindow(ent, user);
                },
                Disabled = !detailsRange,
                Message = detailsRange ? null : Loc.GetString("detail-examine-verb-disabled"),
                Text = Loc.GetString("character-info-inspect-prompt"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"))
            });
        }

        var mind = _mindSystem.GetMind(ent);
        string? desc = null;
        if (TryComp(args.Target, out CharacterDescriptionComponent? characterDesc) &&
            characterDesc.Description.Length > 0)
        {
            desc += characterDesc.Description;
        }

        if (mind != null && TryComp(mind, out CharacterDescriptionComponent? mindInfoComponent) &&
            mindInfoComponent.Description.Length > 0)
        {
            desc += "\n" + mindInfoComponent.Description;
        }

        if (_flavorTextEnabled && desc != null)
        {
            args.Verbs.Add(new ExamineVerb
            {
                Act = () =>
                {
                    var markup = new FormattedMessage();
                    markup.AddMarkupPermissive(desc);
                    _examineSystem.SendExamineTooltip(user, ent, markup, false, false);
                },
                Text = Loc.GetString("detail-examine-verb-text"),
                Category = VerbCategory.Examine,
                Disabled = !detailsRange,
                Message = detailsRange ? null : Loc.GetString("detail-examine-verb-disabled"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"))
            });
        }
    }

    private void OnExamineExploitableInfo(Entity<ExploitableInfoComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var detailsRange = _examineSystem.IsInDetailsRange(args.User, ent);
        var user = args.User;

        if (_exploitableSecretsEnabled
            && ent.Comp.Info != string.Empty
            && (CanAccessExploitableData(ent,user)))
        {
            args.Verbs.Add(new ExamineVerb
            {
                Act = () =>
                {
                    var markup = new FormattedMessage();
                    markup.AddMarkupPermissive(ent.Comp.Info);
                    _examineSystem.SendExamineTooltip(user, ent, markup, false, false);
                },
                Text = Loc.GetString("exploitable-examine-verb-text"),
                Category = VerbCategory.Examine,
                Disabled = !detailsRange,
                Message = detailsRange ? null : Loc.GetString("exploitable-examine-verb-disabled"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"))
            });
        }
    }
    public bool CanAccessExploitableData(EntityUid target, Entity<MindContainerComponent?> requester)
    {
        return target == requester.Owner
               || HasComp<GhostComponent>(requester)
               || (Resolve(requester.Owner,ref requester.Comp, false) && _roleSystem.MindIsAntagonist(requester.Comp.Mind));
    }

    protected virtual void OpenCharacterWindow(EntityUid target, EntityUid requester)
    {
    }

    public bool CanAccessSecretData(EntityUid target, Entity<MindContainerComponent?> requester)
    {
        return target == requester.Owner;
    }
}