namespace BlockifyLib.Launcher.Version.Func
{
    public class SortVersionOption
    {
        public enum VersionSortPropertyOption
        {
            Name, Version, ReleaseDate
        }

        public enum VersionNullReleaseDateSortOption
        {
            AsLatest, AsOldest
        }

        public ProfileConverter.VersionType[] typeOrder { get; set; } =
            {
                ProfileConverter.VersionType.Custom, ProfileConverter.VersionType.Release,
                ProfileConverter.VersionType.Snapshot, ProfileConverter.VersionType.OldBeta,
                ProfileConverter.VersionType.OldAlpha
            };

        public VersionSortPropertyOption PropertyOrderBy { get; set; } =
            VersionSortPropertyOption.Version;

        public VersionNullReleaseDateSortOption NullReleaseDateSortOption { get; set; } =
            VersionNullReleaseDateSortOption.AsOldest;

        public bool AscendingPropertyOrder { get; set; } = true;
        public bool CustomAsRelease { get; set; } = false;
        public bool TypeClassification { get; set; } = true;

    }

    public class SortVersionMetadata
    {
        public SortVersionMetadata(SortVersionOption option)
        {
            this.option = option;
            this.typePriority = new Dictionary<ProfileConverter.VersionType, int>();
            for (int i = 0; i < option.typeOrder.Length; i++)
                typePriority[option.typeOrder[i]] = i;

            List<SortVersionOption.VersionSortPropertyOption> propertyList
                = new List<SortVersionOption.VersionSortPropertyOption>();
            propertyList.Add(option.PropertyOrderBy);
            foreach (SortVersionOption.VersionSortPropertyOption item in
                Enum.GetValues(typeof(SortVersionOption.VersionSortPropertyOption)))
                if (option.PropertyOrderBy != item)
                    propertyList.Add(item);

            propertyOptions = propertyList.ToArray();

            switch (option.NullReleaseDateSortOption)
            {
                case SortVersionOption.VersionNullReleaseDateSortOption.AsLatest:
                    defaultDateTime = DateTime.MaxValue;
                    break;
                case SortVersionOption.VersionNullReleaseDateSortOption.AsOldest:
                    defaultDateTime = DateTime.MinValue;
                    break;
            }
        }

        private readonly SortVersionOption option;
        private readonly SortVersionOption.VersionSortPropertyOption[] propertyOptions;
        private readonly Dictionary<ProfileConverter.VersionType, int> typePriority;
        private readonly DateTime defaultDateTime;

        public Metadata.VersionMetadata[] Sort(IEnumerable<Metadata.VersionMetadata> org)
        {
            var filtered = org.Where(x => getTypePriority(x.ProfType) >= 0)
                .ToArray();
            Array.Sort(filtered, compare);
            return filtered;
        }

        private int getTypePriority(ProfileConverter.VersionType type)
        {
            if (option.CustomAsRelease && type == ProfileConverter.VersionType.Custom)
                type = ProfileConverter.VersionType.Release;

            if (typePriority.TryGetValue(type, out int p))
                return p;

            return -1;
        }

        private int compareType(Metadata.VersionMetadata versionOne, Metadata.VersionMetadata versionTwo) =>
            getTypePriority(versionOne.ProfType) - getTypePriority(versionTwo.ProfType);

        private int compareName(Metadata.VersionMetadata versionOne, Metadata.VersionMetadata versionTwo)
        {
            int result = string.CompareOrdinal(versionOne.Name, versionTwo.Name);
            return !option.AscendingPropertyOrder ? result *= -1 : result;
        }

        private int compareVersion(Metadata.VersionMetadata versionOne, Metadata.VersionMetadata versionTwo)
        {
            bool versionOneR = System.Version.TryParse(versionOne.Name, out System.Version? versionOneV);
            bool versionTwoR = System.Version.TryParse(versionTwo.Name, out System.Version? versionTwoV);

            if (versionOneR && versionTwoR)
            {
                int result = versionOneV?.CompareTo(versionTwoV) ?? 0;
                if (!option.AscendingPropertyOrder)
                    result *= -1;
                return result;
            }

            return versionOneR ? 1 : (versionTwoR ? -1 : 0);
        }

        private int compareReleaseDate(Metadata.VersionMetadata versionOne, Metadata.VersionMetadata versionTwo)
        {
            int result = DateTime.Compare(versionOne.ReleaseTime ?? defaultDateTime, versionTwo.ReleaseTime ?? defaultDateTime);
            return !option.AscendingPropertyOrder ? result *= -1 : result;
        }

        private int compareProperty(Metadata.VersionMetadata versionOne, Metadata.VersionMetadata versionTwo,
            SortVersionOption.VersionSortPropertyOption propertyOption)
        {
            switch (propertyOption)
            {
                case SortVersionOption.VersionSortPropertyOption.Name:
                    return compareName(versionOne, versionTwo);
                case SortVersionOption.VersionSortPropertyOption.ReleaseDate:
                    return compareReleaseDate(versionOne, versionTwo);
                case SortVersionOption.VersionSortPropertyOption.Version:
                    return compareVersion(versionOne, versionTwo);
            }

            return 0;
        }

        private int compare(Metadata.VersionMetadata versionOne, Metadata.VersionMetadata versionTwo)
        {
            int typeCompareResult = compareType(versionOne, versionTwo);
            if (option.TypeClassification && typeCompareResult != 0)
                return typeCompareResult;

            foreach (var propOption in propertyOptions)
            {
                int result = compareProperty(versionOne, versionTwo, propOption);
                if (result != 0)
                    return result;
            }

            return typeCompareResult;
        }
    }
}
