using System.Runtime.InteropServices;

namespace MFilesClone.ShellExtension.Interop;

// Vtable orders below (after IUnknown) must match shobjidl_core.h exactly. Unused
// leading members are still declared so the CLR resolves the calls we do make to
// the correct vtable slot.

[ComImport]
[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

    void GetParent(out IShellItem ppsi);

    void GetDisplayName(int sigdnName, out IntPtr ppszName);

    void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

    void Compare(IShellItem psi, uint hint, out int piOrder);
}

[ComImport]
[Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItemArray
{
    void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

    void GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);

    void GetPropertyDescriptionList(IntPtr keyType, ref Guid riid, out IntPtr ppv);

    void GetAttributes(int attribFlags, uint sfgaoMask, out uint psfgaoAttribs);

    void GetCount(out uint pdwNumItems);

    void GetItemAt(uint dwIndex, out IShellItem ppsi);

    void EnumItems(out IntPtr ppenumShellItems);
}

internal static class ShellItemHelper
{
    private const int SigdnFileSysPath = unchecked((int)0x80058000);

    internal static List<string> GetFilePaths(IntPtr psiItemArray)
    {
        var paths = new List<string>();

        if (psiItemArray == IntPtr.Zero)
        {
            return paths;
        }

        var items = (IShellItemArray)Marshal.GetObjectForIUnknown(psiItemArray);
        items.GetCount(out var count);

        for (uint i = 0; i < count; i++)
        {
            items.GetItemAt(i, out var item);
            item.GetDisplayName(SigdnFileSysPath, out var pathPtr);

            try
            {
                var path = Marshal.PtrToStringUni(pathPtr);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }
            finally
            {
                if (pathPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pathPtr);
                }
            }
        }

        return paths;
    }
}
