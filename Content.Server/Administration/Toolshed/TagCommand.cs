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
    public EntityUid Add(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] EntityUid input,
            [CommandArgument] ValueRef<string, Prototype<TagPrototype>> @ref
        )
    {
        _tag ??= GetSys<TagSystem>();
        _tag.AddTag(input, @ref.Evaluate(ctx)!);
        return input;
    }

    [CommandImplementation("add")]
    public IEnumerable<EntityUid> Add(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] ValueRef<string, Prototype<TagPrototype>> @ref
        )
        => input.Select(x => Add(ctx, x, @ref));

    [CommandImplementation("rm")]
    public EntityUid Rm(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ValueRef<string, Prototype<TagPrototype>> @ref
    )
    {
        _tag ??= GetSys<TagSystem>();
        _tag.RemoveTag(input, @ref.Evaluate(ctx)!);
        return input;
    }

    [CommandImplementation("rm")]
    public IEnumerable<EntityUid> Rm(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] ValueRef<string, Prototype<TagPrototype>> @ref
        )
        => input.Select(x => Rm(ctx, x, @ref));

    [CommandImplementation("addmany")]
    public EntityUid AddMany(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ValueRef<IEnumerable<string>, IEnumerable<string>> @ref
    )
    {
        _tag ??= GetSys<TagSystem>();
        _tag.AddTags(input, (IEnumerable<ProtoId<TagPrototype>>)@ref.Evaluate(ctx)!);
        return input;
    }

    [CommandImplementation("addmany")]
    public IEnumerable<EntityUid> AddMany(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] ValueRef<IEnumerable<string>, IEnumerable<string>> @ref
        )
        => input.Select(x => AddMany(ctx, x, @ref));

    [CommandImplementation("rmmany")]
    public EntityUid RmMany(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ValueRef<IEnumerable<string>, IEnumerable<string>> @ref
    )
    {
        _tag ??= GetSys<TagSystem>();
        _tag.RemoveTags(input, (IEnumerable<ProtoId<TagPrototype>>)@ref.Evaluate(ctx)!);
        return input;
    }

    [CommandImplementation("rmmany")]
    public IEnumerable<EntityUid> RmMany(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] ValueRef<IEnumerable<string>, IEnumerable<string>> @ref
        )
        => input.Select(x => RmMany(ctx, x, @ref));
}
