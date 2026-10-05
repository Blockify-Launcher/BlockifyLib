namespace BlockifyLib.Launcher.Version.Func
{
    public class Library
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Url { get; set; }
        public string? Hash { get; set; }

        // false when Hash is only one of several accepted checksums (old Forge "checksums" array).
        public bool StrictHash { get; set; } = true;
        public long Size { get; set; }
        public bool IsRequire { get; set; }
        public bool IsNative { get; set; }

    }

}
