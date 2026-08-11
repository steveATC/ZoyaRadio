using ZoyaRadio.MediaProbe;

namespace ZoyaRadio.ControlApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new RadioControlForm(RadioRuntimeController.CreateWindowsDefault()));
    }
}
