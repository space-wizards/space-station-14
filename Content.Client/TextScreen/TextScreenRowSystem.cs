using System.Numerics;
using Content.Shared.TextScreen;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.Memory;

namespace Content.Client.TextScreen;

/// <summary>
///     The TextScreenSystem draws text in the game world using 3x5 sprite states for each character.
/// </summary>
public sealed class TextScreenRowSystem : VisualizerSystem<TextScreenRowComponent>
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TextScreenVisualsComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, TextScreenVisualsComponent component, ComponentInit args)
    {
        for (int row = 0; row < component.Rows; row++)
        {

        }
    }

    private void UpdateRows()
    {

    }
}
