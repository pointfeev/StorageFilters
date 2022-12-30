using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StorageFilters
{
    public class StorageFiltersData : GameComponent
    {
        public static readonly string DefaultMainFilterString = "ASF_MainFilter".Translate();
        public static float MaxFilterStringWidth = 94f;

        private static readonly Dictionary<int, string> ZoneCurrentKey = new Dictionary<int, string>();
        private static readonly Dictionary<int, int> ZoneCurrentDepth = new Dictionary<int, int>();

        private static readonly Dictionary<int, string> BuildingCurrentKey = new Dictionary<int, string>();
        private static readonly Dictionary<int, int> BuildingCurrentDepth = new Dictionary<int, int>();

        private static readonly Dictionary<IStoreSettingsParent, string> CurrentKey = new Dictionary<IStoreSettingsParent, string>();
        private static readonly Dictionary<IStoreSettingsParent, int> CurrentDepth = new Dictionary<IStoreSettingsParent, int>();

        private static List<int> zoneMainKeys;
        private static List<string> zoneMainValues;
        private static Dictionary<int, string> zoneMainFilterNames;

        private static List<int> buildingMainKeys;
        private static List<string> buildingNameValues;
        private static Dictionary<int, string> buildingMainFilterNames;

        private static List<IStoreSettingsParent> mainNameKeys;
        private static List<string> mainNameValues;
        private static Dictionary<IStoreSettingsParent, string> mainFilterNames;

        private static List<int> zoneKeys;
        private static List<ExtraThingFilters> zoneValues;
        private static Dictionary<int, ExtraThingFilters> zoneFilters;

        private static List<int> buildingKeys;
        private static List<ExtraThingFilters> buildingValues;
        private static Dictionary<int, ExtraThingFilters> buildingFilters;

        private static List<IStoreSettingsParent> filterKeys;
        private static List<ExtraThingFilters> filterValues;
        private static Dictionary<IStoreSettingsParent, ExtraThingFilters> filters;

        private static List<string> savedKeys;
        private static List<ExtraThingFilter> savedValues;
        internal static Dictionary<string, ExtraThingFilter> SavedFilters;

        public StorageFiltersData(Game _) { }

        internal static void SetMainFilterName(IStoreSettingsParent owner, string name)
        {
            switch (owner)
            {
                case Zone_Stockpile zone:
                    zoneMainFilterNames.SetOrAdd(zone.ID, name);
                    return;
                case Building_Storage building:
                    buildingMainFilterNames.SetOrAdd(building.thingIDNumber, name);
                    return;
                default:
                    mainFilterNames.SetOrAdd(owner, name);
                    return;
            }
        }

        internal static string GetMainFilterName(IStoreSettingsParent owner)
        {
            string name;
            switch (owner)
            {
                case Zone_Stockpile zone:
                    if (zoneMainFilterNames.TryGetValue(zone.ID, out name))
                        return name;
                    if (mainFilterNames.TryGetValue(owner, out name)) // handle pre-optimization zones
                    {
                        zoneMainFilterNames.SetOrAdd(zone.ID, name);
                        filters.Remove(owner);
                        return name;
                    }
                    return DefaultMainFilterString;
                case Building_Storage building:
                    if (buildingMainFilterNames.TryGetValue(building.thingIDNumber, out name))
                        return name;
                    if (mainFilterNames.TryGetValue(owner, out name)) // handle pre-optimization buildings
                    {
                        buildingMainFilterNames.SetOrAdd(building.thingIDNumber, name);
                        filters.Remove(owner);
                        return name;
                    }
                    return DefaultMainFilterString;
                default:
                    if (mainFilterNames.TryGetValue(owner, out name))
                        return name;
                    return DefaultMainFilterString;
            }
        }

        internal static void SetCurrentFilterKey(IStoreSettingsParent owner, string key)
        {
            switch (owner)
            {
                case Zone_Stockpile zone:
                    ZoneCurrentKey.SetOrAdd(zone.ID, key);
                    return;
                case Building_Storage building:
                    BuildingCurrentKey.SetOrAdd(building.thingIDNumber, key);
                    return;
                default:
                    CurrentKey.SetOrAdd(owner, key);
                    return;
            }
        }

        internal static string GetCurrentFilterKey(IStoreSettingsParent owner)
        {
            string key;
            switch (owner)
            {
                case Zone_Stockpile zone:
                    if (ZoneCurrentKey.TryGetValue(zone.ID, out key))
                        return key;
                    return null;
                case Building_Storage building:
                    if (BuildingCurrentKey.TryGetValue(building.thingIDNumber, out key))
                        return key;
                    return null;
                default:
                    if (CurrentKey.TryGetValue(owner, out key))
                        return key;
                    return null;
            }
        }

        internal static void SetCurrentFilterDepth(IStoreSettingsParent owner, int depth)
        {
            switch (owner)
            {
                case Zone_Stockpile zone:
                    ZoneCurrentDepth.SetOrAdd(zone.ID, depth);
                    return;
                case Building_Storage building:
                    BuildingCurrentDepth.SetOrAdd(building.thingIDNumber, depth);
                    return;
                default:
                    CurrentDepth.SetOrAdd(owner, depth);
                    return;
            }
        }

        internal static int GetCurrentFilterDepth(IStoreSettingsParent owner)
        {
            int depth;
            switch (owner)
            {
                case Zone_Stockpile zone:
                    return ZoneCurrentDepth.TryGetValue(zone.ID, out depth) ? depth : 0;
                case Building_Storage building:
                    return BuildingCurrentDepth.TryGetValue(building.thingIDNumber, out depth) ? depth : 0;
                default:
                    return CurrentDepth.TryGetValue(owner, out depth) ? depth : 0;
            }
        }

        internal static void SetExtraThingFilters(IStoreSettingsParent owner, ExtraThingFilters extraThingFilters)
        {
            switch (owner)
            {
                case Zone_Stockpile zone:
                    zoneFilters.SetOrAdd(zone.ID, extraThingFilters);
                    return;
                case Building_Storage building:
                    buildingFilters.SetOrAdd(building.thingIDNumber, extraThingFilters);
                    return;
                default:
                    filters.SetOrAdd(owner, extraThingFilters);
                    return;
            }
        }

        internal static ExtraThingFilters GetExtraThingFilters(IStoreSettingsParent owner)
        {
            ExtraThingFilters extraThingFilters;
            switch (owner)
            {
                case Zone_Stockpile zone:
                    if (zoneFilters.TryGetValue(zone.ID, out extraThingFilters))
                        return extraThingFilters;
                    if (filters.TryGetValue(owner, out extraThingFilters)) // handle pre-optimization zones
                    {
                        zoneFilters.SetOrAdd(zone.ID, extraThingFilters);
                        filters.Remove(owner);
                    }
                    return extraThingFilters;
                case Building_Storage building:
                    if (buildingFilters.TryGetValue(building.thingIDNumber, out extraThingFilters))
                        return extraThingFilters;
                    if (filters.TryGetValue(owner, out extraThingFilters)) // handle pre-optimization buildings
                    {
                        buildingFilters.SetOrAdd(building.thingIDNumber, extraThingFilters);
                        filters.Remove(owner);
                    }
                    return extraThingFilters;
                default:
                    return filters.TryGetValue(owner);
            }
        }

        internal static IEnumerable<ExtraThingFilter> AllFilters()
        {
            foreach (KeyValuePair<int, ExtraThingFilters> zoneFilter in zoneFilters)
                foreach (KeyValuePair<string, ExtraThingFilter> filter in zoneFilter.Value)
                    yield return filter.Value;
            foreach (KeyValuePair<int, ExtraThingFilters> buildingFilter in buildingFilters)
                foreach (KeyValuePair<string, ExtraThingFilter> filter in buildingFilter.Value)
                    yield return filter.Value;
        }

        private static void Initialize<TK, TV>(ref List<TK> keys, ref List<TV> values, ref Dictionary<TK, TV> dictionary)
        {
            if (keys == null)
                keys = new List<TK>();
            if (values == null)
                values = new List<TV>();
            if (dictionary == null)
                dictionary = new Dictionary<TK, TV>();
            _ = keys.RemoveAll(k => k == null);
            _ = values.RemoveAll(v => v == null);
            _ = dictionary.RemoveAll(e => e.Key == null || e.Value == null);
        }

        public static void ExposeSavedFilter()
        {
            Scribe_Collections.Look(ref SavedFilters, "savedFilter", LookMode.Value, LookMode.Deep, ref savedKeys, ref savedValues);
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
                return;
            Initialize(ref savedKeys, ref savedValues, ref SavedFilters);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref filters, "filters", LookMode.Reference, LookMode.Deep, ref filterKeys, ref filterValues);
            Scribe_Collections.Look(ref zoneFilters, "zoneFilters", LookMode.Value, LookMode.Deep, ref zoneKeys, ref zoneValues);
            Scribe_Collections.Look(ref buildingFilters, "buildingFilters", LookMode.Value, LookMode.Deep, ref buildingKeys, ref buildingValues);
            Scribe_Collections.Look(ref mainFilterNames, "mainFilterString", LookMode.Reference, LookMode.Value, ref mainNameKeys, ref mainNameValues);
            Scribe_Collections.Look(ref zoneMainFilterNames, "zoneMainFilterString", LookMode.Value, LookMode.Value, ref zoneMainKeys, ref zoneMainValues);
            Scribe_Collections.Look(ref buildingMainFilterNames, "buildingMainFilterString", LookMode.Value, LookMode.Value, ref buildingMainKeys,
                ref buildingNameValues);
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
                return;
            Initialize(ref filterKeys, ref filterValues, ref filters);
            Initialize(ref zoneKeys, ref zoneValues, ref zoneFilters);
            Initialize(ref buildingKeys, ref buildingValues, ref buildingFilters);
            Initialize(ref mainNameKeys, ref mainNameValues, ref mainFilterNames);
            Initialize(ref zoneMainKeys, ref zoneMainValues, ref zoneMainFilterNames);
            Initialize(ref buildingMainKeys, ref buildingNameValues, ref buildingMainFilterNames);
        }
    }
}