using Content.Shared.Examine;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Localization;

namespace Content.Shared._Starlight.Silicons.Borgs;

public sealed class LawboardSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SiliconLawProviderComponent, ExaminedEvent>(OnExamined);
    }

private void OnExamined(EntityUid uid, SiliconLawProviderComponent component, ExaminedEvent args)
{
    if (!args.IsInDetailsRange)
        return;

    if (!_prototype.TryIndex<SiliconLawsetPrototype>(component.Laws, out var lawsetProto))
        return;

    var lawsetName = _loc.GetString($"board-{lawsetProto.ID.ToLowerInvariant()}-name");
    var description = $"[color=cyan]An electronics board containing the [color=yellow]{lawsetName}[/color] lawset.[/color]\n[color=orange]Uploaded Laws:[/color]";
    int lawNum = 1;
    foreach (var lawId in lawsetProto.Laws)
    {
        if (_prototype.TryIndex<SiliconLawPrototype>(lawId, out var lawProto))
        {
            var lawText = lawProto.LawString != null
                ? _loc.GetString(lawProto.LawString)
                : lawProto.ID;
            description += $"\n[color=lime]Law {lawNum}:[/color] [color=white]{lawText}[/color]";
            lawNum++;
        }
    }

    args.PushMarkup(description);
}
}