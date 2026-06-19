using System.Runtime.InteropServices;

namespace MFilesClone.ShellExtension.Interop;

[Flags]
internal enum ExpCmdFlags
{
    Default = 0,
    HasSubCommands = 0x1,
    HasSplitButton = 0x2,
    HidesLabel = 0x4,
    IsSeparator = 0x8,
    HasLuaShield = 0x10,
    SeparateFolder = 0x20,
}

internal enum ExpCmdState
{
    Enabled = 0,
    Disabled = 0x1,
    Hidden = 0x2,
    Checkbox = 0x4,
    Checked = 0x8,
    RadioCheck = 0x10,
}

// Vtable order (after IUnknown) must match shobjidl_core.h's IExplorerCommand exactly,
// since native Explorer calls into this managed object through a COM-callable wrapper.
[ComImport]
[Guid("a08ce4d0-fa25-44ab-b57c-c7b1c323e0b9")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IExplorerCommand
{
    void GetTitle(IntPtr psiItemArray, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

    void GetIcon(IntPtr psiItemArray, [MarshalAs(UnmanagedType.LPWStr)] out string ppszIcon);

    void GetToolTip(IntPtr psiItemArray, [MarshalAs(UnmanagedType.LPWStr)] out string ppszInfotip);

    void GetCanonicalName(out Guid pguidCommandName);

    void GetState(IntPtr psiItemArray, [MarshalAs(UnmanagedType.Bool)] bool fOkToBeSlow, out ExpCmdState pCmdState);

    void Invoke(IntPtr psiItemArray, IntPtr pbc);

    void GetFlags(out ExpCmdFlags pFlags);

    void EnumSubCommands(out IntPtr ppEnum);
}
