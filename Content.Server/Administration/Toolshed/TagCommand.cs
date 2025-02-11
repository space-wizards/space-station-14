using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class TagCommand : ToolshedCommand
{
    private TagSystem? _tag;

    [CommandImplementation("list")]
    public IEnumerable<ProtoId<TagPrototype>> List([PipedArgument] IEnumerable<EntityUid> ent)
    {
        return ent.SelectMany(x =>
        {
            if (TryComp<TagComponent>(x, out var tags))
                // Note: Cast is required for C# to figure out the type signature.
                return (IEnumerable<ProtoId<TagPrototype>>)tags.Tags;
            return Array.Empty<ProtoId<TagPrototype>>();
        });
    }

    [CommandImplementation("with")]
    public IEnumerable<EntityUid> With(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> entities,
        [CommandArgument] ValueRef<string, Prototype<TagPrototype>> tag)
    {
        _tag ??= GetSys<TagSystem>();
        return entities.Where(e => _tag.HasTag(e, tag.Evaluate(ctx)!));
    }

    [CommandImplementation("add")]
    public EntityUid Add([PipedArgument] EntityUid input, ProtoId<TagPrototype> tag)
    {
        _tag ??= GetSys<TagSystem>();
        _tag.AddTag(input, tag);
        return input;
    }

    [CommandImplementation("add")]
    public IEnumerable<EntityUid> Add([PipedArgument] IEnumerable<EntityUid> input, ProtoId<TagPrototype> tag)
        => input.Select(x => Add(x, tag));

    [CommandImplementation("rm")]
    public EntityUid Rm([PipedArgument] EntityUid input, ProtoId<TagPrototype> tag)
    {
        _tag ??= GetSys<TagSystem>();
        _tag.RemoveTag(input, tag);
        return input;
    }

    [CommandImplementation("rm")]
    public IEnumerable<EntityUid> Rm([PipedArgument] IEnumerable<EntityUid> input, ProtoId<TagPrototype> tag)
        => input.Select(x => Rm(x, tag));

    [CommandImplementation("addmany")]
    public EntityUid AddMany([PipedArgument] EntityUid input, IEnumerable<ProtoId<TagPrototype>> tags)
    {
        _tag ??= GetSys<TagSystem>();
        _tag.AddTags(input, tags);
        return input;
    }

    [CommandImplementation("addmany")]
    public IEnumerable<EntityUid> AddMany([PipedArgument] IEnumerable<EntityUid> input, IEnumerable<ProtoId<TagPrototype>> tags)
        => input.Select(x => AddMany(x, tags.ToArray()));

    [CommandImplementation("rmmany")]
    public EntityUid RmMany([PipedArgument] EntityUid input, IEnumerable<ProtoId<TagPrototype>> tags)
    {
        _tag ??= GetSys<TagSystem>();
        _tag.RemoveTags(input, tags);
        return input;
    }

    [CommandImplementation("rmmany")]
    public IEnumerable<EntityUid> RmMany([PipedArgument] IEnumerable<EntityUid> input, IEnumerable<ProtoId<TagPrototype>> tags)
        => input.Select(x => RmMany(x, tags.ToArray()));
}
