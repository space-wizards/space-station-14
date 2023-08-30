#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.IntegrationTests.Tests.Guidebook;

/// <summary>
///     This test checks that an example document string properly gets parsed by the <see cref="DocumentParsingManager"/>.
/// </summary>
[TestFixture]
[TestOf(typeof(DocumentParsingManager))]
public sealed class DocumentParsingTest
{

    public string TestDocument = @"multiple
   lines
 separated by
only single newlines
make a single rich text control

unless there is a double newline. Also
whitespace before newlines are ignored.

<TestControl/>

<  TestControl  />

<TestControl>
  some text with a nested control
  <TestControl/>
</TestControl>

<TestControl key1=""value1"" key2=""value2 with spaces"" key3=""value3 with a
  newline""/>

<TestControl >
  <TestControl  k=""<\>\\>=\""=<-_?*3.0//"">
  </TestControl>
</TestControl>";

    [Test]
    public async Task ParseTestDocument()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;
        await client.WaitIdleAsync();
        var parser = client.ResolveDependency<DocumentParsingManager>();

        Control ctrl = default!;
        await client.WaitPost(() =>
        {
            ctrl = new Control();
            Assert.That(parser.TryAddMarkup(ctrl, TestDocument));
        });

        Assert.That(ctrl.ChildCount, Is.EqualTo(7));

        var richText1 = ctrl.GetChild(0) as RichTextLabel;
        var richText2 = ctrl.GetChild(1) as RichTextLabel;

        Assert.Multiple(() =>
        {
            Assert.That(richText1, Is.Not.Null);
            Assert.That(richText2, Is.Not.Null);
        });

        // uhh.. WTF. rich text has no means of getting the contents!?!?
        // TODO assert text content is correct after fixing that bullshit.
        // Assert.That(richText1?.Text, Is.EqualTo("multiple lines separated by only single newlines make a single rich text control"));
        // Assert.That(richText2?.Text, Is.EqualTo("unless there is a double newline. Also whitespace before newlines are ignored."));

        var test1 = ctrl.GetChild(2) as TestControl;
        var test2 = ctrl.GetChild(3) as TestControl;
        var test3 = ctrl.GetChild(4) as TestControl;
        var test4 = ctrl.GetChild(5) as TestControl;
        var test5 = ctrl.GetChild(6) as TestControl;

        Assert.Multiple(() =>
        {
            Assert.That(test1, Is.Not.Null);
            Assert.That(test2, Is.Not.Null);
            Assert.That(test3, Is.Not.Null);
            Assert.That(test4, Is.Not.Null);
            Assert.That(test5, Is.Not.Null);
        });

        Assert.Multiple(() =>
        {
            Assert.That(test1!.ChildCount, Is.EqualTo(0));
            Assert.That(test2!.ChildCount, Is.EqualTo(0));
            Assert.That(test3!.ChildCount, Is.EqualTo(2));
            Assert.That(test4!.ChildCount, Is.EqualTo(0));
            Assert.That(test5!.ChildCount, Is.EqualTo(1));
        });

        var subText = test3!.GetChild(0) as RichTextLabel;
        var subTest = test3.GetChild(1) as TestControl;

#pragma warning disable NUnit2045
        Assert.That(subText, Is.Not.Null);
        //Assert.That(subText?.Text, Is.EqualTo("some text with a nested control"));
        Assert.That(subTest, Is.Not.Null);
        Assert.That(subTest?.ChildCount, Is.EqualTo(0));
#pragma warning restore NUnit2045

        var subTest2 = test5!.GetChild(0) as TestControl;
        Assert.That(subTest2, Is.Not.Null);
        Assert.That(subTest2!.ChildCount, Is.EqualTo(0));

        Assert.Multiple(() =>
        {
            Assert.That(test1!.Params, Has.Count.EqualTo(0));
            Assert.That(test2!.Params, Has.Count.EqualTo(0));
            Assert.That(test3.Params, Has.Count.EqualTo(0));
            Assert.That(test4!.Params, Has.Count.EqualTo(3));
            Assert.That(test5.Params, Has.Count.EqualTo(0));
            Assert.That(subTest2.Params, Has.Count.EqualTo(1));
        });

        test4!.Params.TryGetValue("key1", out var val);
        Assert.That(val, Is.EqualTo("value1"));

        test4.Params.TryGetValue("key2", out val);
        Assert.That(val, Is.EqualTo("value2 with spaces"));

        test4.Params.TryGetValue("key3", out val);
        Assert.That(val, Is.EqualTo(@"value3 with a
  newline"));

        subTest2.Params.TryGetValue("k", out val);
        Assert.That(val, Is.EqualTo(@"<>\>=""=<-_?*3.0//"));

        await pair.CleanReturnAsync();
    }

    public sealed class TestControl : Control, IDocumentTag
    {
        public Dictionary<string, string> Params = default!;

        public bool TryParseTag(Dictionary<string, string> param, [NotNullWhen(true)] out Control control)
        {
            Params = param;
            control = this;
            return true;
        }
    }
}
