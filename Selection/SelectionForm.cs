
using System;
using System.Drawing;
using Selection;


namespace SelectionApp
{
    public class SelectionForm : Form
    {
        private Bitmap screenBitmap; // 存储整个屏幕的快照
        private Point startPoint, endPoint; // 鼠标拖拽的起始点和结束点
        private bool isSelecting = false; // 表示是否正在选择区域
        private Rectangle lastRect = Rectangle.Empty; // 上一次绘制的矩形区域，用于优化重绘
        private EditingForm editingForm; // 编辑窗体的引用，用于后续操作

        // 构造函数，初始化窗体的基本属性
        public SelectionForm()
        {
            this.FormBorderStyle = FormBorderStyle.None; // 设置窗体无边框
            this.WindowState = FormWindowState.Maximized; // 设置窗体全屏显示
            this.DoubleBuffered = true; // 启用双缓冲，减少绘制时的闪烁
            this.Load += SelectionForm_Load; // 绑定窗体加载事件
            this.Paint += SelectionForm_Paint; // 绑定绘制事件
            this.MouseDown += SelectionForm_MouseDown; // 绑定鼠标按下事件
            this.MouseMove += SelectionForm_MouseMove; // 绑定鼠标移动事件
            this.MouseUp += SelectionForm_MouseUp; // 绑定鼠标松开事件
        }

        // 窗体加载时执行，捕获屏幕快照
        private void SelectionForm_Load(object sender, EventArgs e)
        {
            // 创建一个与屏幕大小相同的位图
            screenBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            using (Graphics g = Graphics.FromImage(screenBitmap)) // 使用Graphics对象操作位图
            {
                // 从屏幕(0,0)位置复制整个屏幕内容到screenBitmap
                g.CopyFromScreen(0, 0, 0, 0, screenBitmap.Size);
            }

            this.BackgroundImage = screenBitmap; // 将屏幕快照设置为窗体背景
        }

        // 鼠标按下事件，开始选择区域
        private void SelectionForm_MouseDown(object sender, MouseEventArgs e)
        {
            // 仅当左键按下且没有打开编辑窗体时才开始选择
            if (e.Button == MouseButtons.Left && editingForm == null)
            {
                startPoint = e.Location; // 记录鼠标按下的起始点
                endPoint = e.Location; // 初始化结束点为起始点
                isSelecting = true; // 设置选择状态为true，表示开始选择
            }
        }

        // 鼠标移动事件，动态更新选择区域
        private void SelectionForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting) // 如果正在选择
            {
                endPoint = e.Location; // 更新结束点为当前鼠标位置
                Rectangle newRect = GetSelectedRectangle(startPoint, endPoint); // 计算新的矩形区域
                Rectangle invalidateRect = Rectangle.Union(lastRect, newRect); // 合并旧矩形和新矩形，确定重绘区域
                invalidateRect.Inflate(5, 5); // 扩展重绘区域，避免残留边框
                this.Invalidate(invalidateRect); // 请求重绘指定区域
                lastRect = newRect; // 更新上一次矩形为当前矩形
            }
        }

        // 鼠标松开事件，完成选择并打开编辑窗体
        private void SelectionForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (isSelecting) // 如果正在选择
            {
                isSelecting = false; // 结束选择状态
                Rectangle rect = GetSelectedRectangle(startPoint, endPoint); // 获取最终选择的矩形区域
                if (rect.Width > 0 && rect.Height > 0) // 确保选择区域有效（有宽度和高度）
                {
                    // 创建一个新的位图，用于存储选择区域的图像
                    Bitmap selectedBmp = new Bitmap(rect.Width, rect.Height);
                    using (Graphics g = Graphics.FromImage(selectedBmp))
                    {
                        // 从屏幕的rect位置复制内容到selectedBmp
                        g.CopyFromScreen(rect.Location, Point.Empty, rect.Size);
                    }

                    // 创建并显示编辑窗体，传入选择区域图像和位置
                    editingForm = new EditingForm(selectedBmp, rect.Location, this);
                    editingForm.Show();
                    // 当编辑窗体关闭时，清理引用并重绘
                    editingForm.FormClosed += (s, args) =>
                    {
                        editingForm = null; // 清空编辑窗体引用
                        this.Invalidate(); // 请求重绘整个窗体，恢复背景
                    };
                    ApplyMask(); // 应用遮罩效果，突出编辑区域
                }
            }
        }

        // 绘制事件，绘制选择矩形或遮罩
        private void SelectionForm_Paint(object sender, PaintEventArgs e)
        {
            if (isSelecting) // 如果正在选择
            {
                // 获取当前选择的矩形
                Rectangle rect = GetSelectedRectangle(startPoint, endPoint);
                using (Pen pen = new Pen(Color.LightBlue, 3)) // 创建一个浅蓝色、3像素宽的画笔
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash; // 设置为虚线样式
                    e.Graphics.DrawRectangle(pen, rect); // 绘制虚线矩形
                }
            }

            if (editingForm != null) // 如果编辑窗体已打开
            {
                // 创建一个半透明灰色画刷，用于遮罩效果
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, 128, 128, 128)))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle); // 填充整个窗体为灰色遮罩
                }
            }
        }

        // 计算选择区域的矩形
        private Rectangle GetSelectedRectangle(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X); // 取起始点和结束点X坐标的最小值
            int y = Math.Min(p1.Y, p2.Y); // 取Y坐标的最小值
            int width = Math.Abs(p1.X - p2.X); // 计算宽度（X坐标差的绝对值）
            int height = Math.Abs(p1.Y - p2.Y); // 计算高度（Y坐标差的绝对值）
            return new Rectangle(x, y, width, height); // 返回矩形对象
        }

        // 应用遮罩效果
        private void ApplyMask()
        {
            this.Invalidate(); // 请求重绘窗体，显示遮罩
            this.Enabled = false; // 禁用窗体交互，防止用户操作底层窗体
        }

        // 重写Enabled更改时的行为
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (this.Enabled) // 如果窗体重新启用
            {
                this.Invalidate(); // 请求重绘，移除遮罩
            }
        }
    }
}