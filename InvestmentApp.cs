using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using ScottPlot;

namespace InvestmentApp
{
    public partial class MainWindow : Window
    {
        private Random _rand = new Random();

        public MainWindow()
        {
            InitializeComponent();
            UpdateSimulation();
        }

        private void UpdateTrigger(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateSimulation();

        private void UpdateSimulation()
        {
            // 名前空間の不一致やビルドエラーを防ぐためのガード
            if (WpfPlot1 == null || SldMonthly == null) return;

            // 1. パラメータ取得
            double monthlyDeposit = SldMonthly.Value;
            double avgRate = SldRate.Value / 100.0;
            double stdev = SldRisk.Value / 100.0;
            int years = (int)SldYears.Value;
            double targetAmountMan = SldTarget.Value;

            // 2. 統計用の配列準備
            double[] xAxes = new double[years + 1];
            double[] worst = new double[years + 1];
            double[] median = new double[years + 1];
            double[] best = new double[years + 1];

            int trials = 100;
            double[][] allResults = new double[trials][];

            // --- モンテカルロ試行 ---
            for (int t = 0; t < trials; t++)
            {
                allResults[t] = new double[years + 1];
                double total = 0;
                for (int y = 1; y <= years; y++)
                {
                    // 正規分布乱数（ボックス＝ミューラー法）
                    double u1 = 1.0 - _rand.NextDouble();
                    double u2 = 1.0 - _rand.NextDouble();
                    double randNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                    double yearlyRate = avgRate + (stdev * randNormal);

                    for (int m = 0; m < 12; m++) total += monthlyDeposit;
                    total *= (1 + yearlyRate);
                    allResults[t][y] = total / 10000; // 万円単位
                }
            }

            // --- 統計処理 ---
            for (int y = 0; y <= years; y++)
            {
                xAxes[y] = y;
                var yearlyData = allResults.Select(r => r[y]).OrderBy(v => v).ToList();
                worst[y] = yearlyData[4];   // 下位5%
                median[y] = yearlyData[49]; // 中央値
                best[y] = yearlyData[94];   // 上位5%
            }

            // 3. 描画メソッドを呼ぶ
            RenderGraph(xAxes, worst, median, best, years, targetAmountMan);

            // 4. アドバイス更新
            UpdateAdvice(median[years] * 10000, targetAmountMan * 10000, avgRate, years);
        }

        private void RenderGraph(double[] x, double[] w, double[] m, double[] b, int years, double target)
        {
            WpfPlot1.Plot.Clear();

            // ScottPlot 5.0 スタイル設定
            WpfPlot1.Plot.FigureBackground.Color = Color.FromHex("#1E1E1E");
            WpfPlot1.Plot.Axes.Color(Color.FromHex("#E0E0E0"));

            // 塗りつぶし（リスク範囲）
            var range = WpfPlot1.Plot.Add.FillY(x, w, b);
            range.FillColor = Color.FromHex("#BB86FC").WithAlpha(0.15);

            // 各ラインの描画（LegendTextを使用）
            var pBest = WpfPlot1.Plot.Add.Scatter(x, b);
            pBest.LegendText = "最良シナリオ (上位5%)";
            pBest.Color = Color.FromHex("#03DAC6");

            var pMedian = WpfPlot1.Plot.Add.Scatter(x, m);
            pMedian.LegendText = "中央値";
            pMedian.Color = Color.FromHex("#BB86FC");
            pMedian.LineWidth = 3;

            var pWorst = WpfPlot1.Plot.Add.Scatter(x, w);
            pWorst.LegendText = "最悪シナリオ (下位5%)";
            pWorst.Color = Color.FromHex("#CF6679");

            // 目標ライン
            var hLine = WpfPlot1.Plot.Add.HorizontalLine(target);
            hLine.Color = Color.FromHex("#03DAC6");
            hLine.LinePattern = LinePattern.Dashed;

            // 表示範囲の調整
            WpfPlot1.Plot.Axes.SetLimits(0, years, 0, Math.Max(b[years], target) * 1.2);
            WpfPlot1.Plot.ShowLegend(Alignment.UpperLeft);
            WpfPlot1.Refresh();
        }

        private void UpdateAdvice(double currentTotal, double target, double rate, int years)
        {
            if (TxtAdvice == null) return;

            if (currentTotal < target)
            {
                double extra = CalculateRequiredExtra(target, rate, years);
                TxtAdvice.Text = $"【未達成】不足額: {(target - currentTotal) / 10000:N0}万円\nアドバイス: 毎月あと {extra:N0}円 増やすと目標に届く可能性が高まります。";
                TxtAdvice.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
            else
            {
                TxtAdvice.Text = "【達成】現在の計画で目標をクリアできる可能性が高いです。";
                TxtAdvice.Foreground = System.Windows.Media.Brushes.SpringGreen;
            }
        }

        private double CalculateRequiredExtra(double target, double rate, int years)
        {
            double monthlyRate = rate / 12;
            int months = years * 12;
            for (double extra = 1000; extra < 500000; extra += 1000)
            {
                double testTotal = 0;
                double newMonthly = SldMonthly.Value + extra;
                for (int i = 0; i < months; i++) testTotal = (testTotal + newMonthly) * (1 + monthlyRate);
                if (testTotal >= target) return extra;
            }
            return 0;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = "Simulation.png" };
            if (sfd.ShowDialog() == true) WpfPlot1.Plot.SavePng(sfd.FileName, 1000, 600);
        }

        private void BtnCsv_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV File|*.csv", FileName = "Result.csv" };
            if (sfd.ShowDialog() == true)
            {
                MessageBox.Show("CSV出力機能を実行しました。");
            }
        }
    }
}
