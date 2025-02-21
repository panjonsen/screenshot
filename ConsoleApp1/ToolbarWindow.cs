using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Windows.Input;
using Avalonia.Media;

namespace SelectionApp
{
    public class ToolbarWindow : Window
    {
        private readonly EditingWindow editingWindow;

        public ToolbarWindow(EditingWindow parent)
        {
            editingWindow = parent;
            this.Width = 180;
            this.Height = 40;
            this.Background = Brushes.White;
            this.CanResize = false;
            this.ShowInTaskbar = false;

            var stackPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(5)
            };

            stackPanel.Children.Add(new Button { Content = "矩形", Width = 25, Height = 30, Command = new Command(() => editingWindow.SetTool(Tool.Rectangle)) });
            stackPanel.Children.Add(new Button { Content = "圆形", Width = 25, Height = 30, Command = new Command(() => editingWindow.SetTool(Tool.Circle)) });
            stackPanel.Children.Add(new Button { Content = "文字", Width = 25, Height = 30, Command = new Command(() => editingWindow.SetTool(Tool.Text)) });
            stackPanel.Children.Add(new Button { Content = "马赛克", Width = 25, Height = 30, Command = new Command(() => editingWindow.SetTool(Tool.Mosaic)) });
            stackPanel.Children.Add(new Button { Content = "撤销", Width = 25, Height = 30, Command = new Command(() => editingWindow.Undo()) });
            stackPanel.Children.Add(new Button { Content = "完成", Width = 25, Height = 30, Command = new Command(async () => await editingWindow.SaveToClipboard()) });

            this.Content = stackPanel;
        }
    }

    public class Command : ICommand
    {
        private readonly Action action;

        public Command(Action action)
        {
            this.action = action;
        }

        public event EventHandler CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => action();
    }
}