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

public sealed record DockDragData(DockItem Item, DockRegion Source);

public enum DockDropPosition
{
    Center,
    Left,
    Right,
    Top,
    Bottom
}
