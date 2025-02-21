namespace SelectionForm;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        // ApplicationConfiguration.Initialize();
        
        Application.EnableVisualStyles(); // 在 Program.cs 中调用
        Application.Run(new SelectionWin.SelectionForm());
    }
}