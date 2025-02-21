namespace SelectionWin;

public class ShapeButton : Button
{
    public enum ShapeType { Rectangle, Circle, Text, Mosaic, Undo, Finish }
    public ShapeType Shape { get; set; }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        Graphics g = pevent.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        Rectangle rect = new Rectangle(5, 5, this.Width - 10, this.Height - 10);

        // 清空默认背景
        g.Clear(this.BackColor);

        using (Pen pen = new Pen(Color.Black, 2))
        {
            switch (Shape)
            {
                case ShapeType.Rectangle:
                    g.DrawRectangle(pen, rect);
                    break;
                case ShapeType.Circle:
                    g.DrawEllipse(pen, rect);
                    break;
                case ShapeType.Text:
                    g.DrawString("T", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                    break;
                case ShapeType.Mosaic:
                    for (int i = 0; i < rect.Width; i += 5)
                    for (int j = 0; j < rect.Height; j += 5)
                        g.FillRectangle(Brushes.Gray, rect.X + i, rect.Y + j, 3, 3);
                    break;
                case ShapeType.Undo:
                    g.DrawArc(pen, rect, 0, -180); // 绘制半圆箭头
                    break;
                case ShapeType.Finish:
                    g.DrawLine(pen, rect.X + 5, rect.Y + rect.Height / 2, rect.X + rect.Width / 2, rect.Y + rect.Height - 5);
                    g.DrawLine(pen, rect.X + rect.Width / 2, rect.Y + rect.Height - 5, rect.X + rect.Width - 5, rect.Y + 5);
                    break;
            }
        }
    }
}