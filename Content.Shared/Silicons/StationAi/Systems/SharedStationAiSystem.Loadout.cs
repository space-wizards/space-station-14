using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Silicons.Laws;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    [Dependency] private readonly SharedSiliconLawSystem _law = default!;

    public static string ExtraLoadoutScreenId = "ai-screen";
    public static string ExtraLoadoutLawsetId = "ai-lawset";
    public static string ExtraLoadoutNameId = "ai-name";

    private void InitializeLoadout()
    {
        SubscribeLocalEvent<StationAiBrainComponent, ApplyLoadoutExtrasEvent>(ApplyExtras);
        SubscribeLocalEvent<StationAiCoreComponent, ApplyLoadoutExtrasEvent>(ApplyCoreExtras);
    }

    private void ApplyExtras(Entity<StationAiBrainComponent> ent, ref ApplyLoadoutExtrasEvent args)
    {
        SetLoadoutExtraLawset(ent, args.Data);
        if (!TryGetCore(ent.Owner, out var core))
            return;

        RaiseLocalEvent(core, ref args);
    }

    private void ApplyCoreExtras(Entity<StationAiCoreComponent> ent, ref ApplyLoadoutExtrasEvent args)
    {
        SetLoadoutExtraVisuals(ent, args.Data);
    }

    public void SetLoadoutExtraVisuals(EntityUid ent, Dictionary<string, string> data)
    {
        if (data.TryGetValue(ExtraLoadoutScreenId, out var screen))
            _appearance.SetData(ent, StationAiCustomVisualState.Key, screen);

        if (!data.TryGetValue(ExtraLoadoutNameId, out var name) || name == string.Empty)
            return;

        if (!_containers.TryGetContainer(ent, StationAiHolderComponent.Container, out var container) ||
            container.Count == 0)
            return;

        var brain = container.ContainedEntities[0];

        _metadata.SetEntityName(ent, name);
        _metadata.SetEntityName(brain, name);
    }

    public virtual void SetLoadoutExtraLawset(EntityUid brain, Dictionary<string, string> data)
    {
    }

    public virtual void SetLoadoutOnTakeover(EntityUid core, EntityUid brain)
    {
    }
}
