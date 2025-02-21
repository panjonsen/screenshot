namespace SelectionWin;

public partial class SelectionForm : Form
{
private Bitmap screenBitmap;
    private Point startPoint, endPoint;
    private bool isSelecting = false;
    private Rectangle lastRect = Rectangle.Empty;
    private EditingForm editingForm;
    private SizeDisplayForm sizeDisplayForm;
    private MagnifierForm magnifierForm;
    private List<DrawOperation> previousOperations = new List<DrawOperation>();
    private bool isReturningFromEdit = false;
    private Rectangle initialRect;
    private int selectedHandle = -1;

    public SelectionForm()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.DoubleBuffered = true;
        this.Load += SelectionForm_Load;
        this.Paint += SelectionForm_Paint;
        this.MouseDown += SelectionForm_MouseDown;
        this.MouseMove += SelectionForm_MouseMove;
        this.MouseUp += SelectionForm_MouseUp;
        this.MouseClick += SelectionForm_MouseClick;
        this.KeyDown += SelectionForm_KeyDown;

        sizeDisplayForm = new SizeDisplayForm();
        magnifierForm = new MagnifierForm(this);
        this.KeyPreview = true;
    }

    private void SelectionForm_Load(object sender, EventArgs e)
    {
        screenBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
        using (Graphics g = Graphics.FromImage(screenBitmap))
        {
            g.CopyFromScreen(0, 0, 0, 0, screenBitmap.Size);
        }
        this.BackgroundImage = screenBitmap;
        magnifierForm.Show();
        this.Invalidate();
        Console.WriteLine("SelectionForm loaded: Initial invalidate called");
    }

    private void SelectionForm_MouseDown(object sender, MouseEventArgs e)
    {
        if (editingForm != null) return;

        if (e.Button == MouseButtons.Left)
        {
            Console.WriteLine("MouseDown triggered: Starting selection");
            startPoint = e.Location;
            endPoint = e.Location;
            isSelecting = true;
            sizeDisplayForm.Show();
            magnifierForm.Hide();
            this.Invalidate();
        }
    }

    private void SelectionForm_MouseMove(object sender, MouseEventArgs e)
    {
        if (!isSelecting)
        {
            magnifierForm.UpdateMagnifier(e.Location);
        }

        if (isSelecting)
        {
            switch (selectedHandle)
            {
                case 4: // 上中点：固定X范围，只允许Y变化
                    endPoint = new Point(initialRect.Right, e.Y);
                    break;
                case 5: // 右中点：固定Y范围，只允许X变化
                    endPoint = new Point(e.X, initialRect.Bottom);
                    break;
                case 6: // 下中点：固定X范围，只允许Y变化
                    endPoint = new Point(initialRect.Right, e.Y);
                    break;
                case 7: // 左中点：固定Y范围，只允许X变化
                    endPoint = new Point(e.X, initialRect.Bottom);
                    break;
                case -1: // 右键触发：允许自由调整
                    endPoint = e.Location;
                    break;
                default:
                    endPoint = e.Location;
                    break;
            }

            Rectangle newRect = GetSelectedRectangle(startPoint, endPoint);
            Rectangle invalidateRect = Rectangle.Union(lastRect, newRect);
            invalidateRect.Inflate(10, 10);
            this.Invalidate(invalidateRect);
            lastRect = newRect;

            sizeDisplayForm.UpdateSize(newRect);
        }
    }

    private void SelectionForm_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && isSelecting)
        {
            isSelecting = false;
            Rectangle rect = GetSelectedRectangle(startPoint, endPoint);
            if (rect.Width > 0 && rect.Height > 0)
            {
                Console.WriteLine("MouseUp triggered: Creating EditingForm");
                Bitmap selectedBmp = new Bitmap(rect.Width, rect.Height);
                using (Graphics g = Graphics.FromImage(selectedBmp))
                {
                    g.CopyFromScreen(rect.Location, Point.Empty, rect.Size);
                }
                editingForm = new EditingForm(selectedBmp, rect, this, sizeDisplayForm, previousOperations);
                editingForm.Show();
                editingForm.FormClosed += EditingForm_FormClosed;
                ApplyMask();
            }
            else
            {
                sizeDisplayForm.HideForm();
                magnifierForm.Show();
            }
            selectedHandle = -1;
            this.Invalidate();
        }
    }

    private void SelectionForm_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && !isSelecting && editingForm == null)
        {
            Console.WriteLine("Right-click detected in SelectionForm: Exiting program");
            Application.Exit();
        }
    }

    private void SelectionForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Console.WriteLine("Esc pressed in SelectionForm: Exiting program");
            Application.Exit();
            e.Handled = true;
        }
    }

    private void EditingForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        Console.WriteLine("EditingForm closed");
        previousOperations = editingForm.GetCommittedOperations();
        editingForm = null;
        this.Enabled = true;
        sizeDisplayForm.HideForm();

        if (isReturningFromEdit)
        {
            Console.WriteLine("Returning from edit: Starting selection with initial rect " + initialRect);
            StartSelectionFromEdit();
            // 保持放大镜隐藏，直到选择完成或取消
            magnifierForm.Hide();
            isReturningFromEdit = false;
        }
        else
        {
            Console.WriteLine("Not returning from edit: Showing magnifier and resetting state");
            magnifierForm.Show();
            isSelecting = false;
            isReturningFromEdit = false;
        }
        this.Invalidate();
    }

    private void SelectionForm_Paint(object sender, PaintEventArgs e)
    {
        if (isSelecting)
        {
            Rectangle rect = GetSelectedRectangle(startPoint, endPoint);
            using (Pen pen = new Pen(Color.LightBlue, 3))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                e.Graphics.DrawRectangle(pen, rect);
            }
        }
        else if (editingForm != null)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, 128, 128, 128)))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }
        else
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, 128, 128, 128)))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }
    }

    private Rectangle GetSelectedRectangle(Point p1, Point p2)
    {
        int x = Math.Min(p1.X, p2.X);
        int y = Math.Min(p1.Y, p2.Y);
        int width = Math.Abs(p1.X - p2.X);
        int height = Math.Abs(p1.Y - p2.Y);
        return new Rectangle(x, y, width, height);
    }

    private void ApplyMask()
    {
        this.Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (this.Enabled)
        {
            this.Invalidate();
            sizeDisplayForm.HideForm();
        }
    }

    public Bitmap GetScreenBitmap()
    {
        return screenBitmap;
    }

    public void SetReturnFromEdit(Rectangle rect, int handleIndex)
    {
        Console.WriteLine("SetReturnFromEdit called with rect: " + rect + ", handle: " + handleIndex);
        isReturningFromEdit = true;
        initialRect = rect;
        selectedHandle = handleIndex;
    }

    private void StartSelectionFromEdit()
    {
        switch (selectedHandle)
        {
            case 0: // 左上角
                startPoint = new Point(initialRect.Right, initialRect.Bottom);
                endPoint = initialRect.Location;
                break;
            case 1: // 右上角
                startPoint = new Point(initialRect.X, initialRect.Bottom);
                endPoint = new Point(initialRect.Right, initialRect.Y);
                break;
            case 2: // 右下角
                startPoint = initialRect.Location;
                endPoint = new Point(initialRect.Right, initialRect.Bottom);
                break;
            case 3: // 左下角
                startPoint = new Point(initialRect.Right, initialRect.Y);
                endPoint = new Point(initialRect.X, initialRect.Bottom);
                break;
            case 4: // 上中点：固定左右边界，允许向上扩展
                startPoint = new Point(initialRect.X, initialRect.Bottom);
                endPoint = new Point(initialRect.Right, initialRect.Y);
                break;
            case 5: // 右中点：固定上下边界，允许向右扩展
                startPoint = new Point(initialRect.X, initialRect.Y);
                endPoint = new Point(initialRect.Right, initialRect.Bottom);
                break;
            case 6: // 下中点：固定左右边界，允许向下扩展
                startPoint = initialRect.Location;
                endPoint = new Point(initialRect.Right, initialRect.Bottom);
                break;
            case 7: // 左中点：固定上下边界，允许向左扩展
                startPoint = new Point(initialRect.Right, initialRect.Y);
                endPoint = new Point(initialRect.X, initialRect.Bottom);
                break;
            case -1: // 右键触发：允许自由调整
                startPoint = initialRect.Location;
                endPoint = new Point(initialRect.Right, initialRect.Bottom);
                break;
            default:
                startPoint = initialRect.Location;
                endPoint = new Point(initialRect.Right, initialRect.Bottom);
                break;
        }

        isSelecting = true;
        sizeDisplayForm.Show();
        sizeDisplayForm.UpdateSize(GetSelectedRectangle(startPoint, endPoint));
        this.Invalidate();
        Console.WriteLine("StartSelectionFromEdit: Selection started with startPoint " + startPoint + ", endPoint " + endPoint);
    }
}