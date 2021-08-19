using RimWorld;
using Verse;
using System.Collections.Generic;

namespace StorageFilters
{
    public class StorageFiltersData : GameComponent
    {
        public StorageFiltersData(Game _) { }

        public static readonly string DefaultMainFilterString = "Main filter";
        public static readonly float MaxFilterStringWidth = 84f;

        private static List<string> savedFilterKeys;
        private static List<ExtraThingFilter> savedFilterValues;
        private static Dictionary<string, ExtraThingFilter> savedFilter = SavedFilter;

        private static List<IStoreSettingsParent> filterKeys;
        private static List<ExtraThingFilters> filterValues;
        private static Dictionary<IStoreSettingsParent, ExtraThingFilters> filters = Filters;

        private static List<IStoreSettingsParent> mainFilterStringKeys;
        private static List<string> mainFilterStringValues;
        private static Dictionary<IStoreSettingsParent, string> mainFilterString = MainFilterString;

        private static Dictionary<IStoreSettingsParent, string> currentFilterKey = CurrentFilterKey;

        public static Dictionary<string, ExtraThingFilter> SavedFilterNoLoad
        {
            get
            {
                if (savedFilter == null)
                {
                    savedFilter = new Dictionary<string, ExtraThingFilter>();
                }
                if (savedFilterKeys == null)
                {
                    savedFilterKeys = new List<string>();
                }
                if (savedFilterValues == null)
                {
                    savedFilterValues = new List<ExtraThingFilter>();
                }
                savedFilter.RemoveAll((KeyValuePair<string, ExtraThingFilter> entry) => entry.Key is null || entry.Value is null);
                savedFilterKeys.RemoveAll((string entry) => entry is null);
                savedFilterValues.RemoveAll((ExtraThingFilter entry) => entry is null);
                return savedFilter;
            }
        }

        public static Dictionary<string, ExtraThingFilter> SavedFilter
        {
            get
            {
                SaveUtils.Load();
                return SavedFilterNoLoad;
            }
        }

        public static Dictionary<IStoreSettingsParent, ExtraThingFilters> Filters
        {
            get
            {
                if (filters == null)
                {
                    filters = new Dictionary<IStoreSettingsParent, ExtraThingFilters>();
                }
                if (filterKeys == null)
                {
                    filterKeys = new List<IStoreSettingsParent>();
                }
                if (filterValues == null)
                {
                    filterValues = new List<ExtraThingFilters>();
                }
                filters.RemoveAll((KeyValuePair<IStoreSettingsParent, ExtraThingFilters> entry) => entry.Key is null || entry.Value is null);
                filterKeys.RemoveAll((IStoreSettingsParent entry) => entry is null);
                filterValues.RemoveAll((ExtraThingFilters entry) => entry is null);
                return filters;
            }
        }

        public static Dictionary<IStoreSettingsParent, string> MainFilterString
        {
            get
            {
                if (mainFilterString == null)
                {
                    mainFilterString = new Dictionary<IStoreSettingsParent, string>();
                }
                if (mainFilterStringKeys == null)
                {
                    mainFilterStringKeys = new List<IStoreSettingsParent>();
                }
                if (mainFilterStringValues == null)
                {
                    mainFilterStringValues = new List<string>();
                }
                mainFilterString.RemoveAll((KeyValuePair<IStoreSettingsParent, string> entry) => entry.Key is null || entry.Value is null);
                mainFilterStringKeys.RemoveAll((IStoreSettingsParent entry) => entry is null);
                mainFilterStringValues.RemoveAll((string entry) => entry is null);
                return mainFilterString;
            }
        }

        public static Dictionary<IStoreSettingsParent, string> CurrentFilterKey
        {
            get
            {
                if (currentFilterKey == null)
                {
                    currentFilterKey = new Dictionary<IStoreSettingsParent, string>();
                }
                currentFilterKey.RemoveAll((KeyValuePair<IStoreSettingsParent, string> entry) => entry.Key is null || entry.Value is null);
                return currentFilterKey;
            }
        }

        public static void ExposeSavedFilter()
        {
            Scribe_Collections.Look(ref savedFilter, "savedFilter", LookMode.Value, LookMode.Deep, ref savedFilterKeys, ref savedFilterValues);
            _ = SavedFilterNoLoad;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref filters, "filters", LookMode.Reference, LookMode.Deep, ref filterKeys, ref filterValues);
            _ = Filters;

            Scribe_Collections.Look(ref mainFilterString, "mainFilterString", LookMode.Reference, LookMode.Value, ref mainFilterStringKeys, ref mainFilterStringValues);
            _ = MainFilterString;
        }
    }
}
