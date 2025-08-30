using Content.Server.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.PAI;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Server.PAI;

public sealed partial class PAICustomizationSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;

    private int _maxNameLength;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PAICustomizationComponent, ComponentInit>(Customization);
        SubscribeLocalEvent<PAIComponent, PAIEmotionMessage>(OnEmotionMessage);
        SubscribeLocalEvent<PAIComponent, PAIGlassesMessage>(OnGlassesMessage);
        SubscribeLocalEvent<PAIComponent, PAISetNameMessage>(OnSetNameMessage);
        SubscribeLocalEvent<PAIComponent, PAIResetNameMessage>(OnResetNameMessage);

        Subs.CVar(_cfgManager, CCVars.MaxNameLength, value => _maxNameLength = value, true);
    }

    private void Customization(Entity<PAICustomizationComponent> ent, ref ComponentInit args)
    {
        _appearance.SetData(ent.Owner, PAIEmotionVisuals.Emotion, ent.Comp.CurrentEmotion);
        _appearance.SetData(ent.Owner, PAIGlassesVisuals.Glasses, ent.Comp.CurrentGlasses);
    }

    private void OnEmotionMessage(Entity<PAIComponent> ent, ref PAIEmotionMessage args)
    {
        if (!TryComp<PAICustomizationComponent>(ent.Owner, out var customizationComp))
            return;

        if (customizationComp.CurrentEmotion == args.Emotion)
            return;

        customizationComp.CurrentEmotion = args.Emotion;
        Dirty(ent.Owner, customizationComp);

        _appearance.SetData(ent.Owner, PAIEmotionVisuals.Emotion, args.Emotion);

        _ui.ServerSendUiMessage(ent.Owner, PAICustomizationUiKey.Key, new PAIEmotionStateMessage(args.Emotion));
    }

    private void OnGlassesMessage(Entity<PAIComponent> ent, ref PAIGlassesMessage args)
    {
        if (!TryComp<PAICustomizationComponent>(ent.Owner, out var customizationComp))
            return;

        if (customizationComp.CurrentGlasses == args.Glasses)
            return;

        customizationComp.CurrentGlasses = args.Glasses;
        Dirty(ent.Owner, customizationComp);

        _appearance.SetData(ent.Owner, PAIGlassesVisuals.Glasses, args.Glasses);

        _ui.ServerSendUiMessage(ent.Owner, PAICustomizationUiKey.Key, new PAIGlassesStateMessage(args.Glasses));
    }

    private void OnSetNameMessage(Entity<PAIComponent> ent, ref PAISetNameMessage args)
    {
        if (args.Name.Length > _maxNameLength ||
            args.Name.Length == 0 ||
            string.IsNullOrWhiteSpace(args.Name) ||
            string.IsNullOrEmpty(args.Name))
        {
            return;
        }

        var name = args.Name.Trim();
        var metaData = MetaData(ent.Owner);

        var fullName = $"{name} (pAI)";

        if (metaData.EntityName.Equals(fullName, StringComparison.InvariantCulture))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set pAI \"{ToPrettyString(ent.Owner)}\"'s name to: {name}");

        _metaData.SetEntityName(ent.Owner, fullName, metaData);

        _ui.ServerSendUiMessage(ent.Owner, PAICustomizationUiKey.Key, new PAINameStateMessage(name));
    }

    private void OnResetNameMessage(Entity<PAIComponent> ent, ref PAIResetNameMessage args)
    {
        if (ent.Comp.LastUser == null)
            return;

        var metaData = MetaData(ent.Owner);
        var defaultName = Loc.GetString("pai-system-pai-name", ("owner", ent.Comp.LastUser));

        if (metaData.EntityName.Equals(defaultName, StringComparison.InvariantCulture))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} reset pAI \"{ToPrettyString(ent.Owner)}\"'s name to default");

        _metaData.SetEntityName(ent.Owner, defaultName, metaData);

        var baseName = "";
        if (defaultName.EndsWith("'s pAI"))
        {
            baseName = defaultName.Substring(0, defaultName.Length - "'s pAI".Length);
        }
        else
        {
            baseName = defaultName;
        }

        _ui.ServerSendUiMessage(ent.Owner, PAICustomizationUiKey.Key, new PAINameStateMessage(baseName));
    }
}
