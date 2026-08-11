// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System;
using Content.Client.Paper.UI;
using NUnit.Framework;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Tests.Client.DeadSpace.Paper;

[TestFixture]
[TestOf(typeof(PaperWindow))]
public sealed class PaperWindowFormattingTest
{
    private const string BoldOpen = "[bold]";
    private const string BoldClose = "[/bold]";
    private const string BulletOpen = "[bullet]";
    private const string BulletClose = "[/bullet]";

    [Test]
    public void InlineSelectionWrapsAndRepeatedToggleUnwraps()
    {
        var text = "alpha";
        var lower = 0;
        var upper = text.Length;

        text = PaperWindow.ToggleInlineFormatText(text, BoldOpen, BoldClose, ref lower, ref upper);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("[bold]alpha[/bold]"));
            Assert.That(lower, Is.EqualTo(BoldOpen.Length));
            Assert.That(upper, Is.EqualTo(BoldOpen.Length + "alpha".Length));
            Assert.That(text[lower..upper], Is.EqualTo("alpha"));
        });

        text = PaperWindow.ToggleInlineFormatText(text, BoldOpen, BoldClose, ref lower, ref upper);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("alpha"));
            Assert.That(lower, Is.Zero);
            Assert.That(upper, Is.EqualTo("alpha".Length));
        });
    }

    [Test]
    public void InlineFullWrapperSelectionUnwraps()
    {
        var text = "[bold]alpha[/bold]";
        var lower = 0;
        var upper = text.Length;

        text = PaperWindow.ToggleInlineFormatText(text, BoldOpen, BoldClose, ref lower, ref upper);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("alpha"));
            Assert.That(lower, Is.Zero);
            Assert.That(upper, Is.EqualTo("alpha".Length));
        });
    }

    [Test]
    public void InlineEmptyCaretDoesNotNestTags()
    {
        var text = "ab";
        var lower = 1;
        var upper = 1;

        text = PaperWindow.ToggleInlineFormatText(text, BoldOpen, BoldClose, ref lower, ref upper);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("a[bold][/bold]b"));
            Assert.That(lower, Is.EqualTo(1 + BoldOpen.Length));
            Assert.That(upper, Is.EqualTo(lower));
        });

        text = PaperWindow.ToggleInlineFormatText(text, BoldOpen, BoldClose, ref lower, ref upper);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("ab"));
            Assert.That(lower, Is.EqualTo(1));
            Assert.That(upper, Is.EqualTo(1));
        });
    }

    [Test]
    public void InlineSiblingWrappersAreNotCorrupted()
    {
        const string original = "[bold]a[/bold][bold]b[/bold]";
        var text = original;
        var lower = 0;
        var upper = text.Length;

        Assert.That(
            PaperWindow.IsWrappedBySingleOuterPair(text, BoldOpen, BoldClose),
            Is.False);

        text = PaperWindow.ToggleInlineFormatText(text, BoldOpen, BoldClose, ref lower, ref upper);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo(BoldOpen + original + BoldClose));
            Assert.That(text[lower..upper], Is.EqualTo(original));
        });

        text = PaperWindow.ToggleInlineFormatText(text, BoldOpen, BoldClose, ref lower, ref upper);
        Assert.That(text, Is.EqualTo(original));
    }

    [Test]
    public void InlineFormattingPreservesEmojiSelection()
    {
        const string emoji = "😀";
        var text = $"a{emoji}b";
        var lower = text.IndexOf(emoji, StringComparison.Ordinal);
        var upper = lower + emoji.Length;

        text = PaperWindow.ToggleInlineFormatText(text, BoldOpen, BoldClose, ref lower, ref upper);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo($"a[bold]{emoji}[/bold]b"));
            Assert.That(text[lower..upper], Is.EqualTo(emoji));
            Assert.That(upper - lower, Is.EqualTo(emoji.Length));
        });
    }

    [Test]
    public void BlockFormatPreservesBlankLinesAndUntogglesExactly()
    {
        const string original = "\nalpha\n\nbeta\n";
        const string expected = "\n[bullet]alpha[/bullet]\n\n[bullet]beta[/bullet]\n";

        var formatted = PaperWindow.ToggleBlockFormatText(original, BulletOpen, BulletClose);
        var restored = PaperWindow.ToggleBlockFormatText(formatted, BulletOpen, BulletClose);

        Assert.Multiple(() =>
        {
            Assert.That(formatted, Is.EqualTo(expected));
            Assert.That(restored, Is.EqualTo(original));
        });
    }

    [Test]
    public void BlockFormatDoesNotDoubleWrapMixedLines()
    {
        const string original = "[bullet]alpha[/bullet]\nbeta";
        const string expected = "[bullet]alpha[/bullet]\n[bullet]beta[/bullet]";

        var formatted = PaperWindow.ToggleBlockFormatText(original, BulletOpen, BulletClose);

        Assert.That(formatted, Is.EqualTo(expected));
    }

    [Test]
    public void BlockFormatRespectsExclusiveUpperAtLf()
    {
        const string original = "a\nb";
        var lower = 0;
        var upper = 2;

        PaperWindow.ExpandSelectionToLines(original, ref lower, ref upper);
        var formattedBlock = PaperWindow.ToggleBlockFormatText(
            original[lower..upper],
            BulletOpen,
            BulletClose);
        var result = original[..lower] + formattedBlock + original[upper..];

        Assert.Multiple(() =>
        {
            Assert.That(lower, Is.Zero);
            Assert.That(upper, Is.EqualTo(1));
            Assert.That(result, Is.EqualTo("[bullet]a[/bullet]\nb"));
        });
    }

    [Test]
    public void BlockFormatPreservesCrLf()
    {
        const string original = "alpha\r\n\r\nbeta\r\n";
        const string expected = "[bullet]alpha[/bullet]\r\n\r\n[bullet]beta[/bullet]\r\n";

        var formatted = PaperWindow.ToggleBlockFormatText(original, BulletOpen, BulletClose);
        var restored = PaperWindow.ToggleBlockFormatText(formatted, BulletOpen, BulletClose);

        Assert.Multiple(() =>
        {
            Assert.That(formatted, Is.EqualTo(expected));
            Assert.That(restored, Is.EqualTo(original));
        });
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void HeadingReplacesAndRemovesExistingLevels(int oldLevel)
    {
        var original = $"[head={oldLevel}]alpha[/head]";

        Assert.Multiple(() =>
        {
            Assert.That(
                PaperWindow.ApplyHeadingToLine(original, 2),
                Is.EqualTo("[head=2]alpha[/head]"));
            Assert.That(
                PaperWindow.ApplyHeadingToLine(original, null),
                Is.EqualTo("alpha"));
        });
    }

    [Test]
    public void HeadingPreservesBlankLinesAndCrLf()
    {
        const string original = "alpha\r\n\r\nbeta\n";
        const string expected = "[head=3]alpha[/head]\r\n\r\n[head=3]beta[/head]\n";

        var formatted = PaperWindow.ApplyHeadingToLines(original, 3);
        var restored = PaperWindow.ApplyHeadingToLines(formatted, null);

        Assert.Multiple(() =>
        {
            Assert.That(formatted, Is.EqualTo(expected));
            Assert.That(restored, Is.EqualTo(original));
        });
    }

    [Test]
    public void HeadingSiblingWrappersAreNotCorrupted()
    {
        const string original = "[head=1]a[/head][head=2]b[/head]";

        Assert.Multiple(() =>
        {
            Assert.That(PaperWindow.ApplyHeadingToLine(original, null), Is.EqualTo(original));
            Assert.That(
                PaperWindow.ApplyHeadingToLine(original, 3),
                Is.EqualTo($"[head=3]{original}[/head]"));
        });
    }

    [Test]
    public void ColorWrapsPlainSelection()
    {
        var text = "alpha";
        var lower = 0;
        var upper = text.Length;

        text = PaperWindow.ApplyColorToTextAtRange(
            text,
            Color.FromHex("#12AB34"),
            ref lower,
            ref upper);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("[color=#12AB34]alpha[/color]"));
            Assert.That(text[lower..upper], Is.EqualTo("alpha"));
        });
    }

    [Test]
    public void ColorReplacesSurroundingTag()
    {
        const string oldOpen = "[color=#FFFFFF]";
        var text = oldOpen + "alpha[/color]";
        var lower = oldOpen.Length;
        var upper = lower + "alpha".Length;

        text = PaperWindow.ApplyColorToTextAtRange(
            text,
            Color.FromHex("#12AB34"),
            ref lower,
            ref upper);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("[color=#12AB34]alpha[/color]"));
            Assert.That(text[lower..upper], Is.EqualTo("alpha"));
        });
    }

    [Test]
    public void ColorAtEmptyCaretDoesNotNestTags()
    {
        var text = "ab";
        var lower = 1;
        var upper = 1;

        text = PaperWindow.ApplyColorToTextAtRange(
            text,
            Color.FromHex("#12AB34"),
            ref lower,
            ref upper);
        text = PaperWindow.ApplyColorToTextAtRange(
            text,
            Color.FromHex("#ABCDEF"),
            ref lower,
            ref upper);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("a[color=#ABCDEF][/color]b"));
            Assert.That(lower, Is.EqualTo("a[color=#ABCDEF]".Length));
            Assert.That(upper, Is.EqualTo(lower));
        });
    }

    [TestCase("a\nb", 0, 2, 0, 1)]
    [TestCase("aa\nbb", 1, 2, 0, 2)]
    [TestCase("a\nb", 2, 2, 2, 3)]
    [TestCase("a\r\nb", 0, 3, 0, 1)]
    [TestCase("aa\r\nbb", 1, 2, 0, 2)]
    [TestCase("a\r\nb", 3, 3, 3, 4)]
    public void ExpandSelectionHandlesLfAndCrLf(
        string text,
        int lower,
        int upper,
        int expectedLower,
        int expectedUpper)
    {
        PaperWindow.ExpandSelectionToLines(text, ref lower, ref upper);

        Assert.Multiple(() =>
        {
            Assert.That(lower, Is.EqualTo(expectedLower));
            Assert.That(upper, Is.EqualTo(expectedUpper));
        });
    }

    [TestCase("a\nb", 1, TextEdit.LineBreakBias.Top)]
    [TestCase("a\r\nb", 2, TextEdit.LineBreakBias.Top)]
    [TestCase("a\nb", 3, TextEdit.LineBreakBias.Top)]
    [TestCase("a\nb", 0, TextEdit.LineBreakBias.Bottom)]
    [TestCase("a\r\nb", 1, TextEdit.LineBreakBias.Bottom)]
    public void LineBreakBiasMatchesExplicitNewlines(
        string text,
        int index,
        TextEdit.LineBreakBias expected)
    {
        Assert.That(PaperWindow.GetLineBreakBias(text, index), Is.EqualTo(expected));
    }
}
