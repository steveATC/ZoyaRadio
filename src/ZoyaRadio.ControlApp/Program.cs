using ZoyaRadio.MediaProbe;

namespace ZoyaRadio.ControlApp;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 1
            && (string.Equals(
                    args[0],
                    PrivateAlphaSelfTestRunner.PackageArgument,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    args[0],
                    PrivateAlphaSelfTestRunner.NeuralArgument,
                    StringComparison.OrdinalIgnoreCase)))
        {
            bool neural = string.Equals(
                args[0],
                PrivateAlphaSelfTestRunner.NeuralArgument,
                StringComparison.OrdinalIgnoreCase);
            return PrivateAlphaSelfTestRunner.RunAsync(neural, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        ApplicationConfiguration.Initialize();
        Application.Run(
            new RadioControlForm(
                RadioRuntimeController.CreateWindowsDefault(),
                PrivateAlphaSetupController.CreateWindowsDefault()));
        return 0;
    }
}
