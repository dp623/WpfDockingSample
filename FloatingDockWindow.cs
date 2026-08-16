using System.Windows;
using System.Windows.Media;

namespace WpfDockingSample;

public sealed class FloatingDockWindow : Window
{
    public FloatingDockWindow(DockRegion region, Point screenPoint)
    {
        Title = "Floating Dock Region";
        Width = 360;
        Height = 260;
        MinWidth = 200;
        MinHeight = 140;
        Left = screenPoint.X - 40;
        Top = screenPoint.Y - 20;
        WindowStyle = WindowStyle.ToolWindow;
        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        Content = region;
    }
}
