using System;
using System.Drawing;
using System.Windows.Forms;

namespace SelectionWin;

public class ToolbarForm : Form
{
  private readonly EditingForm editingForm;
    private Button btnRectangle, btnCircle, btnText, btnMosaic, btnUndo, btnFinish, btnCancel;
    private TrackBar mosaicSizeTrackBar; // 新增进度条用于调节马赛克尺寸
    private readonly int defaultHeight = 40; // 默认工具栏高度
    private readonly int expandedHeight = 80; // 马赛克模式下扩展高度

    public ToolbarForm(EditingForm parent)
    {
        this.Hide();
        editingForm = parent;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.Size = new Size(215, defaultHeight); // 默认宽度215，高度40
        this.BackColor = Color.White;
        this.TopMost = true;
        UpdatePosition();

        // 初始化按钮
        btnRectangle = new ShapeButton
            { Shape = ShapeButton.ShapeType.Rectangle, Location = new Point(5, 5), Size = new Size(30, 30) };
        btnCircle = new ShapeButton
            { Shape = ShapeButton.ShapeType.Circle, Location = new Point(40, 5), Size = new Size(30, 30) };
        btnText = new ShapeButton
            { Shape = ShapeButton.ShapeType.Text, Location = new Point(75, 5), Size = new Size(30, 30) };
        btnMosaic = new ShapeButton
            { Shape = ShapeButton.ShapeType.Mosaic, Location = new Point(110, 5), Size = new Size(30, 30) };
        btnUndo = new ShapeButton
            { Shape = ShapeButton.ShapeType.Undo, Location = new Point(145, 5), Size = new Size(30, 30) };
        btnFinish = new ShapeButton
            { Shape = ShapeButton.ShapeType.Finish, Location = new Point(180, 5), Size = new Size(30, 30) };

        // 新增马赛克尺寸调节进度条
        mosaicSizeTrackBar = new TrackBar
        {
            Location = new Point(110, 40), // 位于马赛克按钮下方
            Size = new Size(100, 30),
            Minimum = 10, // 最小值 10
            Maximum = 50, // 最大值 50
            Value = 10, // 默认值 10
            TickFrequency = 10,
            Visible = false // 默认隐藏
        };
        mosaicSizeTrackBar.ValueChanged += MosaicSizeTrackBar_ValueChanged;

        // 美化样式
        foreach (var btn in new[] { btnRectangle, btnCircle, btnText, btnMosaic, btnUndo, btnFinish })
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.FromArgb(240, 240, 240);
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(200, 200, 200);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(240, 240, 240);
        }

        // 事件绑定
        btnRectangle.Click += (s, e) => { editingForm.SetTool(Tool.Rectangle); ResetToolbar(); };
        btnCircle.Click += (s, e) => { editingForm.SetTool(Tool.Circle); ResetToolbar(); };
        btnText.Click += (s, e) => { editingForm.SetTool(Tool.Text); ResetToolbar(); };
        btnMosaic.Click += (s, e) => { editingForm.SetTool(Tool.Mosaic); ExpandToolbar(); };
        btnUndo.Click += (s, e) => editingForm.Undo();
        btnFinish.Click += (s, e) => editingForm.SaveToClipboard();

        // 添加到控件
        this.Controls.Add(btnRectangle);
        this.Controls.Add(btnCircle);
        this.Controls.Add(btnText);
        this.Controls.Add(btnMosaic);
        this.Controls.Add(btnUndo);
        this.Controls.Add(btnFinish);
        this.Controls.Add(mosaicSizeTrackBar);
    }

    private void MosaicSizeTrackBar_ValueChanged(object sender, EventArgs e)
    {
        int size = mosaicSizeTrackBar.Value;
        editingForm.SetMosaicSize(size); // 更新马赛克尺寸
        Console.WriteLine("Mosaic Size Changed: " + size);
    }

    private void ExpandToolbar()
    {
        this.Size = new Size(this.Width, expandedHeight); // 扩展高度
        mosaicSizeTrackBar.Visible = true;
        UpdatePosition();
    }

    private void ResetToolbar()
    {
        this.Size = new Size(this.Width, defaultHeight); // 恢复默认高度
        mosaicSizeTrackBar.Visible = false;
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        this.Show();
        this.Location = new Point(
            editingForm.Location.X + editingForm.Width + 20,
            editingForm.Location.Y + editingForm.Height - this.Height
        );
    }

}