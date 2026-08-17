using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfDockingSample;

public partial class MainWindow : Window
{
    private DockRegion _documentRegion = null!;
    private int _toolNumber = 1;
    private int _documentNumber = 1;
    private DockRegion? _previewRegion;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => CreateInitialLayout();
    }

    private void CreateInitialLayout()
    {
        var left = new DockRegion();
        left.AddItem(CreateSolutionItem());
        left.AddItem(CreateToolItem("クラス ビュー", "クラスとメンバーの一覧"), select: false);
        LeftRoot.Content = left;

        var right = new DockRegion();
        right.AddItem(CreatePropertiesItem());
        right.AddItem(CreateToolItem("診断ツール", "CPU 使用率\nメモリ使用量\nイベント"), select: false);
        RightRoot.Content = right;

        var bottom = new DockRegion();
        bottom.AddItem(CreateOutputItem());
        bottom.AddItem(CreateToolItem("エラー一覧", "0 エラー\n0 警告\n0 メッセージ"), select: false);
        BottomRoot.Content = bottom;

        _documentRegion = new DockRegion();
        _documentRegion.AddItem(CreateDocumentItem("Program.cs", "// Program.cs\n\nConsole.WriteLine(\"Docking sample\");"));
        _documentRegion.AddItem(CreateDocumentItem("README.md", "# WPF Docking Sample\n\nタブを別の領域へ移動できます。"), select: false);
        DocumentRoot.Content = _documentRegion;
    }

    public void MoveDockItem(DockDragData data, DockRegion target, DockDropPosition position)
    {
        if (ReferenceEquals(data.Source, target) && position == DockDropPosition.Center)
        {
            target.AddItem(data.Item);
            return;
        }

        // 1項目だけの領域を自分自身に対して分割しても意味がないため無視する。
        if (ReferenceEquals(data.Source, target) && data.Source.ItemCount == 1)
            return;

        data.Source.RemoveItem(data.Item);

        if (position == DockDropPosition.Center)
        {
            target.AddItem(data.Item);
        }
        else
        {
            var newRegion = new DockRegion();
            newRegion.AddItem(data.Item);
            SplitRegion(target, newRegion, position);
        }

        CleanupEmptyRegion(data.Source);
    }

    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(DockDragData)))
        {
            ClearDropPreview();
            e.Effects = DragDropEffects.None;
            return;
        }

        Point windowPoint = e.GetPosition(this);
        DockRegion? target = FindDropTarget(windowPoint);
        if (target is null)
        {
            ClearDropPreview();
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        Point regionPoint = e.GetPosition(target);
        DockDropPosition position = target.GetDropPosition(regionPoint);

        if (!ReferenceEquals(_previewRegion, target))
            ClearDropPreview();

        _previewRegion = target;
        target.ShowIndicator(position);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void Window_PreviewDragLeave(object sender, DragEventArgs e)
    {
        // 子要素間の移動でもDragLeaveが発生することがあるため、作業領域外の時だけ消す。
        Point point = e.GetPosition(DockWorkspace);
        if (point.X < 0 || point.Y < 0 ||
            point.X > DockWorkspace.ActualWidth || point.Y > DockWorkspace.ActualHeight)
            ClearDropPreview();
    }

    private void Window_PreviewDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(DockDragData)) is not DockDragData data)
            return;

        DockRegion? target = FindDropTarget(e.GetPosition(this));
        if (target is null)
        {
            ClearDropPreview();
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        DockDropPosition position = target.GetDropPosition(e.GetPosition(target));
        ClearDropPreview();
        MoveDockItem(data, target, position);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private DockRegion? FindDropTarget(Point windowPoint)
    {
        Point workspacePoint = TranslatePoint(windowPoint, DockWorkspace);
        if (workspacePoint.X < 0 || workspacePoint.Y < 0 ||
            workspacePoint.X > DockWorkspace.ActualWidth ||
            workspacePoint.Y > DockWorkspace.ActualHeight)
            return null;

        var regions = new List<DockRegion>();
        CollectRegions(DockWorkspace, regions);

        DockRegion? nearest = null;
        double nearestDistance = double.MaxValue;

        foreach (DockRegion region in regions)
        {
            if (!region.IsVisible || region.ActualWidth <= 0 || region.ActualHeight <= 0)
                continue;

            Rect bounds;
            try
            {
                bounds = region.TransformToAncestor(this)
                    .TransformBounds(new Rect(0, 0, region.ActualWidth, region.ActualHeight));
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            // 領域内ならその領域を即採用する。
            if (bounds.Contains(windowPoint))
                return region;

            // splitterや余白上では、矩形までの距離が最短の領域を採用する。
            double dx = windowPoint.X < bounds.Left ? bounds.Left - windowPoint.X
                : windowPoint.X > bounds.Right ? windowPoint.X - bounds.Right : 0;
            double dy = windowPoint.Y < bounds.Top ? bounds.Top - windowPoint.Y
                : windowPoint.Y > bounds.Bottom ? windowPoint.Y - bounds.Bottom : 0;
            double distance = dx * dx + dy * dy;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = region;
            }
        }

        return nearest;
    }

    private static void CollectRegions(DependencyObject parent, ICollection<DockRegion> result)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is DockRegion region)
                result.Add(region);
            else
                CollectRegions(child, result);
        }
    }

    private void ClearDropPreview()
    {
        if (_previewRegion is not null)
            _previewRegion.HideIndicator();
        _previewRegion = null;
    }

    private void SplitRegion(DockRegion target, DockRegion newRegion, DockDropPosition position)
    {
        DependencyObject? parent = LogicalTreeHelper.GetParent(target) ?? target.Parent;
        Orientation orientation = position is DockDropPosition.Left or DockDropPosition.Right
            ? Orientation.Horizontal
            : Orientation.Vertical;
        bool newFirst = position is DockDropPosition.Left or DockDropPosition.Top;

        DetachFromParent(target, parent);
        var split = newFirst
            ? new DockSplitContainer(orientation, newRegion, target)
            : new DockSplitContainer(orientation, target, newRegion);

        if (parent is DockSplitContainer parentSplit)
            parentSplit.ReplaceChild(target, split);
        else
            AttachToParent(split, parent);
    }

    private void CleanupEmptyRegion(DockRegion region)
    {
        if (region.ItemCount != 0)
            return;

        DependencyObject? parent = LogicalTreeHelper.GetParent(region) ?? region.Parent;
        if (parent is not DockSplitContainer split)
            return; // ルート領域は空のドロップ先として残す。

        UIElement sibling = split.GetSibling(region);
        DependencyObject? grandParent = LogicalTreeHelper.GetParent(split) ?? split.Parent;
        split.DetachChild(sibling);
        DetachFromParent(split, grandParent);
        if (grandParent is DockSplitContainer parentSplit)
            parentSplit.ReplaceChild(split, sibling);
        else
            AttachToParent(sibling, grandParent);
    }

    private static void DetachFromParent(UIElement element, DependencyObject? parent)
    {
        switch (parent)
        {
            case DockSplitContainer split:
                split.DetachChild(element);
                break;
            case Window window:
                window.Content = null;
                break;
            case ContentControl host:
                host.Content = null;
                break;
            case Panel panel:
                panel.Children.Remove(element);
                break;
            default:
                throw new InvalidOperationException("ドッキング要素の親を特定できません。");
        }
    }

    private static void AttachToParent(UIElement element, DependencyObject? parent)
    {
        switch (parent)
        {
            case DockSplitContainer split:
                // 呼び出し側で置換対象が必要なため、通常この分岐には来ない。
                throw new InvalidOperationException("分割コンテナーへの直接追加はできません。");
            case Window window:
                window.Content = element;
                break;
            case ContentControl host:
                host.Content = element;
                break;
            case Panel panel:
                panel.Children.Add(element);
                break;
            default:
                throw new InvalidOperationException("ドッキング要素の親を特定できません。");
        }
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string kind })
            return;

        if (kind == "Document")
        {
            string title = $"Document{_documentNumber++}.cs";
            _documentRegion.AddItem(CreateDocumentItem(title, $"// {title}\n\n// 新しいドキュメント"));
        }
        else
        {
            string title = $"ツール {_toolNumber++}";
            FindFirstRegion(RightRoot.Content as UIElement)?.AddItem(CreateToolItem(title, "追加されたツールウィンドウ"));
        }
    }

    private static DockRegion? FindFirstRegion(UIElement? element) => element switch
    {
        DockRegion region => region,
        DockSplitContainer split => FindFirstRegion(split.First),
        _ => null
    };

    private static DockItem CreateSolutionItem()
    {
        var tree = new TreeView
        {
            Background = Brushes.Transparent,
            Foreground = Brushes.Gainsboro,
            BorderThickness = new Thickness(0)
        };
        var root = new TreeViewItem { Header = "WpfDockingSample", IsExpanded = true };
        root.Items.Add("App.xaml");
        root.Items.Add("MainWindow.xaml");
        root.Items.Add("DockRegion.cs");
        tree.Items.Add(root);
        return new DockItem("ソリューション エクスプローラー", tree);
    }

    private static DockItem CreatePropertiesItem() => CreateToolItem(
        "プロパティ",
        "Name        MainWindow\nWidth       1180\nHeight      760\nBackground  #1E1E1E");

    private static DockItem CreateOutputItem() => CreateToolItem(
        "出力",
        "ビルドを開始しました...\nビルドに成功しました。\n0 エラー、0 警告");

    private static DockItem CreateToolItem(string title, string text) => new(
        title,
        new TextBox
        {
            Text = text,
            Foreground = Brushes.Gainsboro,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10),
            FontFamily = new FontFamily("Consolas"),
            IsReadOnly = true,
            AcceptsReturn = true
        });

    private static DockItem CreateDocumentItem(string title, string text) => new(
        title,
        new TextBox
        {
            Text = text,
            Foreground = Brushes.Gainsboro,
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(18),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            AcceptsReturn = true,
            AcceptsTab = true
        },
        isDocument: true);
}
