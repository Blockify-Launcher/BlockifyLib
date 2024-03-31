using System.Runtime.InteropServices;

namespace BlockifyLib.Launcher.Utils
{
    internal static class NativeMethods
    {
        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        private const int S_IRUSR = 0x100;
        private const int S_IWUSR = 0x80;
        private const int S_IXUSR = 0x40;

        private const int S_IRGRP = 0x20;
        private const int S_IWGRP = 0x10;
        private const int S_IXGRP = 0x8;

        private const int S_IROTH = 0x4;
        private const int S_IWOTH = 0x2;
        private const int S_IXOTH = 0x1;

        public static readonly int Chmod755 = S_IRUSR | S_IXUSR | S_IWUSR
                                            | S_IRGRP | S_IXGRP
                                            | S_IROTH | S_IXOTH;

        public static void Chmod(string path, int mode)
        {
            chmod(path, mode);
        }
    }
}