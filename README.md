Investment Pro: Monte Carlo SimulatorC# (WPF) と ScottPlot 5.0 を使用した、モンテカルロ法による資産運用シミュレーターです。
単なる利回り計算ではなく、ボラティリティ（リスク）を考慮した統計的な予測と可視化を行います。
📊 アプリケーションの特徴モンテカルロ・シミュレーション: 乱数（ボックス＝ミューラー法）を用いて100通りの市場推移を試行し、統計的に資産推移を予測します。
動的ビジュアライゼーション: スライダーを動かすだけで、Best（上位5%）、中央値、Worst（下位5%）の推移がリアルタイムに更新されます。
目標達成アドバイス: 目標金額に届かない場合、中央値で達成するために必要な「追加積立額」を自動計算して提示します。
ダークモードUI: 視認性の高いダークテーマを採用したモダンなインターフェース。エクスポート機能: シミュレーション結果を画像（PNG）やデータ（CSV）として保存可能です。
🚀 技術スタックFramework: .NET / WPF (Windows Presentation Foundation)Plotting Library: ScottPlot 5.0Algorithm: Monte Carlo Method (Normal distribution via Box-Muller transform)🛠 インストールと実行依存関係.NET 6.0 / 7.0 / 8.0 SDK のいずれかNuGetパッケージ: ScottPlot.WPF手順リポジトリをクローンします。
Visual Studio でソリューションを開きます。NuGet パッケージマネージャーから ScottPlot.WPF をインストールします。
ビルドして実行します。
📖 シミュレーションのロジック本アプリでは、以下の計算式に基づき資産推移を算出しています。
$$YearlyRate = AverageRate + (StandardDeviation \times RandomNormal)$$AverageRate: 設定した期待利回りStandardDeviation: 設定したリスク（標準偏差）RandomNormal: 平均0、分散1の正規分布乱数📂 ファイル構成MainWindow.xaml: スライダーやボタン、グラフ領域を定義するUIレイアウト。MainWindow.xaml.cs: シミュレーションアルゴリズムとグラフ描画ロジック。
📝 免責事項本アプリケーションは投資結果を保証するものではありません。
金融市場の過去のデータに基づいた統計的モデルによる推測であり、実際の運用には慎重な判断が必要です。
<img width="1919" height="1029" alt="スクリーンショット 2026-04-30 182814" src="https://github.com/user-attachments/assets/76034e7b-f8f2-4784-9900-e9f3eed81c57" />
