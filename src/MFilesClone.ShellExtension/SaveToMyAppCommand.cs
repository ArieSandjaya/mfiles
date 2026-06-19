using System.Diagnostics;
using System.Runtime.InteropServices;
using MFilesClone.ShellExtension.Interop;

namespace MFilesClone.ShellExtension;

[ComVisible(true)]
[Guid("d3e4a1c2-9b8a-4f7e-8b3a-2c1d5e6f7a8b")]
[ClassInterface(ClassInterfaceType.None)]
public class SaveToMyAppCommand : IExplorerCommand
{
    public void GetTitle(IntPtr psiItemArray, out string ppszName) => ppszName = "Save to MFiles Clone";

    public void GetIcon(IntPtr psiItemArray, out string ppszIcon) => ppszIcon = string.Empty;

    public void GetToolTip(IntPtr psiItemArray, out string ppszInfotip) => ppszInfotip = "Tambahkan file ke MFiles Clone";

    public void GetCanonicalName(out Guid pguidCommandName) => pguidCommandName = typeof(SaveToMyAppCommand).GUID;

    public void GetState(IntPtr psiItemArray, bool fOkToBeSlow, out ExpCmdState pCmdState) => pCmdState = ExpCmdState.Enabled;

    public void Invoke(IntPtr psiItemArray, IntPtr pbc)
    {
        var paths = ShellItemHelper.GetFilePaths(psiItemArray);
        if (paths.Count == 0)
        {
            return;
        }

        // The shell extension DLL is deployed next to MFilesClone.exe in the same install folder.
        var exePath = Path.Combine(AppContext.BaseDirectory, "MFilesClone.exe");
        var arguments = string.Join(' ', paths.Select(p => $"\"{p}\""));

        Process.Start(new ProcessStartInfo(exePath, arguments) { UseShellExecute = true });
    }

    public void GetFlags(out ExpCmdFlags pFlags) => pFlags = ExpCmdFlags.Default;

    public void EnumSubCommands(out IntPtr ppEnum) => ppEnum = IntPtr.Zero;
}
