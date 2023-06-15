using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.UserInterface.Controls;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.UnitTesting;

namespace Content.Tests.Client.UserInterface.Controls;

[TestFixture]
[TestOf(typeof(ListContainer))]
public sealed class ListContainerTest : RobustUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;

    private record TestListData(int Id) : ListData;

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<IUserInterfaceManager>().InitializeTesting();
    }

    [Test]
    public void TestLayoutBasic()
    {
        var root = new Control { MinSize = (50, 60) };
        var listContainer = new ListContainer { SeparationOverride = 3 };
        root.AddChild(listContainer);
        listContainer.GenerateItem += (_, button) => {
            button.AddChild(new Control { MinSize = (10, 10) });
        };

        var list = new List<TestListData> {new(0), new(1)};
        listContainer.PopulateList(list);
        root.Arrange(new UIBox2(0, 0, 50, 60));

        Assert.That(listContainer.ChildCount, Is.EqualTo(3));
        var children = listContainer.Children.ToList();
        Assert.That(children[0].Height, Is.EqualTo(10));
        Assert.That(children[1].Height, Is.EqualTo(10));

        Assert.That(children[0].Position.Y, Is.EqualTo(0));
        Assert.That(children[1].Position.Y, Is.EqualTo(13)); // Item height + separation
    }

    [Test]
    public void TestCreatePopulateAndEmpty()
    {
        const int x = 50;
        const int y = 10;
        var root = new Control { MinSize = (x, y) };
        var listContainer = new ListContainer { SeparationOverride = 3 };
        root.AddChild(listContainer);
        listContainer.GenerateItem += (_, button) => {
            button.AddChild(new Control { MinSize = (10, 10) });
        };

        var list = new List<TestListData>();
        listContainer.PopulateList(list);
        root.Arrange(new UIBox2(0, 0, x, y));

        list.Add(new(0));
        list.Add(new (1));
        listContainer.PopulateList(list);
        root.Arrange(new UIBox2(0, 0, x, y));

        list.Clear();
        listContainer.PopulateList(list);
        root.Arrange(new UIBox2(0, 0, x, y));
    }

    [Test]
    public void TestOnlyVisibleItemsAreAdded()
    {
        /*
         * 6 items * 10 height + 5 separation * 3 height = 75
         * One items should be off the render
         * 0 13 26 39 52 65 | 75 height
         */
        var root = new Control { MinSize = (50, 60) };
        var listContainer = new ListContainer { SeparationOverride = 3 };
        root.AddChild(listContainer);
        listContainer.GenerateItem += (_, button) => {
            button.AddChild(new Control { MinSize = (10, 10) });
        };

        var list = new List<TestListData> {new(0), new(1), new(2), new(3), new(4), new(5)};
        listContainer.PopulateList(list);
        root.Arrange(new UIBox2(0, 0, 50, 60));

        // 6 ControlData
        Assert.That(listContainer.Data.Count, Is.EqualTo(6));
        // 5 Buttons, 1 Scrollbar
        Assert.That(listContainer.ChildCount, Is.EqualTo(6));

        var children = listContainer.Children.ToList();
        foreach (var child in children)
        {
            if (child is not ListContainerButton)
                continue;
            Assert.That(child.Height, Is.EqualTo(10));
        }

        Assert.That(children[0].Position.Y, Is.EqualTo(0));
        Assert.That(children[1].Position.Y, Is.EqualTo(13));
        Assert.That(children[2].Position.Y, Is.EqualTo(26));
        Assert.That(children[3].Position.Y, Is.EqualTo(39));
        Assert.That(children[4].Position.Y, Is.EqualTo(52));
    }

    [Test]
    public void TestNextItemIsVisibleWhenScrolled()
    {
        /*
         * 6 items * 10 height + 5 separation * 3 height = 75
         * One items should be off the render
         * 0 13 26 39 52 65 | 75 height
         */
        var root = new Control { MinSize = (50, 60) };
        var listContainer = new ListContainer { SeparationOverride = 3 };
        root.AddChild(listContainer);
        listContainer.GenerateItem += (_, button) => {
            button.AddChild(new Control { MinSize = (10, 10) });
        };

        var list = new List<TestListData> {new(0), new(1), new(2), new(3), new(4), new(5)};
        listContainer.PopulateList(list);
        root.Arrange(new UIBox2(0, 0, 50, 60));

        var scrollbar = (ScrollBar) listContainer.Children.Last(c => c is ScrollBar);

        // Test that 6th button is not visible when scrolled
        scrollbar.Value = 5;
        listContainer.Arrange(root.SizeBox);
        var children = listContainer.Children.ToList();
        // 5 Buttons, 1 Scrollbar
        Assert.That(listContainer.ChildCount, Is.EqualTo(6));
        Assert.That(children[0].Position.Y, Is.EqualTo(-5));
        Assert.That(children[1].Position.Y, Is.EqualTo(8));
        Assert.That(children[2].Position.Y, Is.EqualTo(21));
        Assert.That(children[3].Position.Y, Is.EqualTo(34));
        Assert.That(children[4].Position.Y, Is.EqualTo(47));

        // Test that 6th button is visible when scrolled
        scrollbar.Value = 6;
        listContainer.Arrange(root.SizeBox);
        children = listContainer.Children.ToList();
        // 6 Buttons, 1 Scrollbar
        Assert.That(listContainer.ChildCount, Is.EqualTo(7));
        Assert.That(Math.Abs(scrollbar.Value - 6), Is.LessThan(0.01f));
        Assert.That(children[0].Position.Y, Is.EqualTo(-6));
        Assert.That(children[1].Position.Y, Is.EqualTo(7));
        Assert.That(children[2].Position.Y, Is.EqualTo(20));
        Assert.That(children[3].Position.Y, Is.EqualTo(33));
        Assert.That(children[4].Position.Y, Is.EqualTo(46));
        Assert.That(children[5].Position.Y, Is.EqualTo(59));
    }

    [Test]
    public void TestPreviousItemIsVisibleWhenScrolled()
    {
        /*
         * 6 items * 10 height + 5 separation * 3 height = 75
         * One items should be off the render
         * 0 13 26 39 52 65 | 75 height
         */
        var root = new Control { MinSize = (50, 60) };
        var listContainer = new ListContainer { SeparationOverride = 3 };
        root.AddChild(listContainer);
        listContainer.GenerateItem += (_, button) => {
            button.AddChild(new Control { MinSize = (10, 10) });
        };

        var list = new List<TestListData> {new(0), new(1), new(2), new(3), new(4), new(5)};
        listContainer.PopulateList(list);
        root.Arrange(new UIBox2(0, 0, 50, 60));

        var scrollbar = (ScrollBar) listContainer.Children.Last(c => c is ScrollBar);

        var scrollValue = 9;

        // Test that 6th button is not visible when scrolled
        scrollbar.Value = scrollValue;
        listContainer.Arrange(root.SizeBox);
        var children = listContainer.Children.ToList();
        // 6 Buttons, 1 Scrollbar
        Assert.That(listContainer.ChildCount, Is.EqualTo(7));
        Assert.That(children[0].Position.Y, Is.EqualTo(-9));
        Assert.That(children[1].Position.Y, Is.EqualTo(4));
        Assert.That(children[2].Position.Y, Is.EqualTo(17));
        Assert.That(children[3].Position.Y, Is.EqualTo(30));
        Assert.That(children[4].Position.Y, Is.EqualTo(43));
        Assert.That(children[5].Position.Y, Is.EqualTo(56));

        // Test that 6th button is visible when scrolled
        scrollValue = 10;
        scrollbar.Value = scrollValue;
        listContainer.Arrange(root.SizeBox);
        children = listContainer.Children.ToList();
        // 5 Buttons, 1 Scrollbar
        Assert.That(listContainer.ChildCount, Is.EqualTo(6));
        Assert.That(Math.Abs(scrollbar.Value - scrollValue), Is.LessThan(0.01f));
        Assert.That(children[0].Position.Y, Is.EqualTo(3));
        Assert.That(children[1].Position.Y, Is.EqualTo(16));
        Assert.That(children[2].Position.Y, Is.EqualTo(29));
        Assert.That(children[3].Position.Y, Is.EqualTo(42));
        Assert.That(children[4].Position.Y, Is.EqualTo(55));
    }

    /// <summary>
    /// Test that the ListContainer doesn't push other Controls
    /// </summary>
    [Test]
    public void TestDoesNotExpandWhenChildrenAreAdded()
    {
        var height = 60;
        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinSize = (50, height)
        };
        var listContainer = new ListContainer
        {
            SeparationOverride = 0,
            GenerateItem = (_, button) => { button.AddChild(new Control {MinSize = (10, 10)}); }
        };
        root.AddChild(listContainer);
        var button = new ContainerButton
        {
            MinSize = (10, 10)
        };
        root.AddChild(button);

        var list = new List<TestListData> {new(0), new(1), new(2), new(3), new(4), new(5)};
        listContainer.PopulateList(list);
        root.Arrange(new UIBox2(0, 0, 50, height));

        var children = listContainer.Children.ToList();
        // 6 Buttons, 1 Scrollbar
        Assert.That(listContainer.ChildCount, Is.EqualTo(6));
        Assert.That(children[0].Position.Y, Is.EqualTo(0));
        Assert.That(children[1].Position.Y, Is.EqualTo(10));
        Assert.That(children[2].Position.Y, Is.EqualTo(20));
        Assert.That(children[3].Position.Y, Is.EqualTo(30));
        Assert.That(children[4].Position.Y, Is.EqualTo(40));
        Assert.That(button.Position.Y, Is.EqualTo(50));
    }

    [Test]
    public void TestSelectedItemStillSelectedWhenScrolling()
    {
        var height = 10;
        var root = new Control { MinSize = (50, height) };
        var listContainer = new ListContainer { SeparationOverride = 0, Toggle = true };
        root.AddChild(listContainer);
        listContainer.GenerateItem += (_, button) => {
            button.AddChild(new Control { MinSize = (10, 10) });
        };

        var list = new List<TestListData> {new(0), new(1), new(2), new(3), new(4), new(5)};
        listContainer.PopulateList(list);
        root.Arrange(new UIBox2(0, 0, 50, height));

        var scrollbar = (ScrollBar) listContainer.Children.Last(c => c is ScrollBar);

        var children = listContainer.Children.ToList();
        if (children[0] is not ListContainerButton oldButton)
            throw new Exception("First child of ListContainer is not a button");

        listContainer.Select(oldButton.Data);

        // Test that the button is selected even when scrolled away and scrolled back.
        scrollbar.Value = 11;
        listContainer.Arrange(root.SizeBox);
        Assert.That(oldButton.Disposed);
        scrollbar.Value = 0;
        listContainer.Arrange(root.SizeBox);
        children = listContainer.Children.ToList();
        if (children[0] is not ListContainerButton newButton)
            throw new Exception("First child of ListContainer is not a button");
        Assert.That(newButton.Pressed);
        Assert.That(newButton.Disposed == false);
    }
}
