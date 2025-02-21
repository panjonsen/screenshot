using System;
using System.Drawing;
using System.Windows.Forms;

public class MagnifierForm : Form
{
    private const int MAGNIFIER_SIZE = 150; // 放大镜窗口大小
    private const int ZOOM_FACTOR = 4; // 放大倍数
    private const int CAPTURE_SIZE = MAGNIFIER_SIZE / ZOOM_FACTOR; // 捕获区域大小
    private Bitmap zoomedImage; // 放大的图像
    private Point mousePosition; // 鼠标位置
    private Color currentColor; // 当前颜色
    private SelectionWin.SelectionForm parentForm; // 父窗体引用

    public MagnifierForm(SelectionWin.SelectionForm parent)
    {
        parentForm = parent;
        this.FormBorderStyle = FormBorderStyle.None;
        this.Size = new Size(MAGNIFIER_SIZE, MAGNIFIER_SIZE + 50); // 额外50像素用于显示信息
        this.BackColor = Color.Black;
        this.TopMost = true;
        this.ShowInTaskbar = false;
        this.DoubleBuffered = true;
        this.Paint += MagnifierForm_Paint;
    }

    public void UpdateMagnifier(Point mousePos)
    {
        mousePosition = mousePos;
        UpdateZoomedImage();
        this.Location = new Point(mousePos.X + 20, mousePos.Y + 20); // 右下方偏移
        this.Invalidate();
    }

    private void UpdateZoomedImage()
    {
        Bitmap screenBitmap = parentForm.GetScreenBitmap();
        if (screenBitmap == null) return;

        // 计算捕获区域，确保不超出屏幕边界
        int captureX = Math.Max(0, Math.Min(mousePosition.X - CAPTURE_SIZE / 2, screenBitmap.Width - CAPTURE_SIZE));
        int captureY = Math.Max(0, Math.Min(mousePosition.Y - CAPTURE_SIZE / 2, screenBitmap.Height - CAPTURE_SIZE));
        Rectangle captureRect = new Rectangle(captureX, captureY, CAPTURE_SIZE, CAPTURE_SIZE);

        // 捕获并放大
        zoomedImage?.Dispose();
        zoomedImage = new Bitmap(MAGNIFIER_SIZE, MAGNIFIER_SIZE);
        using (Graphics g = Graphics.FromImage(zoomedImage))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; // 像素化放大
            g.DrawImage(screenBitmap, new Rectangle(0, 0, MAGNIFIER_SIZE, MAGNIFIER_SIZE), captureRect, GraphicsUnit.Pixel);
        }

        // 获取当前像素颜色
        int colorX = Math.Max(0, Math.Min(mousePosition.X, screenBitmap.Width - 1));
        int colorY = Math.Max(0, Math.Min(mousePosition.Y, screenBitmap.Height - 1));
        currentColor = screenBitmap.GetPixel(colorX, colorY);
    }

    private void MagnifierForm_Paint(object sender, PaintEventArgs e)
    {
        if (zoomedImage != null)
        {
            e.Graphics.DrawImage(zoomedImage, 0, 0);
            // 绘制中心十字线
            using (Pen pen = new Pen(Color.Red, 1))
            {
                e.Graphics.DrawLine(pen, MAGNIFIER_SIZE / 2, 0, MAGNIFIER_SIZE / 2, MAGNIFIER_SIZE);
                e.Graphics.DrawLine(pen, 0, MAGNIFIER_SIZE / 2, MAGNIFIER_SIZE, MAGNIFIER_SIZE / 2);
            }
        }

        // 显示坐标和颜色信息
        string info = $"X: {mousePosition.X}, Y: {mousePosition.Y}\n" +
                      $"RGB: {currentColor.R}, {currentColor.G}, {currentColor.B}\n" +
                      $"Hex: #{currentColor.R:X2}{currentColor.G:X2}{currentColor.B:X2}";
        e.Graphics.DrawString(info, new Font("Arial", 8), Brushes.White, 5, MAGNIFIER_SIZE + 5);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            zoomedImage?.Dispose();
        }
        base.Dispose(disposing);
    }
}