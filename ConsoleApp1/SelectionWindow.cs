using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace SelectionApp
{
    public class SelectionWindow : Window
    {
        private RenderTargetBitmap screenBitmap;
        private Point startPoint, endPoint;
        private bool isSelecting = false;
        private EditingWindow editingWindow;
        private Canvas canvas;

        public SelectionWindow()
        {
            this.Width = 1920;
            this.Height = 1080;
            this.WindowState = WindowState.Maximized;
            this.Background = Brushes.Black;

            canvas = new Canvas();
            this.Content = canvas;

            canvas.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && editingWindow == null)
                {
                    startPoint = e.GetPosition(canvas);
                    endPoint = startPoint;
                    isSelecting = true;
                }
            };

            canvas.PointerMoved += (s, e) =>
            {
                if (isSelecting)
                {
                    endPoint = e.GetPosition(canvas);
                    canvas.InvalidateVisual();
                }
            };

            canvas.PointerReleased += (s, e) =>
            {
                if (isSelecting)
                {
                    isSelecting = false;
                    var rect = GetRectangle(startPoint, endPoint);
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        var selectedBmp = new RenderTargetBitmap(new PixelSize((int)rect.Width, (int)rect.Height));
                        using (var context = selectedBmp.CreateDrawingContext(null))
                        {
                            context.DrawImage(screenBitmap, rect, new Rect(0, 0, rect.Width, rect.Height));
                        }
                        editingWindow = new EditingWindow(selectedBmp, rect.Location, this);
                        editingWindow.Show();
                        editingWindow.Closed += (s, args) =>
                        {
                            editingWindow = null;
                            canvas.InvalidateVisual();
                        };
                        canvas.IsHitTestVisible = false;
                        canvas.InvalidateVisual();
                    }
                }
            };

            CaptureScreen();
        }

        private void CaptureScreen()
        {
            screenBitmap = new RenderTargetBitmap(new PixelSize((int)Width, (int)Height));
            using (var context = screenBitmap.CreateDrawingContext(null))
            {
                context.FillRectangle(Brushes.White, new Rect(0, 0, Width, Height));
                var text = new FormattedText(
                    "Screen capture placeholder",
                    Typeface.Default,
                    20,
                    TextAlignment.Left,
                    TextWrapping.NoWrap,
                    Size.Infinity
                );
                context.DrawText(Brushes.Black, new Point(Width / 2 - 100, Height / 2), text);
            }
            canvas.Background = new ImageBrush(screenBitmap);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (isSelecting)
            {
                var rect = GetRectangle(startPoint, endPoint);
                context.DrawRectangle(new Pen(Brushes.LightBlue, 3) { DashStyle = DashStyle.Dash }, rect);
            }
            if (editingWindow != null)
            {
                context.FillRectangle(new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)), new Rect(0, 0, Width, Height));
            }
        }

        private Rect GetRectangle(Point p1, Point p2)
        {
            double x = Math.Min(p1.X, p2.X);
            double y = Math.Min(p1.Y, p2.Y);
            double width = Math.Abs(p1.X - p2.X);
            double height = Math.Abs(p1.Y - p2.Y);
            return new Rect(x, y, width, height);
        }

        public void EnableInteraction()
        {
            canvas.IsHitTestVisible = true;
            canvas.InvalidateVisual();
        }
    }
}