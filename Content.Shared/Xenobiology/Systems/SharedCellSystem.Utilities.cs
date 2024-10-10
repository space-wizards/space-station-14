using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology.Systems;

public abstract partial class SharedCellSystem
{
    public string GetCellModifiersString(List<ProtoId<CellModifierPrototype>> modifiers)
    {
        var message = string.Empty;

        foreach (var modifierId in modifiers)
        {
            if (!_prototype.TryIndex(modifierId, out var modifier))
                continue;

            var modifiersMessage = Loc.GetString("cell-sequencer-menu-cell-modifier-message",
                ("name", Loc.GetString(modifier.Name)),
                ("color", modifier.Color.ToHex()));

            message += $"{modifiersMessage}\r\n";
        }

        return message;
    }
}
