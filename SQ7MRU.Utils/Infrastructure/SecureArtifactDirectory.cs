using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SQ7MRU.Utils.Infrastructure;

// Windows: hold each ancestor without FILE_SHARE_DELETE, rejecting reparse points by handle.
// Unix: walk with openat(O_NOFOLLOW) and perform file operations relative to the held directory fd.
internal sealed class SecureArtifactDirectory : IDisposable
{
    private readonly List<SafeFileHandle> handles = [];
    private readonly string directory;
    private SafeFileHandle Current => handles[^1];
    private int Descriptor => Current.DangerousGetHandle().ToInt32();
    private static int DirectoryFlags => OperatingSystem.IsMacOS() ? 0x100000 | 0x100 | 0x1000000 : 0x10000 | 0x20000 | 0x80000;
    private static int NoFollow => OperatingSystem.IsMacOS() ? 0x100 : 0x20000;

    public SecureArtifactDirectory(string path, bool create)
    {
        directory = Path.GetFullPath(path);
        // macOS exposes these OS-managed aliases even for ordinary temporary directories.
        if (OperatingSystem.IsMacOS() && (directory.StartsWith("/var/", StringComparison.Ordinal) || directory.StartsWith("/tmp/", StringComparison.Ordinal)))
            directory = "/private" + directory;
        try
        {
            var root = Path.GetPathRoot(directory)!;
            var current = root;
            if (!OperatingSystem.IsWindows()) handles.Add(UnixHandle(Open(root, DirectoryFlags, 0)));
            foreach (var part in directory[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, part);
                if (OperatingSystem.IsWindows())
                {
                    if (create && !Directory.Exists(current)) Directory.CreateDirectory(current);
                    var handle = CreateFile(current, 0x80000000, 3, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
                    if (handle.IsInvalid) { handle.Dispose(); ThrowLastError(); }
                    handles.Add(handle);
                    if (!GetFileInformationByHandle(handle, out var info)) ThrowLastError();
                    if ((info.Attributes & (uint)FileAttributes.ReparsePoint) != 0 || (info.Attributes & (uint)FileAttributes.Directory) == 0)
                        throw UnsafePath();
                }
                else
                {
                    if (create && MkdirAt(Descriptor, part, 0x1c0) != 0 && Marshal.GetLastPInvokeError() != 17) ThrowLastError();
                    handles.Add(UnixHandle(OpenAt(Descriptor, part, DirectoryFlags, 0)));
                }
            }
        }
        catch { Dispose(); throw; }
    }

    public FileStream OpenRead(string name)
    {
        if (OperatingSystem.IsWindows())
        {
            var handle = CreateFile(Path.Combine(directory, name), 0x80000000, 1, IntPtr.Zero, 3, 0x40200000, IntPtr.Zero);
            if (handle.IsInvalid) { handle.Dispose(); ThrowLastError(); }
            if (!GetFileInformationByHandle(handle, out var info)) { handle.Dispose(); ThrowLastError(); }
            if ((info.Attributes & (uint)FileAttributes.ReparsePoint) != 0 || info.Links > 1) { handle.Dispose(); throw UnsafePath(); }
            return new FileStream(handle, FileAccess.Read, 4096, isAsync: true);
        }
        // Nonblocking prevents an attacker-controlled FIFO from hanging the process.
        var fd = OpenAt(Descriptor, name, NoFollow | (OperatingSystem.IsMacOS() ? 4 : 0x800), 0);
        return new FileStream(UnixHandle(fd), FileAccess.Read, 4096, isAsync: false);
    }

    public FileStream CreateTemporary(string name)
    {
        if (OperatingSystem.IsWindows())
            return new FileStream(Path.Combine(directory, name), FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        var flags = 1 | NoFollow | (OperatingSystem.IsMacOS() ? 0x200 | 0x800 : 0x40 | 0x80);
        return new FileStream(UnixHandle(OpenAt(Descriptor, name, flags, 0x180)), FileAccess.Write, 4096, isAsync: false);
    }

    // The persistent lock file is never deleted: replacing it would split concurrent locks.
    public FileStream? TryAcquireLock(string name)
    {
        if (OperatingSystem.IsWindows())
        {
            var handle = CreateFile(Path.Combine(directory, name), 0xc0000000, 0, IntPtr.Zero, 4, 0x00200000, IntPtr.Zero);
            if (handle.IsInvalid)
            {
                var error = Marshal.GetLastPInvokeError();
                handle.Dispose();
                if (error == 32) return null;
                throw new IOException("Cannot acquire the metadata lock.");
            }
            if (!GetFileInformationByHandle(handle, out var info) ||
                (info.Attributes & (uint)FileAttributes.ReparsePoint) != 0 || info.Links > 1)
            { handle.Dispose(); throw UnsafePath(); }
            return new FileStream(handle, FileAccess.ReadWrite);
        }
        var flags = 2 | NoFollow | (OperatingSystem.IsMacOS() ? 0x200 | 4 : 0x40 | 0x800);
        var unixHandle = UnixHandle(OpenAt(Descriptor, name, flags, 0x180));
        if (Flock(unixHandle.DangerousGetHandle().ToInt32(), 2 | 4) == 0)
            return new FileStream(unixHandle, FileAccess.ReadWrite);
        var lockError = Marshal.GetLastPInvokeError();
        unixHandle.Dispose();
        if (lockError is 11 or 35) return null;
        throw new IOException("Cannot acquire the metadata lock.");
    }

    public void Commit(string temporary, string name)
    {
        if (OperatingSystem.IsWindows()) File.Move(Path.Combine(directory, temporary), Path.Combine(directory, name), true);
        else if (RenameAt(Descriptor, temporary, Descriptor, name) != 0) ThrowLastError();
    }
    public void RemoveTemporary(string name)
    {
        if (OperatingSystem.IsWindows()) File.Delete(Path.Combine(directory, name));
        else if (UnlinkAt(Descriptor, name, 0) != 0 && Marshal.GetLastPInvokeError() != 2) ThrowLastError();
    }
    private static SafeFileHandle UnixHandle(int fd)
    {
        if (fd < 0) ThrowLastError();
        return new SafeFileHandle((IntPtr)fd, ownsHandle: true);
    }
    private static ServiceException UnsafePath() => new(FailureKind.Storage, "Symbolic links, junctions and shared file links are not allowed in the output path.");
    private static void ThrowLastError()
    {
        var error = Marshal.GetLastPInvokeError();
        if ((!OperatingSystem.IsWindows() && error == 2) || (OperatingSystem.IsWindows() && error is 2 or 3)) throw new FileNotFoundException();
        if (!OperatingSystem.IsWindows() && error is 20 or 40 or 62) throw UnsafePath();
        throw new IOException("Cannot securely access the output directory.", new Win32Exception(error));
    }
    public void Dispose()
    {
        for (var index = handles.Count - 1; index >= 0; index--) handles[index].Dispose();
        handles.Clear();
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME Creation, Access, Write;
        public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileInformation information);
    [DllImport("libc", EntryPoint = "open", SetLastError = true)] private static extern int Open(string path, int flags, uint mode);
    [DllImport("libc", EntryPoint = "openat", SetLastError = true)] private static extern int OpenAt(int directory, string path, int flags, uint mode);
    [DllImport("libc", EntryPoint = "mkdirat", SetLastError = true)] private static extern int MkdirAt(int directory, string path, uint mode);
    [DllImport("libc", EntryPoint = "renameat", SetLastError = true)] private static extern int RenameAt(int oldDirectory, string oldName, int newDirectory, string newName);
    [DllImport("libc", EntryPoint = "unlinkat", SetLastError = true)] private static extern int UnlinkAt(int directory, string name, int flags);
    [DllImport("libc", EntryPoint = "flock", SetLastError = true)] private static extern int Flock(int descriptor, int operation);
}
