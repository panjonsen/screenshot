using System;
using System.Drawing;
using System.Windows.Forms;

public class ToolbarForm : Form
{
    private readonly EditingForm editingForm; // 编辑窗体的引用，用于调用其方法
    private Button btnRectangle, btnCircle, btnText, btnMosaic, btnUndo, btnFinish; // 定义工具栏中的按钮

    // 构造函数，初始化工具栏并关联编辑窗体
    public ToolbarForm(EditingForm parent)
    {
        editingForm = parent; // 保存编辑窗体的引用
        InitializeComponents(); // 调用初始化方法
    }

    // 初始化工具栏的控件和布局
    private void InitializeComponents()
    {
        this.FormBorderStyle = FormBorderStyle.None; // 设置窗体无边框，保持简洁
        this.Size = new Size(180, 40); // 设置工具栏大小，宽度180足以容纳6个按钮，高度40
        this.BackColor = Color.White; // 设置背景颜色为白色
        this.TopMost = true; // 设置窗体始终在最上层，避免被其他窗体遮挡
        UpdatePosition(); // 初始化时更新位置，与编辑窗体对齐

        // 创建“矩形”按钮
        btnRectangle = new Button
        {
            Text = "矩形", // 按钮显示文本
            Location = new Point(5, 5), // 位置为(5, 5)，留出边距
            Size = new Size(25, 30), // 大小25x30，适合紧凑布局
            FlatStyle = FlatStyle.Flat // 使用扁平样式，美观简洁
        };
        btnRectangle.FlatAppearance.BorderSize = 0; // 去除按钮边框
        btnRectangle.Click += (s, e) => editingForm.SetTool(Tool.Rectangle); // 点击时切换到矩形工具

        // 创建“圆形”按钮
        btnCircle = new Button
        {
            Text = "圆形",
            Location = new Point(35, 5), // X坐标递增30（25+5间距）
            Size = new Size(25, 30),
            FlatStyle = FlatStyle.Flat
        };
        btnCircle.FlatAppearance.BorderSize = 0;
        btnCircle.Click += (s, e) => editingForm.SetTool(Tool.Circle); // 切换到圆形工具

        // 创建“文字”按钮
        btnText = new Button
        {
            Text = "文字",
            Location = new Point(65, 5),
            Size = new Size(25, 30),
            FlatStyle = FlatStyle.Flat
        };
        btnText.FlatAppearance.BorderSize = 0;
        btnText.Click += (s, e) => editingForm.SetTool(Tool.Text); // 切换到文字工具

        // 创建“马赛克”按钮
        btnMosaic = new Button
        {
            Text = "马赛克",
            Location = new Point(95, 5),
            Size = new Size(25, 30),
            FlatStyle = FlatStyle.Flat
        };
        btnMosaic.FlatAppearance.BorderSize = 0;
        btnMosaic.Click += (s, e) => editingForm.SetTool(Tool.Mosaic); // 切换到马赛克工具

        // 创建“撤销”按钮
        btnUndo = new Button
        {
            Text = "撤销",
            Location = new Point(125, 5),
            Size = new Size(25, 30),
            FlatStyle = FlatStyle.Flat
        };
        btnUndo.FlatAppearance.BorderSize = 0;
        btnUndo.Click += (s, e) => editingForm.Undo(); // 调用撤销方法

        // 创建“完成”按钮
        btnFinish = new Button
        {
            Text = "完成",
            Location = new Point(155, 5),
            Size = new Size(25, 30),
            FlatStyle = FlatStyle.Flat
        };
        btnFinish.FlatAppearance.BorderSize = 0;
        btnFinish.Click += (s, e) => editingForm.SaveToClipboard(); // 调用保存并退出方法

        // 将所有按钮添加到窗体的控件集合
        this.Controls.Add(btnRectangle); // 添加矩形按钮
        this.Controls.Add(btnCircle); // 添加圆形按钮
        this.Controls.Add(btnText); // 添加文字按钮
        this.Controls.Add(btnMosaic); // 添加马赛克按钮
        this.Controls.Add(btnUndo); // 添加撤销按钮
        this.Controls.Add(btnFinish); // 添加完成按钮

        // 调试输出，确认工具栏边界
        Console.WriteLine($"ToolbarForm Bounds: {this.Bounds}");
    }

    // 更新工具栏位置，吸附在编辑窗体右下方
    public void UpdatePosition()
    {
        this.Location = new Point(
            editingForm.Location.X + editingForm.Width + 20, // X坐标：编辑窗体右边缘+20像素间距
            editingForm.Location.Y + editingForm.Height - this.Height // Y坐标：编辑窗体底部对齐工具栏顶部
        );
    }
}