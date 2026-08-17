using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WpfDockingSample;

public sealed class DockSplitContainer : Grid
{
    public Orientation Orientation { get; }
    public UIElement First { get; private set; }
    public UIElement Second { get; private set; }

    public DockSplitContainer(Orientation orientation, UIElement first, UIElement second)
    {
        Orientation = orientation;
        First = first;
        Second = second;
        MinWidth = 90;
        MinHeight = 70;
        BuildVisualTree();
    }

    public UIElement GetSibling(UIElement child) =>
        ReferenceEquals(child, First) ? Second : First;

    public void DetachChild(UIElement child)
    {
        Children.Remove(child);
    }

    public void ReplaceChild(UIElement oldChild, UIElement newChild)
    {
        bool isFirst = ReferenceEquals(oldChild, First);
        bool isSecond = ReferenceEquals(oldChild, Second);
        if (!isFirst && !isSecond)
            throw new InvalidOperationException("指定された要素はこの分割領域の子ではありません。");

        int index = isFirst ? 0 : 2;
        Children.Remove(oldChild);
        if (isFirst) First = newChild; else Second = newChild;
        Place(newChild, index);
    }

    private void BuildVisualTree()
    {
        var splitter = new GridSplitter
        {
            Background = new SolidColorBrush(Color.FromRgb(70, 70, 74)),
            ShowsPreview = true,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext
        };

        if (Orientation == System.Windows.Controls.Orientation.Horizontal)
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 70 });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 70 });
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            Place(First, 0);
            Place(splitter, 1);
            Place(Second, 2);
        }
        else
        {
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 55 });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(5) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 55 });
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            Place(First, 0);
            Place(splitter, 1);
            Place(Second, 2);
        }
    }

    private void Place(UIElement element, int index)
    {
        if (Orientation == System.Windows.Controls.Orientation.Horizontal)
            SetColumn(element, index);
        else
            SetRow(element, index);
        Children.Add(element);
    }
}
