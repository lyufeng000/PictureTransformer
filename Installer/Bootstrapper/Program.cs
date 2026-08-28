using WixToolset.BootstrapperApplicationApi;

namespace PictureTransformer.SetupUI;

internal static class Program
{
    private static int Main()
    {
        ManagedBootstrapperApplication.Run(new PictureTransformerBootstrapper());
        return 0;
    }
}
