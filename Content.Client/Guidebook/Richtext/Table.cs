using System.Diagnostics.CodeAnalysis;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Guidebook.Richtext;

[UsedImplicitly]
public sealed class Table : TableContainer, IDocumentTag
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("guidebook.table");

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        HorizontalExpand = true;
        control = this;

        if (!args.TryGetValue("Columns", out var columns) || !int.TryParse(columns, out var columnsCount))
        {
            _sawmill.Error("Guidebook tag \"Table\" does not specify required property \"Columns.\"");
            control = null;
            return false;
        }

        Columns = columnsCount;

        return true;
    }
}
