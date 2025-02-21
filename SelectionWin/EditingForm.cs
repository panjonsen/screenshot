using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace SelectionWin;

public class EditingForm : Form
{
private Bitmap originalImage;
    private List<DrawOperation> committedOperations;
    private DrawOperation currentOperation = null;
    private Tool currentTool = Tool.None;
    private PictureBox pictureBox1;
    private ToolbarForm toolbarForm;
    private Rectangle selectedRect;
    private SelectionForm parentForm;
    private SizeDisplayForm sizeDisplayForm;
    private AddText selectedText = null;
    private CustomTextBox textInputBox = null;
    private int resizeHandle = -1;
    private List<Point> mosaicPoints = new List<Point>();
    private readonly Font textFont = new Font("Arial", 12, FontStyle.Regular, GraphicsUnit.Point);
    private readonly Color textColor = Color.Red;
    private int mosaicSize = 10;
    private Panel borderPanel;

    public EditingForm(Bitmap image, Rectangle rect, SelectionForm parent, SizeDisplayForm sizeForm, List<DrawOperation> previousOps)
    {
        originalImage = new Bitmap(image);
        selectedRect = rect;
        parentForm = parent;
        sizeDisplayForm = sizeForm;
        committedOperations = previousOps != null ? new List<DrawOperation>(previousOps) : new List<DrawOperation>();
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.Size = new Size(selectedRect.Width + 6, selectedRect.Height + 6);
        this.Location = new Point(selectedRect.X - 3, selectedRect.Y - 3);
        this.BackColor = Color.Gray;

        borderPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = this.Size,
            BackColor = Color.Transparent
        };
        borderPanel.Paint += BorderPanel_Paint;
        this.Controls.Add(borderPanel);

        pictureBox1 = new PictureBox
        {
            Location = new Point(3, 3),
            Size = selectedRect.Size,
            Cursor = Cursors.Default
        };
        borderPanel.Controls.Add(pictureBox1);

        pictureBox1.MouseDown += PictureBox1_MouseDown;
        pictureBox1.MouseMove += PictureBox1_MouseMove;
        pictureBox1.MouseUp += PictureBox1_MouseUp;
        pictureBox1.MouseClick += PictureBox1_MouseClick;
        pictureBox1.Paint += PictureBox1_Paint;

        toolbarForm = new ToolbarForm(this);
        toolbarForm.Show();

        this.Load += (s, e) =>
        {
            this.Location = new Point(selectedRect.X - 3, selectedRect.Y - 3);
            toolbarForm.UpdatePosition();
            sizeDisplayForm.UpdateSize(selectedRect);
        };
        this.FormClosed += (s, e) => toolbarForm.Close();
    }

    private void BorderPanel_Paint(object sender, PaintEventArgs e)
    {
        using (Pen pen = new Pen(Color.Chartreuse, 3))
        {
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        }
    }

    public void SetTool(Tool tool)
    {
        currentTool = tool;
        if (currentTool == Tool.Mosaic)
        {
            pictureBox1.Cursor = CreateCircleCursor(mosaicSize);
        }
        else
        {
            pictureBox1.Cursor = Cursors.Default;
        }
    }

    public void SetMosaicSize(int size)
    {
        mosaicSize = size;
        if (currentTool == Tool.Mosaic)
        {
            pictureBox1.Cursor = CreateCircleCursor(mosaicSize);
        }
    }

    private Cursor CreateCircleCursor(int size)
    {
        int cursorSize = size + 4;
        using (Bitmap bitmap = new Bitmap(cursorSize, cursorSize))
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            using (Pen pen = new Pen(Color.Black, 2))
            {
                g.DrawEllipse(pen, 2, 2, size, size);
            }
            IntPtr hIcon = bitmap.GetHicon();
            return new Cursor(hIcon);
        }
    }

    public void Undo()
    {
        if (committedOperations.Count > 0)
        {
            committedOperations.RemoveAt(committedOperations.Count - 1);
            pictureBox1.Invalidate();
            Console.WriteLine("Undo operation: Removed last operation");
        }
    }

    public void SaveToClipboard()
    {
        // 创建最终图片，仅包含编辑内容，不包含边框
        Bitmap finalImage = new Bitmap(selectedRect.Width, selectedRect.Height);
        using (Graphics gx = Graphics.FromImage(finalImage))
        {
            gx.TextRenderingHint = TextRenderingHint.AntiAlias;
            gx.Clear(Color.Transparent); // 确保背景透明
            gx.DrawImage(originalImage, 0, 0); // 绘制原始图像
            foreach (var op in committedOperations)
            {
                op.Draw(gx); // 仅绘制操作内容
            }
        }
        Clipboard.SetImage(finalImage);
        Console.WriteLine("Image saved to clipboard without border");
        this.Close();
        if (parentForm != null) parentForm.Close();
        Application.Exit();
    }

    public List<DrawOperation> GetCommittedOperations()
    {
        return committedOperations;
    }

    private void ReturnToSelection(int handleIndex)
    {
        Console.WriteLine("ReturnToSelection called with handle: " + handleIndex);
        parentForm.SetReturnFromEdit(selectedRect, handleIndex);
        toolbarForm.Hide();
        sizeDisplayForm.HideForm();
        this.Close();
    }

    private void ResetToInitialState()
    {
        Console.WriteLine("ResetToInitialState called: Clearing operations and returning to initial state");
        committedOperations.Clear();
        toolbarForm.Hide();
        sizeDisplayForm.HideForm();
        this.Close();
    }

    private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (textInputBox != null && !textInputBox.Bounds.Contains(e.Location))
            {
                EndTextInput();
                return;
            }

            resizeHandle = GetResizeHandle(e.Location);
            if (resizeHandle >= 0)
            {
                Console.WriteLine("Midpoint clicked: Returning to selection with handle " + resizeHandle);
                ReturnToSelection(resizeHandle);
                return;
            }

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
                if (selectedText == null && textInputBox == null)
                {
                    textInputBox = new CustomTextBox
                    {
                        Location = e.Location,
                        Size = new Size(50, 35),
                        Multiline = true,
                        AcceptsReturn = true,
                        ImeMode = ImeMode.On,
                        BackColor = Color.FromArgb(10, 0, 0, 0),
                        ForeColor = textColor,
                        Font = textFont,
                        BorderStyle = BorderStyle.None
                    };
                    textInputBox.TextChanged += TextInputBox_TextChanged;
                    pictureBox1.Controls.Add(textInputBox);
                    textInputBox.Focus();
                }
            }
            else if (currentTool == Tool.Mosaic)
            {
                mosaicPoints.Clear();
                mosaicPoints.Add(e.Location);
                pictureBox1.Invalidate();
            }
        }
    }

    private void PictureBox1_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            Console.WriteLine("Right-click detected in EditingForm: Resetting to initial state");
            ResetToInitialState();
        }
    }

    private void TextInputBox_TextChanged(object sender, EventArgs e)
    {
        if (textInputBox != null)
        {
            using (Graphics gx = pictureBox1.CreateGraphics())
            {
                gx.TextRenderingHint = TextRenderingHint.AntiAlias;
                string[] lines = textInputBox.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                float maxWidth = 0;
                float totalHeight = 0;
                float lineHeight = gx.MeasureString(" ", textFont).Height * 0.8f;
                foreach (var line in lines)
                {
                    SizeF lineSize = gx.MeasureString(line + " ", textFont);
                    maxWidth = Math.Max(maxWidth, lineSize.Width);
                    totalHeight += lineHeight;
                }
                int newWidth = Math.Max(50, (int)maxWidth + 10);
                int newHeight = Math.Max(35, (int)totalHeight + 10);
                newWidth = Math.Min(newWidth, 300);
                textInputBox.Size = new Size(newWidth, newHeight);
            }
            textInputBox.Refresh();
            pictureBox1.Invalidate();
        }
    }

    private void EndTextInput()
    {
        if (textInputBox != null && !string.IsNullOrEmpty(textInputBox.Text))
        {
            AddText newText = new AddText
            {
                Text = textInputBox.Text,
                Position = textInputBox.Location,
                Font = textFont,
                Color = textColor
            };
            committedOperations.Add(newText);
        }
        pictureBox1.Controls.Remove(textInputBox);
        textInputBox.Dispose();
        textInputBox = null;
        pictureBox1.Invalidate();
    }

    private void EditingForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Console.WriteLine("Esc pressed in EditingForm: Exiting program");
            Application.Exit();
            e.Handled = true;
        }
    }

    private void EditingForm_Deactivate(object sender, EventArgs e)
    {
        this.Activate();
        Console.WriteLine("EditingForm deactivated: Keeping visible");
    }

    private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (currentOperation != null)
            {
                if (currentOperation is DrawRectangle rect)
                {
                    rect.EndPoint = new Point(
                        Math.Max(0, Math.Min(e.X, selectedRect.Width - 1)),
                        Math.Max(0, Math.Min(e.Y, selectedRect.Height - 1))
                    );
                    pictureBox1.Invalidate();
                }
                else if (currentOperation is DrawCircle circle)
                {
                    circle.EndPoint = new Point(
                        Math.Max(0, Math.Min(e.X, selectedRect.Width - 1)),
                        Math.Max(0, Math.Min(e.Y, selectedRect.Height - 1))
                    );
                    pictureBox1.Invalidate();
                }
            }
            else if (currentTool == Tool.Mosaic)
            {
                mosaicPoints.Add(e.Location);
                pictureBox1.Invalidate();
            }
            else if (selectedText != null)
            {
                selectedText.Position = e.Location;
                pictureBox1.Invalidate();
            }
        }
        else
        {
            int handle = GetResizeHandle(e.Location);
            pictureBox1.Cursor = handle >= 0 ? Cursors.SizeAll : (currentTool == Tool.Mosaic ? CreateCircleCursor(mosaicSize) : Cursors.Default);
        }
    }

    private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (currentOperation != null)
            {
                committedOperations.Add(currentOperation);
                currentOperation = null;
                pictureBox1.Invalidate();
            }
            else if (currentTool == Tool.Mosaic && mosaicPoints.Count > 0)
            {
                committedOperations.Add(new DrawMosaic { MosaicPoints = new List<Point>(mosaicPoints), Size = mosaicSize });
                mosaicPoints.Clear();
                pictureBox1.Invalidate();
                Console.WriteLine("Mosaic operation committed");
            }
            selectedText = null;
        }
    }

    private void PictureBox1_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
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
        if (mosaicPoints.Count > 0)
        {
            using (Brush brush = new SolidBrush(Color.FromArgb(128, 128, 128)))
            {
                foreach (var point in mosaicPoints)
                {
                    int size = mosaicSize;
                    Rectangle rect = new Rectangle(point.X - size / 2, point.Y - size / 2, size, size);
                    rect.Intersect(new Rectangle(0, 0, originalImage.Width, originalImage.Height));
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                }
            }
        }
        DrawResizeHandles(e.Graphics, new Rectangle(0, 0, selectedRect.Width, selectedRect.Height));
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
        using (Graphics gx = pictureBox1.CreateGraphics())
        {
            gx.TextRenderingHint = TextRenderingHint.AntiAlias;
            SizeF size = gx.MeasureString(text.Text, text.Font);
            return new Rectangle(text.Position, Size.Ceiling(size));
        }
    }

    private Point[] GetResizeHandles(Rectangle rect)
    {
        return new Point[]
        {
            new Point(rect.Left, rect.Top),
            new Point(rect.Right, rect.Top),
            new Point(rect.Right, rect.Bottom),
            new Point(rect.Left, rect.Bottom),
            new Point((rect.Left + rect.Right) / 2, rect.Top),
            new Point(rect.Right, (rect.Top + rect.Bottom) / 2),
            new Point((rect.Left + rect.Right) / 2, rect.Bottom),
            new Point(rect.Left, (rect.Top + rect.Bottom) / 2)
        };
    }

    private void DrawResizeHandles(Graphics g, Rectangle rect)
    {
        Point[] handles = GetResizeHandles(rect);
        using (SolidBrush brush = new SolidBrush(Color.White))
        {
            foreach (Point handle in handles)
            {
                g.FillRectangle(brush, handle.X - 5, handle.Y - 5, 10, 10);
            }
        }
    }

    private int GetResizeHandle(Point mousePoint)
    {
        Point[] handles = GetResizeHandles(new Rectangle(0, 0, selectedRect.Width, selectedRect.Height));
        for (int i = 0; i < handles.Length; i++)
        {
            Rectangle handleRect = new Rectangle(handles[i].X - 8, handles[i].Y - 8, 16, 16);
            if (handleRect.Contains(mousePoint))
            {
                return i;
            }
        }
        return -1;
    }
}

public class CustomTextBox : TextBox
{
    public CustomTextBox()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        this.BackColor = Color.FromArgb(10, 0, 0, 0);
        this.BorderStyle = BorderStyle.None;
        this.Multiline = true;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x20;
            return cp;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using (Pen pen = new Pen(Color.Red, 2))
        {
            Rectangle borderRect = new Rectangle(0, 0, this.Width - 2, this.Height - 2);
            e.Graphics.DrawRectangle(pen, borderRect);
        }
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
        using (Pen pen = new Pen(Color, Thickness))
        {
            Rectangle rect = new Rectangle(
                Math.Min(StartPoint.X, EndPoint.X),
                Math.Min(StartPoint.Y, EndPoint.Y),
                Math.Abs(StartPoint.X - EndPoint.X),
                Math.Abs(StartPoint.Y - EndPoint.Y)
            );
            rect.Intersect(new Rectangle(0, 0, (int)g.VisibleClipBounds.Width, (int)g.VisibleClipBounds.Height));
            if (rect.Width > 0 && rect.Height > 0)
            {
                g.DrawRectangle(pen, rect);
            }
        }
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
        using (Pen pen = new Pen(Color, Thickness))
        {
            Rectangle rect = new Rectangle(
                Math.Min(StartPoint.X, EndPoint.X),
                Math.Min(StartPoint.Y, EndPoint.Y),
                Math.Abs(StartPoint.X - EndPoint.X),
                Math.Abs(StartPoint.Y - EndPoint.Y)
            );
            rect.Intersect(new Rectangle(0, 0, (int)g.VisibleClipBounds.Width, (int)g.VisibleClipBounds.Height));
            if (rect.Width > 0 && rect.Height > 0)
            {
                g.DrawEllipse(pen, rect);
            }
        }
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

public class DrawMosaic : DrawOperation
{
    public List<Point> MosaicPoints { get; set; }
    public int Size { get; set; }

    public override void Draw(Graphics g)
    {
        using (Brush brush = new SolidBrush(Color.Gray))
        {
            foreach (var center in MosaicPoints)
            {
                Rectangle rect = new Rectangle(center.X - Size / 2, center.Y - Size / 2, Size, Size);
                rect.Intersect(new Rectangle(0, 0, (int)g.VisibleClipBounds.Width, (int)g.VisibleClipBounds.Height));
                if (rect.Width > 0 && rect.Height > 0)
                {
                    g.FillRectangle(brush, rect);
                }
            }
        }
    }
}