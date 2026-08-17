using System.Collections.ObjectModel;
using System.Windows.Input;

namespace WpfDockingSample;

/// <summary>MainWindowに表示する項目とコマンドを提供します。</summary>
public sealed class MainWindowViewModel
{
    private int _toolNumber = 1;
    private int _documentNumber = 1;

    /// <summary>ViewModelを初期化し、サンプル項目を作成します。</summary>
    public MainWindowViewModel()
    {
        Items = new ObservableCollection<DockItem>
        {
            new DockItem("ソリューション エクスプローラー", "WpfDockingSample\n  App.xaml\n  MainWindow.xaml\n  DockingManager.xaml", DockInitialPlacement.Left),
            new DockItem("クラス ビュー", "クラスとメンバーの一覧", DockInitialPlacement.Left),
            new DockItem("プロパティ", "Name        MainWindow\nWidth       1180\nHeight      760\nBackground  #1E1E1E", DockInitialPlacement.Right),
            new DockItem("診断ツール", "CPU 使用率\nメモリ使用量\nイベント", DockInitialPlacement.Right),
            new DockItem("出力", "ビルドを開始しました...\nビルドに成功しました。\n0 エラー、0 警告", DockInitialPlacement.Bottom),
            new DockItem("エラー一覧", "0 エラー\n0 警告\n0 メッセージ", DockInitialPlacement.Bottom),
            new DockItem("Program.cs", "// Program.cs\n\nConsole.WriteLine(\"Docking sample\");", DockInitialPlacement.Document, true),
            new DockItem("README.md", "# WPF Docking Sample\n\nタブを別の領域へ移動できます。", DockInitialPlacement.Document, true)
        };
        AddToolCommand = new RelayCommand(_ => AddTool());
        AddDocumentCommand = new RelayCommand(_ => AddDocument());
    }

    /// <summary>ドッキング表示する項目一覧を取得します。</summary>
    public ObservableCollection<DockItem> Items { get; }

    /// <summary>ツール項目を追加するコマンドを取得します。</summary>
    public ICommand AddToolCommand { get; }

    /// <summary>ドキュメント項目を追加するコマンドを取得します。</summary>
    public ICommand AddDocumentCommand { get; }

    /// <summary>右側領域へ新しいツール項目を追加します。</summary>
    private void AddTool()
    {
        string title = $"ツール {_toolNumber++}";
        Items.Add(new DockItem(title, "追加されたツールウィンドウ", DockInitialPlacement.Right));
    }

    /// <summary>ドキュメント領域へ新しいドキュメントを追加します。</summary>
    private void AddDocument()
    {
        string title = $"Document{_documentNumber++}.cs";
        Items.Add(new DockItem(title, $"// {title}\n\n// 新しいドキュメント", DockInitialPlacement.Document, true));
    }
}
