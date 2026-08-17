using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WpfDockingSample;

/// <summary>2つのドッキング要素とサイズ変更用GridSplitterを保持します。</summary>
public sealed class DockSplitContainer : Grid
{
    public Orientation Orientation { get; }
    public UIElement First { get; private set; }
    public UIElement Second { get; private set; }

    /// <summary>指定方向に2つの要素を分割配置します。</summary>
    public DockSplitContainer(Orientation orientation, UIElement first, UIElement second)
    {
        Orientation = orientation;
        First = first;
        Second = second;
        MinWidth = 90;
        MinHeight = 70;
        BuildVisualTree();
    }

    /// <summary>指定した子要素とは反対側の子要素を返します。</summary>
    public UIElement GetSibling(UIElement child) =>
        ReferenceEquals(child, First) ? Second : First;

    /// <summary>レイアウト情報を維持したまま子要素をビジュアルツリーから外します。</summary>
    public void DetachChild(UIElement child)
    {
        Children.Remove(child);
    }

    /// <summary>指定した子要素を新しい要素へ置き換えます。</summary>
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

    /// <summary>分割方向に応じた行列とGridSplitterを構築します。</summary>
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

    /// <summary>要素を指定した行または列へ配置します。</summary>
    private void Place(UIElement element, int index)
    {
        if (Orientation == System.Windows.Controls.Orientation.Horizontal)
            SetColumn(element, index);
        else
            SetRow(element, index);
        Children.Add(element);
    }
}
