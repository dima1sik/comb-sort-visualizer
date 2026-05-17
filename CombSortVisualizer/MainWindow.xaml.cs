using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CombSortVisualizer
{
    public partial class MainWindow : Window
    {
        int[] _source, _data;
        int _gap, _index, _hi1 = -1, _hi2 = -1;
        bool _swapped, _playing, _lastSwap;
        long _compares, _swaps;
        readonly Stopwatch _watch = new Stopwatch();
        readonly Random _rng = new Random();

        public MainWindow()
        {
            InitializeComponent();
            UpdateMetrics();
        }

        void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            int n;
            if (!int.TryParse(SizeBox.Text, out n) || n <= 0)
                n = 40;
            if (n > 200) n = 200;

            _source = Enumerable.Range(1, n).ToArray();

            for (int k = n - 1; k > 0; k--)
            {
                int j = _rng.Next(k + 1);
                int tmp = _source[k];
                _source[k] = _source[j];
                _source[j] = tmp;
            }

            ResetSort();
            DrawArray();
        }

        void ResetSort()
        {
            _data = _source != null ? _source.ToArray() : null;
            _gap = _data != null ? _data.Length : 0;
            _index = 0;
            _swapped = true;
            _playing = false;
            _lastSwap = false;
            _hi1 = _hi2 = -1;
            _compares = 0;
            _swaps = 0;
            _watch.Reset();
            UpdateMetrics();
        }

        async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_data == null) return;

            _playing = true;
            if (!_watch.IsRunning)
                _watch.Start();

            while (_playing && StepOnce())
            {
                DrawArray();
                UpdateMetrics();

                int delay = (int)(250 - SpeedSlider.Value * 230); 
                if (delay > 0)
                    await Task.Delay(delay);
            }

            _watch.Stop();
        }

        void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            _playing = false;
            _watch.Stop();
            UpdateMetrics();
        }

        void BtnStep_Click(object sender, RoutedEventArgs e)
        {
            if (_playing || _data == null) return;

            if (!_watch.IsRunning)
                _watch.Start();

            if (StepOnce())
            {
                DrawArray();
                UpdateMetrics();
            }
            else
            {
                _watch.Stop();
            }
        }

        void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetSort();
            DrawArray();
        }

        bool StepOnce()
        {
            if (_data == null) return false;

            int n = _data.Length;
            if (n <= 1) return false;

            if (_index >= n - _gap)
            {
                if (_gap > 1)
                {
                    _gap = (int)(_gap / 1.247f);
                    if (_gap < 1) _gap = 1;
                }
                else if (!_swapped)
                {
                    return false; 
                }

                _index = 0;
                _swapped = false;
            }

            int j = _index + _gap;
            if (j >= n) return false;

            _hi1 = _index;
            _hi2 = j;
            _compares++;

            if (_data[_index] > _data[j])
            {
                int tmp = _data[_index];
                _data[_index] = _data[j];
                _data[j] = tmp;

                _swaps++;
                _swapped = true;
                _lastSwap = true;
            }
            else
            {
                _lastSwap = false;
            }

            _index++;
            return true;
        }

        void DrawArray()
        {
            int[] arr = _data;
            if (arr == null || arr.Length == 0)
            {
                Img.Source = null;
                return;
            }

            int w = (int)(Img.ActualWidth > 0 ? Img.ActualWidth : 800);
            int h = (int)(Img.ActualHeight > 0 ? Img.ActualHeight : 300);
            if (w <= 0 || h <= 0) return;

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(5, 8, 20)),
                                 null, new Rect(0, 0, w, h));

                int n = arr.Length;
                double barW = (double)w / n;
                double topPad = 20;
                double scale = (double)(h - topPad) / Math.Max(1, arr.Max());

                for (int k = 0; k < n; k++)
                {
                    Brush b;
                    if (k == _hi1 || k == _hi2)
                        b = _lastSwap
                            ? new SolidColorBrush(Color.FromRgb(180, 180, 180))
                            : new SolidColorBrush(Color.FromRgb(130, 130, 130));
                    else
                        b = new SolidColorBrush(Color.FromRgb(70, 70, 70));

                    double barH = arr[k] * scale;
                    double x = k * barW;
                    double y = h - barH;

                    dc.DrawRectangle(b, null, new Rect(x, y, barW - 1, barH));

                    FormattedText text = new FormattedText(
                        arr[k].ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(new FontFamily("Segoe UI"),
                                     FontStyles.Normal,
                                     FontWeights.Bold,
                                     FontStretches.Normal),
                        Math.Max(10, barW * 0.35),
                        Brushes.White,
                        1.0);

                    double textX = x + (barW - text.Width) / 2;
                    double textY = y - text.Height - 2; 

                    if (textY < 0) textY = 0; 

                    dc.DrawText(text, new Point(textX, textY));
                }
            }

            RenderTargetBitmap bmp =
                new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);
            Img.Source = bmp;
        }

        void UpdateMetrics()
        {
            int n = _data != null ? _data.Length : 0;
            LblMetrics.Content =
                string.Format("N={0,-3} | cmp={1,-6} | swaps={2,-6} | time={3,-6} ms",
                    n, _compares, _swaps, _watch.ElapsedMilliseconds);
        }
    }
}