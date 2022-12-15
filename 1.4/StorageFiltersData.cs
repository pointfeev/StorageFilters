using System.Collections.Generic;
using RimWorld;
using StorageFilters.Utilities;
using Verse;

namespace StorageFilters
{
    public class StorageFiltersData : GameComponent
    {
        public static readonly string DefaultMainFilterString = "ASF_MainFilter".Translate();
        public static float MaxFilterStringWidth = 94f;

        private static List<string> _savedFilterKeys;
        private static List<ExtraThingFilter> _savedFilterValues;
        private static Dictionary<string, ExtraThingFilter> _savedFilter = SavedFilter;

        private static List<IStoreSettingsParent> _filterKeys;
        private static List<ExtraThingFilters> _filterValues;
        private static Dictionary<IStoreSettingsParent, ExtraThingFilters> _filters = Filters;

        private static List<IStoreSettingsParent> _mainFilterStringKeys;
        private static List<string> _mainFilterStringValues;
        private static Dictionary<IStoreSettingsParent, string> _mainFilterString = MainFilterString;

        private static Dictionary<IStoreSettingsParent, string> _currentFilterKey = CurrentFilterKey;
        private static Dictionary<IStoreSettingsParent, int> _currentFilterDepth = CurrentFilterDepth;

        public StorageFiltersData(Game _) { }

        public static Dictionary<string, ExtraThingFilter> SavedFilterNoLoad
        {
            get
            {
                if (_savedFilter is null)
                    _savedFilter = new Dictionary<string, ExtraThingFilter>();
                if (_savedFilterKeys is null)
                    _savedFilterKeys = new List<string>();
                if (_savedFilterValues is null)
                    _savedFilterValues = new List<ExtraThingFilter>();
                _ = _savedFilter.RemoveAll(entry => entry.Key is null || entry.Value is null);
                _ = _savedFilterKeys.RemoveAll(entry => entry is null);
                _ = _savedFilterValues.RemoveAll(entry => entry is null);
                return _savedFilter;
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
                if (_filters is null)
                    _filters = new Dictionary<IStoreSettingsParent, ExtraThingFilters>();
                if (_filterKeys is null)
                    _filterKeys = new List<IStoreSettingsParent>();
                if (_filterValues is null)
                    _filterValues = new List<ExtraThingFilters>();
                _ = _filters.RemoveAll(entry => entry.Key is null || entry.Value is null);
                _ = _filterKeys.RemoveAll(entry => entry is null);
                _ = _filterValues.RemoveAll(entry => entry is null);
                return _filters;
            }
        }

        public static Dictionary<IStoreSettingsParent, string> MainFilterString
        {
            get
            {
                if (_mainFilterString is null)
                    _mainFilterString = new Dictionary<IStoreSettingsParent, string>();
                if (_mainFilterStringKeys is null)
                    _mainFilterStringKeys = new List<IStoreSettingsParent>();
                if (_mainFilterStringValues is null)
                    _mainFilterStringValues = new List<string>();
                _ = _mainFilterString.RemoveAll(entry => entry.Key is null || entry.Value is null);
                _ = _mainFilterStringKeys.RemoveAll(entry => entry is null);
                _ = _mainFilterStringValues.RemoveAll(entry => entry is null);
                return _mainFilterString;
            }
        }

        public static Dictionary<IStoreSettingsParent, string> CurrentFilterKey
        {
            get
            {
                if (_currentFilterKey is null)
                    _currentFilterKey = new Dictionary<IStoreSettingsParent, string>();
                _ = _currentFilterKey.RemoveAll(entry => entry.Key is null || entry.Value is null);
                return _currentFilterKey;
            }
        }

        public static Dictionary<IStoreSettingsParent, int> CurrentFilterDepth
        {
            get
            {
                if (_currentFilterDepth is null)
                    _currentFilterDepth = new Dictionary<IStoreSettingsParent, int>();
                _ = _currentFilterDepth.RemoveAll(entry => entry.Key is null);
                return _currentFilterDepth;
            }
        }

        public static void ExposeSavedFilter()
        {
            Scribe_Collections.Look(ref _savedFilter, "savedFilter", LookMode.Value, LookMode.Deep,
                                    ref _savedFilterKeys,
                                    ref _savedFilterValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                _ = SavedFilterNoLoad;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _filters, "filters", LookMode.Reference, LookMode.Deep, ref _filterKeys,
                                    ref _filterValues);
            Scribe_Collections.Look(ref _mainFilterString, "mainFilterString", LookMode.Reference, LookMode.Value,
                                    ref _mainFilterStringKeys, ref _mainFilterStringValues);
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
                return;
            _ = Filters;
            _ = MainFilterString;
        }
    }
}