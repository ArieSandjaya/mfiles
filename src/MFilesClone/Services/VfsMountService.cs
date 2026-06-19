using DokanNet;
using DokanNet.Logging;

namespace MFilesClone.Services;

public class VfsMountService : IDisposable
{
    private const string MountPoint = "M:\\";

    private readonly VaultFileSystem _fileSystem;
    private Dokan? _dokan;
    private DokanInstance? _instance;

    public VfsMountService(VaultFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public bool IsMounted => _instance is not null;

    public void Mount()
    {
        if (IsMounted)
        {
            return;
        }

        _dokan = new Dokan(new NullLogger());
        _instance = new DokanInstanceBuilder(_dokan)
            .ConfigureOptions(options =>
            {
                options.MountPoint = MountPoint;
            })
            .Build(_fileSystem);
    }

    public void Unmount()
    {
        _instance?.Dispose();
        _instance = null;
        _dokan?.Dispose();
        _dokan = null;
    }

    public void Dispose() => Unmount();
}
