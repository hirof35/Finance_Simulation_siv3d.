using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using ScottPlot;
using ScottPlot.WPF; // これを追加

namespace tousiapp
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
            if (WpfPlot1 == null) return;

            // 1. パラメータ取得
            double monthlyDeposit = SldMonthly.Value;
            double avgRate = SldRate.Value / 100.0;
            double stdev = SldRisk.Value / 100.0;
            int years = (int)SldYears.Value;
            double targetAmountRaw = SldTarget.Value * 10000;

            // 2. モンテカルロ試行 (100回)
            int trials = 100;
            double[][] allResults = new double[trials][];
            int firstAchievementMonth = -1;

            for (int t = 0; t < trials; t++)
            {
                allResults[t] = new double[years + 1];
                double total = 0;
                for (int y = 1; y <= years; y++)
                {
                    // 正規分布乱数生成
                    double u1 = 1.0 - _rand.NextDouble();
                    double u2 = 1.0 - _rand.NextDouble();
                    double randNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                    double yearlyRate = avgRate + (stdev * randNormal);

                    for (int m = 0; m < 12; m++) total += monthlyDeposit;
                    total *= (1 + yearlyRate);
                    allResults[t][y] = total / 10000;
                }
            }

            // 3. 統計処理
            double[] xAxes = Enumerable.Range(0, years + 1).Select(x => (double)x).ToArray();
            double[] worst = new double[years + 1];
            double[] median = new double[years + 1];
            double[] best = new double[years + 1];

            for (int y = 0; y <= years; y++)
            {
                var yearEndValues = allResults.Select(r => r[y]).OrderBy(v => v).ToList();
                worst[y] = yearEndValues[4];   // 下位5%
                median[y] = yearEndValues[49]; // 中央値
                best[y] = yearEndValues[94];   // 上位5%
            }

            // 4. グラフ描画
            RenderGraph(xAxes, worst, median, best, years, SldTarget.Value);

            // 5. アドバイス更新
            UpdateAdvice(median[years] * 10000, targetAmountRaw, avgRate, years);
        }

        private void RenderGraph(double[] x, double[] w, double[] m, double[] b, int years, double target)
        {
            WpfPlot1.Plot.Clear();

            // ScottPlot 5.0 の最新の色の書き方
            WpfPlot1.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
            WpfPlot1.Plot.Axes.Color(ScottPlot.Color.FromHex("#E0E0E0"));

            var range = WpfPlot1.Plot.Add.FillY(x, w, b);
            range.FillColor = ScottPlot.Color.FromHex("#BB86FC").WithAlpha(0.15f);

            // Scatter の戻り値を使って色を設定
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
            var hLine = WpfPlot1.Plot.Add.HorizontalLine(target);
            hLine.Color = ScottPlot.Color.FromHex("#03DAC6");
            hLine.LinePattern = LinePattern.Dashed;

            WpfPlot1.Plot.Axes.SetLimits(0, years, 0, Math.Max(b[years], target) * 1.2);
            WpfPlot1.Refresh();
        }

        private void UpdateAdvice(double currentTotal, double target, double rate, int years)
        {
            if (currentTotal < target)
            {
                double extra = CalculateRequiredExtra(target, rate, years);
                TxtAdvice.Text = $"【未達成】不足額: {(target - currentTotal) / 10000:N0}万円\nアドバイス: 毎月あと {extra:N0}円 増やすと中央値で目標に届きます。";
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
                // 中央値の結果を出力
                StringBuilder csv = new StringBuilder("Year,MedianAsset(ManYen)\n");
                // ... (各年のデータをループで追加)
                File.WriteAllText(sfd.FileName, csv.ToString());
                MessageBox.Show("CSVを出力しました。");
            }
        }
    }
}
