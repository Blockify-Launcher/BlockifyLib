namespace BlockifyLib.Launcher
{
    public static class ProfileConverter
    {
        public static ProfileType FromString(string val)
        {
            switch (val)
            {
                case "release":
                    return ProfileType.Release;
                case "snapshot":
                    return ProfileType.Snapshot;
                case "old_alpha":
                    return ProfileType.OldAlpha;
                case "old_beta":
                    return ProfileType.OldBeta;
                default:
                    return ProfileType.Custom;
            }
        }

        public static string ToString(ProfileType type)
        {
            switch (type)
            {
                case ProfileType.OldAlpha:
                    return "old_alpha";
                case ProfileType.OldBeta:
                    return "old_beta";
                case ProfileType.Snapshot:
                    return "snapshot";
                case ProfileType.Release:
                    return "release";
                case ProfileType.Custom:
                default:
                    return "unknown";
            }
        }

        public static bool CheckOld(string val) =>
            CheckOld(FromString(val));

        public static bool CheckOld(ProfileType t)
        {
            if (t == ProfileType.OldAlpha || t == ProfileType.OldBeta)
                return true;
            else
                return false;
        }

        public enum ProfileType
        {
            OldAlpha,
            OldBeta,
            Snapshot,
            Release,
            Custom
        }
    }
}
