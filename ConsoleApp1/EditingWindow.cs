using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SelectionApp
{
    public class EditingWindow : Window
    {
        private RenderTargetBitmap originalImage;
        private List<DrawOperation> operations = new List<DrawOperation>();
        private DrawOperation currentOperation = null;
        private Tool currentTool = Tool.None;
        private Canvas canvas;
        private TextBox textInputBox = null;
        private SelectionWindow parentWindow;

        public EditingWindow(RenderTargetBitmap image, Point location, SelectionWindow parent)
        {
            originalImage = image;
            parentWindow = parent;
            this.Width = image.PixelSize.Width;
            this.Height = image.PixelSize.Height;
            this.Position = new PixelPoint((int)location.X, (int)location.Y);
            this.Background = Brushes.Gray;

            canvas = new Canvas
            {
                Width = image.PixelSize.Width,
                Height = image.PixelSize.Height,
                Background = new ImageBrush(image)
            };
            this.Content = canvas;

            canvas.PointerPressed += (s, e) =>
            {
                var position = e.GetPosition(canvas);
                if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
                {
                    if (currentTool == Tool.Rectangle)
                    {
                        currentOperation = new DrawRectangle { StartPoint = position, EndPoint = position, Color = Colors.Red, Thickness = 2 };
                    }
                    else if (currentTool == Tool.Circle)
                    {
                        currentOperation = new DrawCircle { StartPoint = position, EndPoint = position, Color = Colors.Red, Thickness = 2 };
                    }
                    else if (currentTool == Tool.Text)
                    {
                        foreach (var op in operations)
                        {
                            if (op is AddText text && text.GetBounds().Contains(position))
                            {
                                currentOperation = text;
                                return;
                            }
                        }
                        if (textInputBox == null)
                        {
                            textInputBox = new TextBox
                            {
                                Width = 100,
                                Height = 20,
                                Background = Brushes.White,
                                BorderBrush = Brushes.Black,
                                BorderThickness = new Thickness(1)
                            };
                            Canvas.SetLeft(textInputBox, position.X);
                            Canvas.SetTop(textInputBox, position.Y);
                            canvas.Children.Add(textInputBox);
                            textInputBox.Focus();
                            textInputBox.KeyDown += (s, ke) =>
                            {
                                if (ke.Key == Key.Enter && !string.IsNullOrEmpty(textInputBox.Text))
                                {
                                    operations.Add(new AddText
                                    {
                                        Text = textInputBox.Text,
                                        Position = new Point(Canvas.GetLeft(textInputBox), Canvas.GetTop(textInputBox)),
                                        Typeface = new Typeface("Arial"),
                                        FontSize = 12,
                                        Color = Colors.Red
                                    });
                                    canvas.Children.Remove(textInputBox);
                                    textInputBox = null;
                                    canvas.InvalidateVisual();
                                }
                                else if (ke.Key == Key.Escape)
                                {
                                    canvas.Children.Remove(textInputBox);
                                    textInputBox = null;
                                    canvas.InvalidateVisual();
                                }
                            };
                        }
                    }
                    else if (currentTool == Tool.Mosaic)
                    {
                        ApplyMosaic(position);
                    }
                }
            };

            canvas.PointerMoved += (s, e) =>
            {
                var position = e.GetPosition(canvas);
                if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
                {
                    if (currentOperation != null)
                    {
                        if (currentOperation is DrawRectangle rect) rect.EndPoint = position;
                        else if (currentOperation is DrawCircle circle) circle.EndPoint = position;
                        else if (currentOperation is AddText text) text.Position = position;
                        canvas.InvalidateVisual();
                    }
                    else if (currentTool == Tool.Mosaic)
                    {
                        ApplyMosaic(position);
                    }
                }
            };

            canvas.PointerReleased += (s, e) =>
            {
                if (currentOperation != null)
                {
                    operations.Add(currentOperation);
                    currentOperation = null;
                    canvas.InvalidateVisual();
                }
            };

            var toolbar = new ToolbarWindow(this);
            toolbar.Show();
            toolbar.Position = new PixelPoint(
                this.Position.X + (int)this.Width + 20,
                this.Position.Y + (int)this.Height - (int)toolbar.Height
            );
            this.Closed += (s, e) => toolbar.Close();
        }

        public void SetTool(Tool tool)
        {
            currentTool = tool;
            canvas.Cursor = tool == Tool.Mosaic ? new Cursor(StandardCursorType.Cross) : null;
        }

        public void Undo()
        {
            if (operations.Count > 0)
            {
                operations.RemoveAt(operations.Count - 1);
                canvas.InvalidateVisual();
            }
        }

        public async Task SaveToClipboard()
        {
            var finalImage = new RenderTargetBitmap(new PixelSize((int)originalImage.PixelSize.Width, (int)originalImage.PixelSize.Height));
            using (var context = finalImage.CreateDrawingContext(null))
            {
                context.DrawImage(originalImage, new Rect(0, 0, originalImage.PixelSize.Width, originalImage.PixelSize.Height));
                foreach (var op in operations)
                {
                    op.Draw(context);
                }
            }
            var clipboard = AvaloniaLocator.Current.GetService<IClipboard>();
            if (clipboard != null)
            {
                await clipboard.SetDataObjectAsync(new DataObject { { DataFormats.Bitmap, finalImage } });
            }
            this.Close();
            if (parentWindow != null) parentWindow.Close();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            foreach (var op in operations)
            {
                op.Draw(context);
            }
            if (currentOperation != null) currentOperation.Draw(context);
        }

        private void ApplyMosaic(Point center)
        {
            int mosaicSize = 10;
            var rect = new Rect(center.X - mosaicSize / 2, center.Y - mosaicSize / 2, mosaicSize, mosaicSize);
            rect = rect.Intersect(new Rect(0, 0, originalImage.PixelSize.Width, originalImage.PixelSize.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;

            var mosaicArea = new RenderTargetBitmap(new PixelSize((int)rect.Width, (int)rect.Height));
            using (var srcContext = originalImage.CreateDrawingContext(null))
            using (var dstContext = mosaicArea.CreateDrawingContext(null))
            {
                dstContext.DrawImage(originalImage, rect, new Rect(0, 0, rect.Width, rect.Height));
                dstContext.FillRectangle(new SolidColorBrush(Colors.Gray), new Rect(0, 0, rect.Width, rect.Height));
            }
            using (var context = originalImage.CreateDrawingContext(null))
            {
                context.DrawImage(mosaicArea, new Rect(0, 0, rect.Width, rect.Height), rect);
            }
            canvas.InvalidateVisual();
        }
    }

    public enum Tool { None, Rectangle, Circle, Text, Mosaic }

    public abstract class DrawOperation
    {
        public abstract void Draw(DrawingContext context);
        public virtual Rect GetBounds() => new Rect();
    }

    public class DrawRectangle : DrawOperation
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public Color Color { get; set; }
        public double Thickness { get; set; }

        public override void Draw(DrawingContext context)
        {
            var rect = GetRectangle(StartPoint, EndPoint);
            context.DrawRectangle(new Pen(new SolidColorBrush(Color), Thickness), rect);
        }

        private Rect GetRectangle(Point p1, Point p2)
        {
            double x = Math.Min(p1.X, p2.X);
            double y = Math.Min(p1.Y, p2.Y);
            double width = Math.Abs(p1.X - p2.X);
            double height = Math.Abs(p1.Y - p2.Y);
            return new Rect(x, y, width, height);
        }
    }

    public class DrawCircle : DrawOperation
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public Color Color { get; set; }
        public double Thickness { get; set; }

        public override void Draw(DrawingContext context)
        {
            var rect = GetRectangle(StartPoint, EndPoint);
            context.DrawEllipse(new Pen(new SolidColorBrush(Color), Thickness), rect);
        }

        private Rect GetRectangle(Point p1, Point p2)
        {
            double x = Math.Min(p1.X, p2.X);
            double y = Math.Min(p1.Y, p2.Y);
            double width = Math.Abs(p1.X - p2.X);
            double height = Math.Abs(p1.Y - p2.Y);
            return new Rect(x, y, width, height);
        }
    }

    public class AddText : DrawOperation
    {
        public string Text { get; set; }
        public Point Position { get; set; }
        public Typeface Typeface { get; set; }
        public double FontSize { get; set; }
        public Color Color { get; set; }

        public override void Draw(DrawingContext context)
        {
            var formattedText = new FormattedText(
                Text,
                Typeface,
                FontSize,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                Size.Infinity
            );
            context.DrawText(new SolidColorBrush(Color), Position, formattedText);
        }

        public override Rect GetBounds()
        {
            var formattedText = new FormattedText(
                Text,
                Typeface,
                FontSize,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                Size.Infinity
            );
            return new Rect(Position.X, Position.Y, formattedText.Width, formattedText.Height);
        }
    }
}