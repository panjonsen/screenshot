# SelectionWin

## 项目概述 (Project Overview)
![image](https://github.com/user-attachments/assets/7a712443-0ec5-4777-9c08-9d383e0ebe56)
![image](https://github.com/user-attachments/assets/40eb78fb-7d42-4939-a8b2-f7a74455c162)
![image](https://github.com/user-attachments/assets/9a49af8e-5da7-4d22-adcb-bd6651eae190)

### 中文
`SelectionWin` 是一个基于 Windows Forms 的屏幕截图和编辑工具，允许用户从屏幕上选择一个区域并对其进行编辑。用户可以绘制矩形、圆形、文字和马赛克，支持动态调整编辑区域大小和撤销操作。编辑完成后，结果可保存到剪贴板。该项目旨在提供一个轻量、直观的截图编辑体验。  另有c++ qt 版本可以阅览我的其他公开仓库

### English
`SelectionWin` is a Windows Forms-based screenshot and editing tool that enables users to select a region from the screen and edit it. Users can draw rectangles, circles, text, and mosaics, with support for dynamically resizing the editing area and undoing operations. Once editing is complete, the result can be saved to the clipboard. This project aims to provide a lightweight and intuitive screenshot editing experience.

## 功能特性 (Features)

### 中文
- **屏幕截图选择**：从屏幕捕获图像并选择编辑区域。
- **多种绘制工具**：
  - 矩形和圆形：支持边界限制，避免超出编辑区域。
  - 文字：多行输入，紧凑行距，支持中文输入法。
  - 马赛克：可调节尺寸（10-50），鼠标光标显示为圆形。
- **动态调整**：通过中点手柄调整选择区域大小。
- **撤销功能**：支持撤销上一次编辑操作。
- **工具栏**：提供直观的操作界面，包含尺寸调节进度条。
- **保存到剪贴板**：编辑结果不包含边框，仅保存内容。

### English
- **Screenshot Selection**: Capture an image from the screen and select an editing region.
- **Multiple Drawing Tools**:
  - Rectangles and Circles: Boundary-limited to stay within the editing area.
  - Text: Multi-line input with compact line spacing, supports Chinese IME.
  - Mosaic: Adjustable size (10-50), with a circular mouse cursor.
- **Dynamic Resizing**: Adjust the selection area size using midpoint handles.
- **Undo Functionality**: Undo the last editing operation.
- **Toolbar**: Intuitive interface with a size adjustment slider for mosaics.
- **Save to Clipboard**: Saves the edited result without borders, only the content.

## 代码结构 (Code Structure)

### 中文
- **`SelectionForm.cs`**：
  - 主窗体，负责屏幕截图捕获和区域选择。
  - 使用 `Bitmap` 从原始截图裁剪选择区域，确保无边框。
  - 通过 `Paint` 事件绘制选择边框，仅作为视觉提示。
- **`EditingForm.cs`**：
  - 编辑窗体，提供绘制和编辑功能。
  - 使用 `PictureBox` 显示编辑内容，外部边框由 `Panel` 绘制。
  - 保存逻辑 (`SaveToClipboard`) 仅包含编辑内容，不含边框。
- **`ToolbarForm.cs`**：
  - 工具栏窗体，提供工具选择和马赛克尺寸调节（`TrackBar`）。
  - 支持动态高度调整，显示/隐藏尺寸调节控件。
- **`CustomTextBox.cs`**：
  - 自定义文本输入控件，支持透明背景和红色边框。
- **`DrawOperation.cs`**：
  - 抽象类及其派生类 (`DrawRectangle`, `DrawCircle`, `AddText`, `DrawMosaic`)，定义绘制操作。

### English
- **`SelectionForm.cs`**:
  - Main form for screenshot capture and region selection.
  - Crops the selected area from the original screenshot using `Bitmap`, ensuring no borders.
  - Draws the selection border via the `Paint` event as a visual cue only.
- **`EditingForm.cs`**:
  - Editing form providing drawing and editing capabilities.
  - Uses `PictureBox` for content display, with an external border drawn by a `Panel`.
  - Save logic (`SaveToClipboard`) includes only the edited content, excluding borders.
- **`ToolbarForm.cs`**:
  - Toolbar form offering tool selection and mosaic size adjustment (`TrackBar`).
  - Supports dynamic height adjustment to show/hide the size control.
- **`CustomTextBox.cs`**:
  - Custom text input control with a transparent background and red border.
- **`DrawOperation.cs`**:
  - Abstract class and its derivatives (`DrawRectangle`, `DrawCircle`, `AddText`, `DrawMosaic`) defining drawing operations.

## 使用说明 (Usage)

### 中文
1. 启动程序，进入全屏截图模式。
2. 鼠标左键拖动选择编辑区域，蓝色边框显示边界。
3. 松开鼠标后，进入编辑模式，工具栏提供绘制选项。
4. 使用工具绘制内容，马赛克尺寸可通过进度条调节。
5. 点击“Finish”保存编辑结果到剪贴板，边框不包含在内。
6. 支持撤销操作和区域调整。

### English
1. Launch the program to enter full-screen screenshot mode.
2. Drag the left mouse button to select an editing area, with a blue border indicating the boundary.
3. Release the mouse to enter editing mode, where the toolbar provides drawing options.
4. Use tools to draw content, with mosaic size adjustable via a slider.
5. Click “Finish” to save the edited result to the clipboard, excluding the border.
6. Supports undoing operations and resizing the area.

## 技术细节 (Technical Details)

### 中文
- **框架**：基于 .NET Framework 的 Windows Forms。
- **语言**：C#。
- **依赖**：无外部库，仅使用标准 Windows Forms 控件。
- **绘制**：使用 `Graphics` 类进行区域选择和内容绘制。
- **边界控制**：矩形和圆形绘制限制在 `selectedRect` 内。
- **行距优化**：文字输入使用 `CustomTextBox`，手动调整行距为 0.8 倍。

### English
- **Framework**: Windows Forms based on .NET Framework.
- **Language**: C#.
- **Dependencies**: No external libraries, uses standard Windows Forms controls.
- **Rendering**: Uses the `Graphics` class for region selection and content drawing.
- **Boundary Control**: Rectangle and circle drawing restricted within `selectedRect`.
- **Line Spacing Optimization**: Text input uses `CustomTextBox` with manually adjusted 0.8x line spacing.

## 如何贡献 (How to Contribute)

### 中文
欢迎提交问题和拉取请求。请遵循以下步骤：
1. Fork 本仓库。
2. 创建你的功能分支（`git checkout -b feature/your-feature`）。
3. 提交你的更改（`git commit -m "Add your feature"`）。
4. 推送到远程分支（`git push origin feature/your-feature`）。
5. 创建拉取请求。

### English
Contributions are welcome via issues and pull requests. Please follow these steps:
1. Fork this repository.
2. Create your feature branch (`git checkout -b feature/your-feature`).
3. Commit your changes (`git commit -m "Add your feature"`).
4. Push to the branch (`git push origin feature/your-feature`).
5. Create a pull request.

## 开源许可 (License)

### 中文
此项目采用 [MIT 许可证](https://opensource.org/licenses/MIT) 开源，详情请查看 `LICENSE` 文件。

### English
This project is open-sourced under the [MIT License](https://opensource.org/licenses/MIT). See the `LICENSE` file for details.
