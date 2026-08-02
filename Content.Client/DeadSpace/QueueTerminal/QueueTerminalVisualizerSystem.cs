// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.QueueTerminal;
using Robust.Client.GameObjects;

namespace Content.Client.DeadSpace.QueueTerminal;

/// <summary>
/// Отображает текущий вызываемый номер терминала, используя три слоя спрайтов цифр (сотни/десятки/единицы), с состояниями RSI для каждой цифры, названными
/// "digit_0".."digit_9" (плюс "digit_blank" для очищенного отображения)
/// </summary>
public sealed class QueueTerminalVisualizerSystem : VisualizerSystem<QueueTerminalComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, QueueTerminalComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        AppearanceSystem.TryGetData<int>(uid, QueueDisplayVisuals.Number, out var number, args.Component);
        QueueDigitVisuals.SetDigits(SpriteSystem, (uid, args.Sprite), number);
    }
}
public sealed class QueueTicketVisualizerSystem : VisualizerSystem<QueueTicketComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, QueueTicketComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        AppearanceSystem.TryGetData<int>(uid, QueueDisplayVisuals.Number, out var number, args.Component);
        QueueDigitVisuals.SetDigits(SpriteSystem, (uid, args.Sprite), number);
    }
}
public static class QueueDigitVisuals
{
    public static void SetDigits(SpriteSystem spriteSystem, Entity<SpriteComponent?> sprite, int number)
    {
        if (number <= 0)
        {
            SetBlank(spriteSystem, sprite, QueueDigitLayers.Hundreds);
            SetBlank(spriteSystem, sprite, QueueDigitLayers.Tens);
            SetBlank(spriteSystem, sprite, QueueDigitLayers.Ones);
            return;
        }

        var clamped = Math.Clamp(number, 0, QueueTerminalComponent.MaxNumber);
        var hundreds = clamped / 100;
        var tens = clamped / 10 % 10;
        var ones = clamped % 10;

        SetDigit(spriteSystem, sprite, QueueDigitLayers.Hundreds, hundreds);
        SetDigit(spriteSystem, sprite, QueueDigitLayers.Tens, tens);
        SetDigit(spriteSystem, sprite, QueueDigitLayers.Ones, ones);
    }

    private static void SetDigit(SpriteSystem spriteSystem, Entity<SpriteComponent?> sprite, QueueDigitLayers layer, int digit)
    {
        spriteSystem.LayerSetVisible(sprite, layer, true);
        spriteSystem.LayerSetRsiState(sprite, layer, $"digit_{digit}");
    }

    private static void SetBlank(SpriteSystem spriteSystem, Entity<SpriteComponent?> sprite, QueueDigitLayers layer)
    {
        spriteSystem.LayerSetRsiState(sprite, layer, "digit_blank");
    }
}
