using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Microsoft.VisualBasic;

namespace Selection;

public partial class EditingForm : Form
{

 private Bitmap originalImage;
    private List<DrawOperation> committedOperations = new List<DrawOperation>();
    private DrawOperation currentOperation = null;
    private Tool currentTool = Tool.None;
    private PictureBox pictureBox1;
    private ToolbarForm toolbarForm;
    private Point selectionLocation;
    private AddText selectedText = null;
    private AddText tempText = null; // 临时文字框

    public EditingForm(Bitmap image, Point location)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image), "传入的截图图像不能为空。");
        }
        originalImage = new Bitmap(image);
        selectionLocation = location;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.Size = originalImage.Size;
        this.Location = selectionLocation;
        this.BackColor = Color.Gray;

        Console.WriteLine($"Expected Location: {selectionLocation}");
        Console.WriteLine($"Actual Location Before Show: {this.Location}");

        pictureBox1 = new PictureBox
        {
            Location = Point.Empty,
            Size = originalImage.Size,
            Cursor = Cursors.Default
        };
        this.Controls.Add(pictureBox1);

        pictureBox1.MouseDown += PictureBox1_MouseDown;
        pictureBox1.MouseMove += PictureBox1_MouseMove;
        pictureBox1.MouseUp += PictureBox1_MouseUp;
        pictureBox1.Paint += PictureBox1_Paint;

        toolbarForm = new ToolbarForm(this);
        toolbarForm.Show();

        this.Load += (s, e) =>
        {
            this.Location = selectionLocation;
            Console.WriteLine($"Actual Location After Show: {this.Location}");
            toolbarForm.UpdatePosition();
        };
        this.FormClosed += (s, e) =>
        {
            toolbarForm.Close();
            ((SelectionForm)Application.OpenForms["SelectionForm"]).Enabled = true;
        };
    }

    public void SetTool(Tool tool)
    {
        currentTool = tool;
        pictureBox1.Cursor = tool == Tool.Mosaic ? Cursors.Cross : Cursors.Default;
    }

    public void Undo()
    {
        if (committedOperations.Count > 0)
        {
            committedOperations.RemoveAt(committedOperations.Count - 1);
            pictureBox1.Invalidate();
        }
    }

    public void SaveToClipboard()
    {
        Bitmap finalImage = new Bitmap(originalImage.Width, originalImage.Height);
        using (Graphics g = Graphics.FromImage(finalImage))
        {
            g.DrawImage(originalImage, 0, 0);
            foreach (var op in committedOperations)
            {
                op.Draw(g);
            }
        }
        Clipboard.SetImage(finalImage);
        this.Close(); // 保存后关闭编辑窗体
    }

    private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (currentTool == Tool.Rectangle)
            {
                currentOperation = new DrawRectangle { StartPoint = e.Location, EndPoint = e.Location, Color = Color.Red, Thickness = 2 };
            }
            else if (currentTool == Tool.Circle)
            {
                currentOperation = new DrawCircle { StartPoint = e.Location, EndPoint = e.Location, Color = Color.Red, Thickness = 2 };
            }
            else if (currentTool == Tool.Text)
            {
                selectedText = FindTextAtPoint(e.Location);
                if (selectedText == null)
                {
                    // 创建临时文字框
                    tempText = new AddText
                    {
                        Position = e.Location,
                        Font = new Font("Arial", 12),
                        Color = Color.Red,
                        Text = ""
                    };
                    string text = Interaction.InputBox("请输入文字:", "添加文字", "");
                    if (!string.IsNullOrEmpty(text))
                    {
                        tempText.Text = text;
                        committedOperations.Add(tempText);
                    }
                    tempText = null;
                    pictureBox1.Invalidate();
                }
            }
            else if (currentTool == Tool.Mosaic)
            {
                ApplyMosaic(e.Location);
            }
        }
    }

    private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (currentOperation != null)
            {
                if (currentOperation is DrawRectangle rect) rect.EndPoint = e.Location;
                else if (currentOperation is DrawCircle circle) circle.EndPoint = e.Location;
                pictureBox1.Invalidate();
            }
            else if (currentTool == Tool.Mosaic)
            {
                ApplyMosaic(e.Location);
            }
            else if (selectedText != null)
            {
                selectedText.Position = e.Location;
                pictureBox1.Invalidate();
            }
        }
    }

    private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
    {
        if (currentOperation != null)
        {
            committedOperations.Add(currentOperation);
            currentOperation = null;
            pictureBox1.Invalidate();
        }
        selectedText = null;
    }

    private void PictureBox1_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.DrawImage(originalImage, 0, 0);
        foreach (var op in committedOperations)
        {
            op.Draw(e.Graphics);
            if (op == selectedText)
            {
                Rectangle bounds = GetTextBounds(selectedText);
                using (Pen pen = new Pen(Color.Blue, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    e.Graphics.DrawRectangle(pen, bounds);
                }
            }
        }
        if (currentOperation != null) currentOperation.Draw(e.Graphics);
        if (tempText != null) // 绘制临时文字框
        {
            Rectangle bounds = GetTextBounds(tempText);
            using (Pen pen = new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                e.Graphics.DrawRectangle(pen, bounds);
            }
        }
    }

    private AddText FindTextAtPoint(Point point)
    {
        foreach (var op in committedOperations)
        {
            if (op is AddText text && GetTextBounds(text).Contains(point))
            {
                return text;
            }
        }
        return null;
    }

    private Rectangle GetTextBounds(AddText text)
    {
        using (Graphics g = pictureBox1.CreateGraphics())
        {
            SizeF size = string.IsNullOrEmpty(text.Text) ? new SizeF(100, 20) : g.MeasureString(text.Text, text.Font);
            return new Rectangle(text.Position, Size.Ceiling(size));
        }
    }

    private void ApplyMosaic(Point center)
    {
        int mosaicSize = 10;
        Rectangle rect = new Rectangle(
            center.X - mosaicSize / 2,
            center.Y - mosaicSize / 2,
            mosaicSize,
            mosaicSize
        );

        rect.Intersect(new Rectangle(0, 0, originalImage.Width, originalImage.Height));
        if (rect.Width <= 0 || rect.Height <= 0) return;

        BitmapData data = originalImage.LockBits(rect, ImageLockMode.ReadWrite, originalImage.PixelFormat);
        unsafe
        {
            byte* ptr = (byte*)data.Scan0;
            int bytesPerPixel = Image.GetPixelFormatSize(originalImage.PixelFormat) / 8;
            int stride = data.Stride;

            long r = 0, g = 0, b = 0;
            int pixelCount = 0;
            for (int y = 0; y < rect.Height; y++)
            {
                for (int x = 0; x < rect.Width; x++)
                {
                    int index = y * stride + x * bytesPerPixel;
                    b += ptr[index];
                    g += ptr[index + 1];
                    r += ptr[index + 2];
                    pixelCount++;
                }
            }
            byte avgB = (byte)(b / pixelCount);
            byte avgG = (byte)(g / pixelCount);
            byte avgR = (byte)(r / pixelCount);

            for (int y = 0; y < rect.Height; y++)
            {
                for (int x = 0; x < rect.Width; x++)
                {
                    int index = y * stride + x * bytesPerPixel;
                    ptr[index] = avgB;
                    ptr[index + 1] = avgG;
                    ptr[index + 2] = avgR;
                }
            }
        }
        originalImage.UnlockBits(data);
        pictureBox1.Invalidate(rect);
    }
}
public enum Tool { None, Rectangle, Circle, Text, Mosaic }

public abstract class DrawOperation
{
    public abstract void Draw(Graphics g);
}

public class DrawRectangle : DrawOperation
{
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }
    public Color Color { get; set; }
    public int Thickness { get; set; }

    public override void Draw(Graphics g)
    {
        Rectangle rect = GetRectangle(StartPoint, EndPoint);
        using (Pen pen = new Pen(Color, Thickness))
        {
            g.DrawRectangle(pen, rect);
        }
    }

    private Rectangle GetRectangle(Point p1, Point p2)
    {
        int x = Math.Min(p1.X, p2.X);
        int y = Math.Min(p1.Y, p2.Y);
        int width = Math.Abs(p1.X - p2.X);
        int height = Math.Abs(p1.Y - p2.Y);
        return new Rectangle(x, y, width, height);
    }
}

public class DrawCircle : DrawOperation
{
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }
    public Color Color { get; set; }
    public int Thickness { get; set; }

    public override void Draw(Graphics g)
    {
        Rectangle rect = GetRectangle(StartPoint, EndPoint);
        using (Pen pen = new Pen(Color, Thickness))
        {
            g.DrawEllipse(pen, rect);
        }
    }

    private Rectangle GetRectangle(Point p1, Point p2)
    {
        int x = Math.Min(p1.X, p2.X);
        int y = Math.Min(p1.Y, p2.Y);
        int width = Math.Abs(p1.X - p2.X);
        int height = Math.Abs(p1.Y - p2.Y);
        return new Rectangle(x, y, width, height);
    }
}

public class AddText : DrawOperation
{
    public string Text { get; set; }
    public Point Position { get; set; }
    public Font Font { get; set; }
    public Color Color { get; set; }

    public override void Draw(Graphics g)
    {
        using (Brush brush = new SolidBrush(Color))
        {
            g.DrawString(Text, Font, brush, Position);
        }
    }
}