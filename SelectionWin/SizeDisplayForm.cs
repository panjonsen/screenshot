using System;
using System.Drawing;
using System.Windows.Forms;

public class SizeDisplayForm : Form
{
    private Label sizeLabel; // 用于显示尺寸的标签

    public SizeDisplayForm()
    {
        this.FormBorderStyle = FormBorderStyle.None; // 无边框
        this.BackColor = Color.Black; // 黑色背景
        this.ForeColor = Color.White; // 白色文字
        this.Size = new Size(100, 22); // 默认大小，足够显示如"1920 × 1080"
        this.ShowInTaskbar = false; // 不显示在任务栏
        this.TopMost = true; // 保持在最上层

        sizeLabel = new Label
        {
            AutoSize = true, // 自动调整大小以适应文字
            Location = new Point(4, 4), // 留出边距
            Font = new Font("Arial", 10),
            ForeColor = Color.White
        };
        this.Controls.Add(sizeLabel);
    }

    // 更新尺寸文字和位置
    public void UpdateSize(Rectangle rect)
    {
        string sizeText = $"{rect.Width} × {rect.Height}";
        sizeLabel.Text = sizeText;

        // 调整窗体大小以适应文字
        SizeF textSize = sizeLabel.CreateGraphics().MeasureString(sizeText, sizeLabel.Font);
        this.Size = new Size((int)textSize.Width + 8, (int)textSize.Height + 8); // 增加边距

        // 动态调整位置，紧贴矩形左上角
        int x = rect.X;
        int y = rect.Y - this.Height - 5; // 默认在上方
        if (y < 0) y = rect.Y + 5; // 如果超出顶部，移到矩形内
        if (x + this.Width > Screen.PrimaryScreen.Bounds.Width) // 如果超出右侧
            x = Screen.PrimaryScreen.Bounds.Width - this.Width - 5;
        this.Location = new Point(x, y);
    }

    // 隐藏窗体
    public void HideForm()
    {
        this.Hide();
    }
}