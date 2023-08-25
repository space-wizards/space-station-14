using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using Robust.Shared.Prototypes;

namespace Content.Server.Corvax.Wagging;

/// <summary>
/// Adds action to toggle wagging animation for tail markings that support that
/// </summary>
public sealed class WaggingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("wag");

        SubscribeLocalEvent<WaggingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WaggingComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<WaggingComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnStartup(EntityUid uid, WaggingComponent component, ComponentStartup args)
    {
        _action.AddAction(uid, component.ToggleAction, null);
    }

    private void OnToggleAction(EntityUid uid, WaggingComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        ToggleWagging(uid, wagging: component);

        args.Handled = true;
    }

    private void OnMobStateChanged(EntityUid uid, WaggingComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (component.Wagging)
            ToggleWagging(uid, wagging: component);
    }

    public void ToggleWagging(EntityUid uid, WaggingComponent? wagging = null, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref wagging, ref humanoid))
            return;

        if (humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings))
        {
            wagging.Wagging = !wagging.Wagging;
            for (var idx = 0; idx < markings.Count; idx++) // Animate all possible tails
            {
                var currentMarkingId = markings[idx].MarkingId;
                var newMarkingId = wagging.Wagging ? $"{currentMarkingId}Animated" : currentMarkingId.Replace("Animated", ""); // Ok while tails is marking
                if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
                {
                    _sawmill.Warning($"{ToPrettyString(uid)} tried toggle wagging but {newMarkingId} marking not exist");
                    continue;
                }

                _humanoidAppearance.SetMarkingId(uid, MarkingCategories.Tail, idx, newMarkingId,
                    humanoid: humanoid);
            }

            var emoteText = Loc.GetString(wagging.Wagging ? "wagging-emote-start" : "wagging-emote-stop");
            _chat.TrySendInGameICMessage(uid, emoteText, InGameICChatType.Emote, ChatTransmitRange.Normal); // Ok while emotes dont have radial menu
        }
    }
}
