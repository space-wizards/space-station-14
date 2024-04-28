using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook.RichText;

public sealed class ProtodataTag : IMarkupTag
{
    //[Dependency] private readonly GuidebookDataSystem _guidebookData = default!;

    public string Name => "protodata";

    public string TextBefore(MarkupNode node)
    {
        if (!node.Value.TryGetString(out var id))
            return "";

        var guidebookData = IoCManager.Resolve<IEntityManager>().System<GuidebookDataSystem>();

        if (!guidebookData.TryGetValue(id, out var value))
            return "INVALID";

        return value;
    }
}
