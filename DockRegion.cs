using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfDockingSample;

public sealed class DockRegion : Border
{
    private readonly TabControl _tabs;
    private readonly Border _dropIndicator;
    private readonly Dictionary<DockItem, TabItem> _tabByItem = new Dictionary<DockItem, TabItem>();
    private Point _mouseDownPoint;
    private TabItem? _dragCandidate;

    public IReadOnlyCollection<DockItem> Items => _tabByItem.Keys;
    public int ItemCount => _tabByItem.Count;

    public DockRegion()
    {
        Background = new SolidColorBrush(Color.FromRgb(37, 37, 38));
        BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70));
        BorderThickness = new Thickness(1);
        MinWidth = 90;
        MinHeight = 70;

        var root = new Grid();
        _tabs = new TabControl
        {
            Background = Background,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        };

        _dropIndicator = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(150, 0, 122, 204)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 180, 255)),
            BorderThickness = new Thickness(2),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };

        root.Children.Add(_tabs);
        root.Children.Add(_dropIndicator);
        Panel.SetZIndex(_dropIndicator, 100);
        Child = root;

    }

    public void AddItem(DockItem item, bool select = true)
    {
        if (_tabByItem.ContainsKey(item))
        {
            if (select)
                _tabs.SelectedItem = _tabByItem[item];
            return;
        }

        var tab = new TabItem
        {
            Header = item.Title,
            Content = item.Content,
            Tag = item,
            Foreground = Brushes.Gainsboro,
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70))
        };
        tab.PreviewMouseLeftButtonDown += Tab_MouseLeftButtonDown;
        tab.PreviewMouseMove += Tab_MouseMove;
        tab.PreviewMouseLeftButtonUp += (_, _) => _dragCandidate = null;

        _tabByItem.Add(item, tab);
        _tabs.Items.Add(tab);
        if (select)
            _tabs.SelectedItem = tab;
    }

    public bool RemoveItem(DockItem item)
    {
        TabItem? tab;
        if (!_tabByItem.TryGetValue(item, out tab))
            return false;

        _tabByItem.Remove(item);
        // UIElementは複数の親を持てないため、移動前にContentを明示的に外す。
        tab.Content = null;
        _tabs.Items.Remove(tab);
        return true;
    }

    private void Tab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TabItem tab)
            return;

        // タブ本文のTextBox操作ではなく、ヘッダー付近からのみドラッグを開始する。
        if (e.GetPosition(tab).Y > 34)
            return;

        _dragCandidate = tab;
        _mouseDownPoint = e.GetPosition(this);
    }

    private void Tab_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragCandidate is null || e.LeftButton != MouseButtonState.Pressed ||
            _dragCandidate.Tag is not DockItem item)
            return;

        Point current = e.GetPosition(this);
        if (Math.Abs(current.X - _mouseDownPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _mouseDownPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _dragCandidate = null;
        var data = new DockDragData(item, this);
        DragDropEffects result = DragDrop.DoDragDrop(
            this,
            new DataObject(typeof(DockDragData), data),
            DragDropEffects.Move);

        // Move以外（ウィンドウ外など）は何も変更せず、元の位置へ戻す。
        // 実際のRemoveItemは有効なDropが確定した後にだけ行われる。
        if (result != DragDropEffects.Move)
            return;
    }

    internal DockDropPosition GetDropPosition(Point point)
    {
        // 各辺30%を分割用に使い、中央40%をタブ化用に使う。
        // 固定px上限をなくし、大きな領域でもドロップしやすくする。
        double edgeX = ActualWidth * 0.30;
        double edgeY = ActualHeight * 0.30;

        if (point.X < edgeX) return DockDropPosition.Left;
        if (point.X > ActualWidth - edgeX) return DockDropPosition.Right;
        if (point.Y < edgeY) return DockDropPosition.Top;
        if (point.Y > ActualHeight - edgeY) return DockDropPosition.Bottom;
        return DockDropPosition.Center;
    }

    internal void ShowIndicator(DockDropPosition position)
    {
        _dropIndicator.Visibility = Visibility.Visible;
        _dropIndicator.Margin = position switch
        {
            DockDropPosition.Left => new Thickness(0, 0, ActualWidth * 0.55, 0),
            DockDropPosition.Right => new Thickness(ActualWidth * 0.55, 0, 0, 0),
            DockDropPosition.Top => new Thickness(0, 0, 0, ActualHeight * 0.55),
            DockDropPosition.Bottom => new Thickness(0, ActualHeight * 0.55, 0, 0),
            _ => new Thickness(ActualWidth * 0.18, ActualHeight * 0.18,
                               ActualWidth * 0.18, ActualHeight * 0.18)
        };
    }

    internal void HideIndicator()
    {
        _dropIndicator.Visibility = Visibility.Collapsed;
    }
}
