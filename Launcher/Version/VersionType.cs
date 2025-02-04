namespace BlockifyLib.Launcher.Version
{
    public static class ProfileConverter
    {
        public static VersionType FromString(string val)
        {
            switch (val)
            {
                case "release":
                    return VersionType.Release;
                case "snapshot":
                    return VersionType.Snapshot;
                case "old_alpha":
                    return VersionType.OldAlpha;
                case "old_beta":
                    return VersionType.OldBeta;
                default:
                    return VersionType.Custom;
            }
        }

        public static string ToString(VersionType type)
        {
            switch (type)
            {
                case VersionType.OldAlpha:
                    return "old_alpha";
                case VersionType.OldBeta:
                    return "old_beta";
                case VersionType.Snapshot:
                    return "snapshot";
                case VersionType.Release:
                    return "release";
                case VersionType.Custom:
                default:
                    return "unknown";
            }
        }

        public static List<VersionType> GetListVersion()
        {
            var list = new List<VersionType>();
            Array days = Enum.GetValues(typeof(VersionType));
            foreach (VersionType element in days)
                list.Add(element);
            return list;
        }

        public static bool CheckOld(string val) =>
            CheckOld(FromString(val));

        public static bool CheckOld(VersionType t)
        {
            if (t == VersionType.OldAlpha || t == VersionType.OldBeta)
                return true;
            else
                return false;
        }

        public enum VersionType
        {
            OldAlpha,
            OldBeta,
            Snapshot,
            Release,
            Custom
        }
    }
}
