using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfDockingSample;

/// <summary>
/// タブ化、領域分割、サイズ変更を提供する再利用可能なドッキングコントロールです。
/// </summary>
public partial class DockingManager : UserControl
{
    /// <summary>ItemsSource依存関係プロパティを識別します。</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable<DockItem>), typeof(DockingManager),
        new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>ItemTemplate依存関係プロパティを識別します。</summary>
    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(DockingManager),
        new PropertyMetadata(null, OnItemTemplateChanged));

    private DockRegion? _previewRegion;
    private DockRegion? _leftRegion;
    private DockRegion? _rightRegion;
    private DockRegion? _bottomRegion;
    private DockRegion? _documentRegion;
    private INotifyCollectionChanged? _observedCollection;

    /// <summary>DockingManagerを初期化します。</summary>
    public DockingManager()
    {
        InitializeComponent();
        Loaded += Control_Loaded;
        Unloaded += Control_Unloaded;
    }

    /// <summary>表示するDockItemの列挙を取得、設定します。</summary>
    public IEnumerable<DockItem>? ItemsSource
    {
        get => (IEnumerable<DockItem>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>各DockItemの本文表示に使うDataTemplateを取得、設定します。</summary>
    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>ItemsSource変更時にコレクション監視とレイアウトを更新します。</summary>
    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var manager = (DockingManager)d;
        manager.ObserveCollection(e.OldValue as INotifyCollectionChanged, e.NewValue as INotifyCollectionChanged);
        if (manager.IsLoaded)
            manager.RebuildLayout();
    }

    /// <summary>ItemTemplate変更時に既存タブを新しいテンプレートで再構築します。</summary>
    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var manager = (DockingManager)d;
        if (manager.IsLoaded)
            manager.RebuildLayout();
    }

    /// <summary>ロード時にコレクションを監視し、初期レイアウトを構築します。</summary>
    private void Control_Loaded(object sender, RoutedEventArgs e)
    {
        ObserveCollection(null, ItemsSource as INotifyCollectionChanged);
        RebuildLayout();
    }

    /// <summary>アンロード時にコレクションイベントを解除します。</summary>
    private void Control_Unloaded(object sender, RoutedEventArgs e)
    {
        ObserveCollection(_observedCollection, null);
    }

    /// <summary>監視対象コレクションのCollectionChanged購読を切り替えます。</summary>
    private void ObserveCollection(INotifyCollectionChanged? oldCollection, INotifyCollectionChanged? newCollection)
    {
        if (oldCollection is not null)
            oldCollection.CollectionChanged -= Items_CollectionChanged;
        if (_observedCollection is not null && !ReferenceEquals(_observedCollection, oldCollection))
            _observedCollection.CollectionChanged -= Items_CollectionChanged;

        _observedCollection = newCollection;
        if (newCollection is not null)
        {
            newCollection.CollectionChanged -= Items_CollectionChanged;
            newCollection.CollectionChanged += Items_CollectionChanged;
        }
    }

    /// <summary>ItemsSourceへの追加、削除、リセットをレイアウトへ反映します。</summary>
    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            RebuildLayout();
            return;
        }

        if (e.OldItems is not null)
        {
            foreach (DockItem item in e.OldItems)
                RemoveItemFromLayout(item);
        }

        if (e.NewItems is not null)
        {
            foreach (DockItem item in e.NewItems)
                AddItemToInitialRegion(item);
        }
    }

    /// <summary>4つのルート領域を作り、ItemsSourceを初期配置します。</summary>
    private void RebuildLayout()
    {
        ClearDropPreview();
        _leftRegion = new DockRegion(ItemTemplate);
        _rightRegion = new DockRegion(ItemTemplate);
        _bottomRegion = new DockRegion(ItemTemplate);
        _documentRegion = new DockRegion(ItemTemplate);
        LeftRoot.Content = _leftRegion;
        RightRoot.Content = _rightRegion;
        BottomRoot.Content = _bottomRegion;
        DocumentRoot.Content = _documentRegion;

        if (ItemsSource is null)
            return;
        foreach (DockItem item in ItemsSource)
            AddItemToInitialRegion(item, false);
    }

    /// <summary>項目をInitialPlacementで指定されたルート領域へ追加します。</summary>
    private void AddItemToInitialRegion(DockItem item, bool select = true)
    {
        DockRegion? target = item.InitialPlacement switch
        {
            DockInitialPlacement.Left => FindFirstRegion(LeftRoot.Content as UIElement),
            DockInitialPlacement.Right => FindFirstRegion(RightRoot.Content as UIElement),
            DockInitialPlacement.Bottom => FindFirstRegion(BottomRoot.Content as UIElement),
            _ => FindFirstRegion(DocumentRoot.Content as UIElement)
        };
        target?.AddItem(item, select);
    }

    /// <summary>項目を現在所属している領域から削除します。</summary>
    private void RemoveItemFromLayout(DockItem item)
    {
        DockRegion? region = EnumerateRegions().FirstOrDefault(candidate => candidate.Items.Contains(item));
        if (region is null || !region.RemoveItem(item))
            return;
        CleanupEmptyRegion(region);
    }

    /// <summary>ドラッグ位置に対応する領域とプレビューを更新します。</summary>
    private void Control_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(DockDragData)))
        {
            ClearDropPreview();
            e.Effects = DragDropEffects.None;
            return;
        }

        DockRegion? target = FindDropTarget(e.GetPosition(this));
        if (target is null)
        {
            ClearDropPreview();
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        DockDropPosition position = target.GetDropPosition(e.GetPosition(target));
        if (!ReferenceEquals(_previewRegion, target)) ClearDropPreview();
        _previewRegion = target;
        target.ShowIndicator(position);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    /// <summary>コントロール外へ移動したときにプレビューを消去します。</summary>
    private void Control_PreviewDragLeave(object sender, DragEventArgs e)
    {
        Point point = e.GetPosition(DockWorkspace);
        if (point.X < 0 || point.Y < 0 || point.X > DockWorkspace.ActualWidth || point.Y > DockWorkspace.ActualHeight)
            ClearDropPreview();
    }

    /// <summary>有効なドロップをタブ移動または領域分割として確定します。</summary>
    private void Control_PreviewDrop(object sender, DragEventArgs e)
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

    /// <summary>項目を対象領域へ移動し、必要に応じて領域を分割します。</summary>
    private void MoveDockItem(DockDragData data, DockRegion target, DockDropPosition position)
    {
        if (ReferenceEquals(data.Source, target) && position == DockDropPosition.Center)
        {
            target.AddItem(data.Item);
            return;
        }
        if (ReferenceEquals(data.Source, target) && data.Source.ItemCount == 1)
            return;

        data.Source.RemoveItem(data.Item);
        if (position == DockDropPosition.Center)
            target.AddItem(data.Item);
        else
        {
            var newRegion = new DockRegion(ItemTemplate);
            newRegion.AddItem(data.Item);
            SplitRegion(target, newRegion, position);
        }
        CleanupEmptyRegion(data.Source);
    }

    /// <summary>対象領域を指定方向へ2分割し、新領域を挿入します。</summary>
    private void SplitRegion(DockRegion target, DockRegion newRegion, DockDropPosition position)
    {
        DependencyObject? parent = LogicalTreeHelper.GetParent(target) ?? target.Parent;
        Orientation orientation = position is DockDropPosition.Left or DockDropPosition.Right
            ? Orientation.Horizontal : Orientation.Vertical;
        bool newFirst = position is DockDropPosition.Left or DockDropPosition.Top;
        DetachFromParent(target, parent);
        var split = newFirst ? new DockSplitContainer(orientation, newRegion, target)
                             : new DockSplitContainer(orientation, target, newRegion);
        if (parent is DockSplitContainer parentSplit) parentSplit.ReplaceChild(target, split);
        else AttachToParent(split, parent);
    }

    /// <summary>空になった入れ子領域を削除し、兄弟領域を繰り上げます。</summary>
    private void CleanupEmptyRegion(DockRegion region)
    {
        if (region.ItemCount != 0) return;
        DependencyObject? parent = LogicalTreeHelper.GetParent(region) ?? region.Parent;
        if (parent is not DockSplitContainer split) return;
        UIElement sibling = split.GetSibling(region);
        DependencyObject? grandParent = LogicalTreeHelper.GetParent(split) ?? split.Parent;
        split.DetachChild(sibling);
        DetachFromParent(split, grandParent);
        if (grandParent is DockSplitContainer parentSplit) parentSplit.ReplaceChild(split, sibling);
        else AttachToParent(sibling, grandParent);
    }

    /// <summary>要素を現在の論理・ビジュアル親から取り外します。</summary>
    private static void DetachFromParent(UIElement element, DependencyObject? parent)
    {
        switch (parent)
        {
            case DockSplitContainer split: split.DetachChild(element); break;
            case ContentControl host: host.Content = null; break;
            case Panel panel: panel.Children.Remove(element); break;
            default: throw new InvalidOperationException("ドッキング要素の親を特定できません。");
        }
    }

    /// <summary>要素を指定した構造上の親へ追加します。</summary>
    private static void AttachToParent(UIElement element, DependencyObject? parent)
    {
        switch (parent)
        {
            case DockSplitContainer: throw new InvalidOperationException("分割コンテナーへの直接追加はできません。");
            case ContentControl host: host.Content = element; break;
            case Panel panel: panel.Children.Add(element); break;
            default: throw new InvalidOperationException("ドッキング要素の親を特定できません。");
        }
    }

    /// <summary>作業領域内の座標に対し、包含または最寄りのDockRegionを返します。</summary>
    private DockRegion? FindDropTarget(Point controlPoint)
    {
        Point workspacePoint = TranslatePoint(controlPoint, DockWorkspace);
        if (workspacePoint.X < 0 || workspacePoint.Y < 0 || workspacePoint.X > DockWorkspace.ActualWidth || workspacePoint.Y > DockWorkspace.ActualHeight)
            return null;

        DockRegion? nearest = null;
        double nearestDistance = double.MaxValue;
        foreach (DockRegion region in EnumerateRegions())
        {
            if (!region.IsVisible || region.ActualWidth <= 0 || region.ActualHeight <= 0) continue;
            Rect bounds = region.TransformToAncestor(this).TransformBounds(new Rect(0, 0, region.ActualWidth, region.ActualHeight));
            if (bounds.Contains(controlPoint)) return region;
            double dx = controlPoint.X < bounds.Left ? bounds.Left - controlPoint.X : controlPoint.X > bounds.Right ? controlPoint.X - bounds.Right : 0;
            double dy = controlPoint.Y < bounds.Top ? bounds.Top - controlPoint.Y : controlPoint.Y > bounds.Bottom ? controlPoint.Y - bounds.Bottom : 0;
            double distance = dx * dx + dy * dy;
            if (distance < nearestDistance) { nearestDistance = distance; nearest = region; }
        }
        return nearest;
    }

    /// <summary>現在のビジュアルツリーから全DockRegionを列挙します。</summary>
    private IEnumerable<DockRegion> EnumerateRegions()
    {
        var regions = new List<DockRegion>();
        CollectRegions(DockWorkspace, regions);
        return regions;
    }

    /// <summary>指定要素以下にあるDockRegionを再帰的に収集します。</summary>
    private static void CollectRegions(DependencyObject parent, ICollection<DockRegion> result)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is DockRegion region) result.Add(region);
            else CollectRegions(child, result);
        }
    }

    /// <summary>指定要素ツリーの先頭DockRegionを返します。</summary>
    private static DockRegion? FindFirstRegion(UIElement? element) => element switch
    {
        DockRegion region => region,
        DockSplitContainer split => FindFirstRegion(split.First),
        _ => null
    };

    /// <summary>現在表示中のドロッププレビューを消去します。</summary>
    private void ClearDropPreview()
    {
        _previewRegion?.HideIndicator();
        _previewRegion = null;
    }
}
