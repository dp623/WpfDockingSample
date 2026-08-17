# 前提
本プログラムはAI（Codex）にて生成

# WPF Docking UI Sample

WPF標準機能だけで実装した、Visual Studio風の簡易ドッキングUIです。

## 実行

```powershell
dotnet run --project .\WpfDockingSample.csproj
```

## 操作

タブの見出しをドラッグし、移動先の領域へ重ねます。

- **領域中央へドロップ**: 既存領域のタブとして追加
- **領域の左・右端へドロップ**: 領域を左右に分割
- **領域の上・下端へドロップ**: 領域を上下に分割
- **アプリの領域外へドロップ**: フローティングウィンドウ化
- **境界線をドラッグ**: 各領域の幅または高さを変更

青いプレビューが、ドロップ後に使用される範囲を示します。ツールウィンドウとドキュメントは同じ `DockItem` として扱われるため、どちらも任意の領域へ移動できます。

## 実装構成

- `DockItem.cs`: 移動可能なコンテンツのデータ
- `DockRegion.cs`: 複数項目のタブ表示、ドラッグ開始、ドロップ位置判定
- `DockSplitContainer.cs`: 再帰的な領域分割と `GridSplitter`
- `MainWindow.xaml.cs`: タブ移動、分割ツリーの組み替え、空領域の整理
- `FloatingDockWindow.cs`: 独立ウィンドウ表示

## ドッキングツリー

分割操作を繰り返すと、次のようなツリーになります。

```text
DockSplitContainer（左右分割）
├─ DockRegion（タブA、タブB）
└─ DockSplitContainer（上下分割）
   ├─ DockRegion（タブC）
   └─ DockRegion（タブD、タブE）
```

各 `DockSplitContainer` が2つの子と1本の `GridSplitter` を持つため、入れ子になったすべての領域を個別にサイズ変更できます。

学習用のため、Visual StudioにあるAuto Hide、タブ順のドラッグ変更、レイアウト保存・復元などはまだ含めていません。
