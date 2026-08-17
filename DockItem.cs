using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfDockingSample;

/// <summary>ドッキング領域に表示する、UI非依存の項目を表します。</summary>
public sealed class DockItem : INotifyPropertyChanged
{
    private string _content;

    /// <summary>新しいドッキング項目を初期化します。</summary>
    /// <param name="title">タブタイトル。</param>
    /// <param name="content">項目の内容。</param>
    /// <param name="initialPlacement">初回配置先。</param>
    /// <param name="isDocument">ドキュメントの場合はtrue。</param>
    public DockItem(string title, string content, DockInitialPlacement initialPlacement, bool isDocument = false)
    {
        Title = title;
        _content = content;
        InitialPlacement = initialPlacement;
        IsDocument = isDocument;
    }

    /// <summary>タブタイトルを取得します。</summary>
    public string Title { get; }

    /// <summary>初回配置先を取得します。</summary>
    public DockInitialPlacement InitialPlacement { get; }

    /// <summary>ドキュメントかどうかを取得します。</summary>
    public bool IsDocument { get; }

    /// <summary>表示または編集する内容を取得、設定します。</summary>
    public string Content
    {
        get => _content;
        set
        {
            if (_content == value) return;
            _content = value;
            OnPropertyChanged();
        }
    }

    /// <summary>プロパティ値が変更されたときに発生します。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>PropertyChangedイベントを発生させます。</summary>
    /// <param name="propertyName">変更されたプロパティ名。</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>項目の初回配置先を表します。</summary>
public enum DockInitialPlacement { Left, Right, Bottom, Document }

/// <summary>ドラッグ中の項目と移動元領域を保持します。</summary>
public sealed class DockDragData
{
    /// <summary>ドラッグデータを初期化します。</summary>
    public DockDragData(DockItem item, DockRegion source) { Item = item; Source = source; }

    /// <summary>移動対象を取得します。</summary>
    public DockItem Item { get; }

    /// <summary>移動元領域を取得します。</summary>
    public DockRegion Source { get; }
}

/// <summary>領域内のドロップ位置を表します。</summary>
public enum DockDropPosition { Center, Left, Right, Top, Bottom }
