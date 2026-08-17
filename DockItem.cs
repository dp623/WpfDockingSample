using System.Windows;

namespace WpfDockingSample;

public sealed class DockItem
{
    public string Title { get; }
    public UIElement Content { get; }
    public bool IsDocument { get; }

    public DockItem(string title, UIElement content, bool isDocument = false)
    {
        Title = title;
        Content = content;
        IsDocument = isDocument;
    }
}

public sealed class DockDragData
{
    public DockItem Item { get; }
    public DockRegion Source { get; }

    public DockDragData(DockItem item, DockRegion source)
    {
        Item = item;
        Source = source;
    }
}

public enum DockDropPosition
{
    Center,
    Left,
    Right,
    Top,
    Bottom
}
