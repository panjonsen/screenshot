using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

public class EditingForm : Form
{
    private Bitmap originalImage; // 存储原始截图
    private List<DrawOperation> committedOperations = new List<DrawOperation>(); // 已完成的绘图操作列表
    private DrawOperation currentOperation = null; // 当前正在进行的操作
    private Tool currentTool = Tool.None; // 当前选中的工具
    private PictureBox pictureBox1; // 显示和编辑图像的控件
    private ToolbarForm toolbarForm; // 工具栏窗体引用
    private Point selectionLocation; // 选择区域在屏幕上的位置
    private AddText selectedText = null; // 当前选中的文字对象
    private TextBox textInputBox = null; // 动态添加的文字输入框
    private SelectionForm.SelectionForm parentForm; // 父窗体引用，用于关闭时同步

    // 构造函数，初始化编辑窗体
    public EditingForm(Bitmap image, Point location, SelectionForm.SelectionForm parent)
    {
        originalImage = new Bitmap(image); // 创建原始图像的副本，避免修改原始数据
        selectionLocation = location; // 记录选择区域的屏幕位置
        parentForm = parent; // 保存父窗体引用
        InitializeComponents(); // 初始化控件和事件
    }

    // 初始化窗体和控件
    private void InitializeComponents()
    {
        this.FormBorderStyle = FormBorderStyle.None; // 设置窗体无边框
        this.Size = originalImage.Size; // 设置窗体大小与图像一致
        this.Location = selectionLocation; // 设置窗体位置为选择区域位置
        this.BackColor = Color.Gray; // 设置背景颜色为灰色，便于调试边界

        // 创建PictureBox用于显示和编辑图像
        pictureBox1 = new PictureBox
        {
            Location = Point.Empty, // 位置为(0,0)，填充整个窗体
            Size = originalImage.Size, // 大小与图像一致
            Cursor = Cursors.Default // 默认鼠标光标
        };
        this.Controls.Add(pictureBox1); // 将PictureBox添加到窗体控件集合

        // 绑定PictureBox的事件
        pictureBox1.MouseDown += PictureBox1_MouseDown; // 鼠标按下事件
        pictureBox1.MouseMove += PictureBox1_MouseMove; // 鼠标移动事件
        pictureBox1.MouseUp += PictureBox1_MouseUp; // 鼠标松开事件
        pictureBox1.Paint += PictureBox1_Paint; // 绘制事件

        // 创建并显示工具栏窗体
        toolbarForm = new ToolbarForm(this); // 实例化工具栏，传入当前编辑窗体引用
        toolbarForm.Show(); // 显示工具栏

        // 窗体加载事件，确保位置正确
        this.Load += (s, e) =>
        {
            this.Location = selectionLocation; // 再次确认窗体位置
            Console.WriteLine($"Actual Location After Show: {this.Location}"); // 调试输出实际位置
            toolbarForm.UpdatePosition(); // 更新工具栏位置
        };

        // 窗体关闭事件，清理相关资源
        this.FormClosed += (s, e) =>
        {
            toolbarForm.Close(); // 关闭工具栏窗体
            // 当编辑窗体关闭时，不再恢复父窗体状态，而是由SaveToClipboard控制程序退出
        };
    }

    // 设置当前工具
    public void SetTool(Tool tool)
    {
        currentTool = tool; // 更新当前工具
        // 根据工具类型更改鼠标光标样式
        pictureBox1.Cursor = tool == Tool.Mosaic ? Cursors.Cross : Cursors.Default;
    }

    // 撤销上一步操作
    public void Undo()
    {
        if (committedOperations.Count > 0) // 检查是否有操作可撤销
        {
            committedOperations.RemoveAt(committedOperations.Count - 1); // 移除最后的操作
            pictureBox1.Invalidate(); // 请求重绘PictureBox，更新显示
        }
    }

    // 将编辑后的图像保存到剪贴板并退出程序
    public void SaveToClipboard()
    {
        Bitmap finalImage = new Bitmap(originalImage.Width, originalImage.Height); // 创建最终图像的位图
        using (Graphics g = Graphics.FromImage(finalImage))
        {
            g.DrawImage(originalImage, 0, 0); // 绘制原始图像
            foreach (var op in committedOperations) // 绘制所有已完成的操作
            {
                op.Draw(g);
            }
        }
        Clipboard.SetImage(finalImage); // 将最终图像保存到剪贴板
        this.Close(); // 关闭当前编辑窗体
        if (parentForm != null) parentForm.Close(); // 关闭父窗体（SelectionForm）
        Application.Exit(); // 退出整个应用程序，确保程序结束
    }

    // 鼠标按下事件，开始绘制或操作
    private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) // 只处理左键点击
        {
            if (currentTool == Tool.Rectangle) // 矩形工具
            {
                // 创建新的矩形操作，初始化起始点和结束点
                currentOperation = new DrawRectangle { StartPoint = e.Location, EndPoint = e.Location, Color = Color.Red, Thickness = 2 };
            }
            else if (currentTool == Tool.Circle) // 圆形工具
            {
                // 创建新的圆形操作，初始化起始点和结束点
                currentOperation = new DrawCircle { StartPoint = e.Location, EndPoint = e.Location, Color = Color.Red, Thickness = 2 };
            }
            else if (currentTool == Tool.Text) // 文字工具
            {
                selectedText = FindTextAtPoint(e.Location); // 检查是否点击了已有文字
                if (selectedText == null && textInputBox == null) // 如果没有选中文字且没有输入框
                {
                    // 创建动态文字输入框
                    textInputBox = new TextBox
                    {
                        Location = e.Location, // 输入框位置为鼠标点击处
                        Size = new Size(100, 20), // 默认大小
                        BorderStyle = BorderStyle.FixedSingle // 单线边框
                    };
                    pictureBox1.Controls.Add(textInputBox); // 添加到PictureBox控件集合
                    textInputBox.Focus(); // 聚焦输入框，准备输入
                    textInputBox.KeyDown += TextInputBox_KeyDown; // 绑定键盘事件
                }
            }
            else if (currentTool == Tool.Mosaic) // 马赛克工具
            {
                ApplyMosaic(e.Location); // 在点击位置应用马赛克
            }
        }
    }

    // 处理文字输入框的键盘事件
    private void TextInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter) // 用户按下Enter键
        {
            if (!string.IsNullOrEmpty(textInputBox.Text)) // 如果输入框有内容
            {
                // 创建新的文字操作对象
                AddText newText = new AddText
                {
                    Text = textInputBox.Text, // 设置文字内容
                    Position = textInputBox.Location, // 设置文字位置
                    Font = new Font("Arial", 12), // 设置字体
                    Color = Color.Red // 设置颜色
                };
                committedOperations.Add(newText); // 添加到已完成操作列表
            }
            pictureBox1.Controls.Remove(textInputBox); // 从PictureBox移除输入框
            textInputBox.Dispose(); // 释放输入框资源
            textInputBox = null; // 清空输入框引用
            pictureBox1.Invalidate(); // 请求重绘，显示新文字
            e.Handled = true; // 标记事件已处理，防止冒泡
        }
        else if (e.KeyCode == Keys.Escape) // 用户按下Esc键
        {
            pictureBox1.Controls.Remove(textInputBox); // 移除输入框
            textInputBox.Dispose(); // 释放资源
            textInputBox = null; // 清空引用
            pictureBox1.Invalidate(); // 请求重绘
            e.Handled = true; // 标记事件已处理
        }
    }

    // 鼠标移动事件，动态更新绘制
    private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) // 只在左键按住时处理
        {
            if (currentOperation != null) // 如果有当前操作
            {
                if (currentOperation is DrawRectangle rect) rect.EndPoint = e.Location; // 更新矩形结束点
                else if (currentOperation is DrawCircle circle) circle.EndPoint = e.Location; // 更新圆形结束点
                pictureBox1.Invalidate(); // 请求重绘，显示动态效果
            }
            else if (currentTool == Tool.Mosaic) // 马赛克工具
            {
                ApplyMosaic(e.Location); // 在鼠标移动路径上应用马赛克
            }
            else if (selectedText != null) // 如果有选中的文字
            {
                selectedText.Position = e.Location; // 更新文字位置（拖动文字）
                pictureBox1.Invalidate(); // 请求重绘
            }
        }
    }

    // 鼠标松开事件，完成绘制
    private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
    {
        if (currentOperation != null) // 如果有当前操作
        {
            committedOperations.Add(currentOperation); // 将操作添加到已完成列表
            currentOperation = null; // 清空当前操作
            pictureBox1.Invalidate(); // 请求重绘
        }
        selectedText = null; // 清空选中文字状态
    }

    // 绘制事件，渲染图像和操作
    private void PictureBox1_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.DrawImage(originalImage, 0, 0); // 绘制原始图像作为背景
        foreach (var op in committedOperations) // 遍历所有已完成的操作
        {
            op.Draw(e.Graphics); // 绘制每个操作
            if (op == selectedText) // 如果是选中文字
            {
                Rectangle bounds = GetTextBounds(selectedText); // 获取文字边界
                using (Pen pen = new Pen(Color.Blue, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    e.Graphics.DrawRectangle(pen, bounds); // 绘制蓝色虚线框，表示选中
                }
            }
        }
        if (currentOperation != null) currentOperation.Draw(e.Graphics); // 绘制当前正在进行的操作
    }

    // 查找鼠标点击位置是否有文字
    private AddText FindTextAtPoint(Point point)
    {
        foreach (var op in committedOperations) // 遍历所有操作
        {
            if (op is AddText text && GetTextBounds(text).Contains(point)) // 如果是文字且点击点在边界内
            {
                return text; // 返回该文字对象
            }
        }
        return null; // 未找到，返回null
    }

    // 计算文字的边界矩形
    private Rectangle GetTextBounds(AddText text)
    {
        using (Graphics g = pictureBox1.CreateGraphics()) // 创建Graphics对象用于测量
        {
            SizeF size = g.MeasureString(text.Text, text.Font); // 测量文字的宽度和高度
            return new Rectangle(text.Position, Size.Ceiling(size)); // 返回边界矩形
        }
    }

    // 应用马赛克效果
    private void ApplyMosaic(Point center)
    {
        int mosaicSize = 10; // 定义马赛克块的大小（10x10像素）
        // 计算马赛克区域，以鼠标为中心
        Rectangle rect = new Rectangle(
            center.X - mosaicSize / 2,
            center.Y - mosaicSize / 2,
            mosaicSize,
            mosaicSize
        );
        // 确保马赛克区域不超出图像边界
        rect.Intersect(new Rectangle(0, 0, originalImage.Width, originalImage.Height));
        if (rect.Width <= 0 || rect.Height <= 0) return; // 如果区域无效，直接返回

        // 锁定图像数据以进行像素操作
        BitmapData data = originalImage.LockBits(rect, ImageLockMode.ReadWrite, originalImage.PixelFormat);
        unsafe
        {
            byte* ptr = (byte*)data.Scan0; // 获取像素数据的起始指针
            int bytesPerPixel = Image.GetPixelFormatSize(originalImage.PixelFormat) / 8; // 每个像素的字节数（通常为4：RGBA）
            int stride = data.Stride; // 每行的字节数（包括填充字节）

            // 计算区域内所有像素的平均颜色
            long r = 0, g = 0, b = 0; // 累加RGB分量
            int pixelCount = 0; // 像素计数
            for (int y = 0; y < rect.Height; y++) // 遍历高度（修复：使用rect.Height而不是rectHeight）
            {
                for (int x = 0; x < rect.Width; x++) // 遍历宽度
                {
                    int index = y * stride + x * bytesPerPixel; // 计算当前像素的索引
                    b += ptr[index]; // 累加蓝色分量
                    g += ptr[index + 1]; // 累加绿色分量
                    r += ptr[index + 2]; // 累加红色分量
                    pixelCount++; // 增加像素计数
                }
            }
            byte avgB = (byte)(b / pixelCount); // 计算平均蓝色
            byte avgG = (byte)(g / pixelCount); // 计算平均绿色
            byte avgR = (byte)(r / pixelCount); // 计算平均红色

            // 将区域内所有像素设置为平均颜色
            for (int y = 0; y < rect.Height; y++) // 遍历高度（修复：使用rect.Height）
            {
                for (int x = 0; x < rect.Width; x++) // 遍历宽度
                {
                    int index = y * stride + x * bytesPerPixel; // 计算像素索引
                    ptr[index] = avgB; // 设置蓝色
                    ptr[index + 1] = avgG; // 设置绿色
                    ptr[index + 2] = avgR; // 设置红色
                }
            }
        }
        originalImage.UnlockBits(data); // 解锁图像数据
        pictureBox1.Invalidate(rect); // 请求重绘马赛克区域
    }
}

// 定义工具枚举，表示不同的编辑工具
public enum Tool
{
    None, // 无工具
    Rectangle, // 矩形工具
    Circle, // 圆形工具
    Text, // 文字工具
    Mosaic // 马赛克工具
}

// 绘图操作的抽象基类，所有具体操作必须实现Draw方法
public abstract class DrawOperation
{
    public abstract void Draw(Graphics g); // 抽象方法，要求子类实现绘制逻辑
}

// 绘制矩形操作类
public class DrawRectangle : DrawOperation
{
    public Point StartPoint { get; set; } // 矩形起始点
    public Point EndPoint { get; set; } // 矩形结束点
    public Color Color { get; set; } // 矩形边框颜色
    public int Thickness { get; set; } // 矩形边框粗细

    public override void Draw(Graphics g)
    {
        using (Pen pen = new Pen(Color, Thickness)) // 创建画笔
        {
            // 计算矩形的位置和大小
            Rectangle rect = new Rectangle(
                Math.Min(StartPoint.X, EndPoint.X),
                Math.Min(StartPoint.Y, EndPoint.Y),
                Math.Abs(StartPoint.X - EndPoint.X),
                Math.Abs(StartPoint.Y - EndPoint.Y)
            );
            g.DrawRectangle(pen, rect); // 绘制矩形
        }
    }
}

// 绘制圆形操作类
public class DrawCircle : DrawOperation
{
    public Point StartPoint { get; set; } // 圆形起始点
    public Point EndPoint { get; set; } // 圆形结束点
    public Color Color { get; set; } // 圆形边框颜色
    public int Thickness { get; set; } // 圆形边框粗细

    public override void Draw(Graphics g)
    {
        using (Pen pen = new Pen(Color, Thickness)) // 创建画笔
        {
            // 计算圆形的外接矩形
            Rectangle rect = new Rectangle(
                Math.Min(StartPoint.X, EndPoint.X),
                Math.Min(StartPoint.Y, EndPoint.Y),
                Math.Abs(StartPoint.X - EndPoint.X),
                Math.Abs(StartPoint.Y - EndPoint.Y)
            );
            g.DrawEllipse(pen, rect); // 绘制圆形（椭圆）
        }
    }
}

// 添加文字操作类
public class AddText : DrawOperation
{
    public string Text { get; set; } // 文字内容
    public Point Position { get; set; } // 文字位置
    public Font Font { get; set; } // 文字字体
    public Color Color { get; set; } // 文字颜色

    public override void Draw(Graphics g)
    {
        using (Brush brush = new SolidBrush(Color)) // 创建画刷
        {
            g.DrawString(Text, Font, brush, Position); // 在指定位置绘制文字
        }
    }
}