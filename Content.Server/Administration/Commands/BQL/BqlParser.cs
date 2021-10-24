using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

// this is all really shit but it works and only runs once a command.
namespace Content.Server.Administration.Commands.BQL
{
    public static class BqlParser
    {
        private enum TokenKind
        {
            With,
            Named,
            ParentedTo,
            Prototyped,
            Tagged,
            Select,
            Do,
            String,
        }

        private readonly struct Token
        {
            public readonly TokenKind Kind;
            public readonly string Text;

            private Token(TokenKind kind, string text)
            {
                Kind = kind;
                Text = text;
            }

            //I didn't want to write a proper parser. --moony
            public static Tuple<string, Token> ExtractOneToken(string inp)
            {
                inp = inp.TrimStart();
                return inp switch
                {
                    _ when inp.StartsWith("with ") => new Tuple<string, Token>(inp[4..], new Token(TokenKind.With, "with")),
                    _ when inp.StartsWith("named ") => new Tuple<string, Token>(inp[5..], new Token(TokenKind.Named, "named")),
                    _ when inp.StartsWith("parented_to ") => new Tuple<string, Token>(inp[11..], new Token(TokenKind.ParentedTo, "parented_to")),
                    _ when inp.StartsWith("prototyped ") => new Tuple<string, Token>(inp[10..], new Token(TokenKind.Prototyped, "prototyped")),
                    _ when inp.StartsWith("tagged ") => new Tuple<string, Token>(inp[6..], new Token(TokenKind.Tagged, "tagged")),
                    _ when inp.StartsWith("select ") => new Tuple<string, Token>(inp[6..], new Token(TokenKind.Select, "select")),
                    _ when inp.StartsWith("do ") => new Tuple<string, Token>(inp[2..], new Token(TokenKind.Do, "do")),
                    _ => ExtractStringToken(inp)
                };
            }

            private static Tuple<string, Token> ExtractStringToken(string inp)
            {
                inp = inp.TrimStart();
                if (inp.StartsWith("\""))
                {
                    var acc = "";
                    var skipNext = false;
                    foreach (var rune in inp[1..])
                    {
                        if (skipNext)
                        {
                            acc += rune;
                            skipNext = false;
                            continue;
                        }

                        switch (rune)
                        {
                            case '\\':
                                skipNext = true;
                                continue;
                            case '"':
                                return new Tuple<string, Token>(inp[(acc.Length+2)..], new Token(TokenKind.String, acc));
                            default:
                                acc += rune;
                                continue;
                        }
                    }

                    throw new Exception("Missing a \" somewhere.");
                }

                if (inp.Contains(" ") == false)
                {
                    return new Tuple<string, Token>("", new Token(TokenKind.String, inp));
                }
                var word = inp[..inp.IndexOf(" ", StringComparison.Ordinal)];
                var rem = inp[inp.IndexOf(" ", StringComparison.Ordinal)..];
                return new Tuple<string, Token>(rem, new Token(TokenKind.String, word));
            }
        }

        // Extracts and evaluates a query, then returns the rest.
        public static Tuple<string, IEnumerable<IEntity>> DoEntityQuery(string query, IEntityManager entityManager)
        {
            var remainingQuery = query;
            var componentFactory = IoCManager.Resolve<IComponentFactory>();
            var entities = entityManager.GetEntities();

            while (true)
            {
                Token t;
                (remainingQuery, t) = Token.ExtractOneToken(remainingQuery);

                switch (t.Kind)
                {
                    case TokenKind.With:
                    {
                        Token nt;
                        (remainingQuery, nt) = Token.ExtractOneToken(remainingQuery);
                        var comp = componentFactory.GetRegistration(nt.Text).Type;
                        entities = entities.Where(e => e.HasComponent(comp));
                        break;
                    }
                    case TokenKind.Named:
                    {
                        Token nt;
                        (remainingQuery, nt) = Token.ExtractOneToken(remainingQuery);
                        var r = new Regex("^" + nt.Text + "$");
                        entities = entities.Where(e => r.IsMatch(e.Name));
                        break;
                    }
                    case TokenKind.Tagged:
                    {
                        Token nt;
                        (remainingQuery, nt) = Token.ExtractOneToken(remainingQuery);
                        var text = nt.Text;
                        entities = entities.Where(e =>
                        {
                            if (e.TryGetComponent<TagComponent>(out var tagComponent))
                            {
                                return tagComponent.Tags.Contains(text);
                            }

                            return false;
                        });
                        break;
                    }
                    case TokenKind.ParentedTo:
                    {
                        Token nt;
                        (remainingQuery, nt) = Token.ExtractOneToken(remainingQuery);
                        var uid = EntityUid.Parse(nt.Text);
                        entities = entities.Where(e => e.Transform.Parent?.Owner.Uid == uid);
                        break;
                    }
                    case TokenKind.Prototyped:
                    {
                        Token nt;
                        (remainingQuery, nt) = Token.ExtractOneToken(remainingQuery);
                        entities = entities.Where(e => e.Prototype?.ID == nt.Text);
                        break;
                    }
                    case TokenKind.Select:
                    {
                        Token nt;
                        (remainingQuery, nt) = Token.ExtractOneToken(remainingQuery);
                        entities = entities.OrderBy(a => Guid.NewGuid()); //Cheeky way of randomizing.
                        if (int.TryParse(nt.Text, out var x))
                        {
                            entities = entities.Take(x);
                        }
                        else if (nt.Text.Last() == '%' && int.TryParse(nt.Text[..^1], out x))
                        {
                            var enumerable = entities.ToArray();
                            var amount = (int)Math.Floor(enumerable.Length * (x * 0.01));
                            entities = enumerable.Take(amount);
                        }
                        else
                        {
                            throw new Exception("The value " + nt.Text + " is not a valid number nor a valid percentage.");
                        }
                        break;
                    }
                    case TokenKind.Do:
                        return new Tuple<string, IEnumerable<IEntity>>(remainingQuery, entities);
                    default:
                        throw new Exception("Unknown token called " + t.Text + ", which was parsed as a "+ t.Kind.ToString());
                }

                if (remainingQuery.TrimStart() == "")
                    return new Tuple<string, IEnumerable<IEntity>>(remainingQuery, entities);
            }
        }
    }
}
