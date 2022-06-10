using System.Collections.Generic;
using System.Reflection;

using RimWorld;

using UnityEngine;

using Verse;

namespace StorageFilters
{
    public static class StorageFilters
    {
        public static Rect? StorageTabRect = null;

        public static void FillTab(ITab_Storage instance, Vector2 size)
        {
            try
            {
                _ = (float)instance.GetType().GetMethod("get_TopAreaHeight", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance, new object[] { });
            }
            catch
            {
                return; // if it can't get the TopAreaHeight, it means it's a container without priority and thus not actually storage
            }
            IStoreSettingsParent storeSettingsParent = GenUtils.GetSelectedStoreSettingsParent();
            if (storeSettingsParent is null)
            {
                Log.Warning("ASF_ModPrefix".Translate() + "ASF_StoreSettingsParentError".Translate());
                return;
            }
            ExtraThingFilters tabFilters = StorageFiltersData.Filters.TryGetValue(storeSettingsParent);
            if (tabFilters is null)
            {
                StorageFiltersData.Filters.SetOrAdd(storeSettingsParent, new ExtraThingFilters());
                tabFilters = StorageFiltersData.Filters.TryGetValue(storeSettingsParent);
            }
            string mainFilterString = StorageFiltersData.MainFilterString.TryGetValue(storeSettingsParent);
            if (mainFilterString is null)
            {
                StorageFiltersData.MainFilterString.SetOrAdd(storeSettingsParent, StorageFiltersData.DefaultMainFilterString);
                mainFilterString = StorageFiltersData.MainFilterString.TryGetValue(storeSettingsParent);
            }
            string tabFilter = StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent);
            if (tabFilter is null)
            {
                StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
                StorageFiltersData.CurrentFilterDepth.SetOrAdd(storeSettingsParent, 0);
                tabFilter = StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent);
            }
            Rect window = new Rect(0, 0, size.x, size.y);
            StorageTabRect = window;
            GUI.BeginGroup(window.ContractedBy(10f));
            Rect position = new Rect(166f, 0, Text.CalcSize(tabFilter).x + 16f, 29f);
            GenUtils.FilterSelectionButton(instance, storeSettingsParent, tabFilters, mainFilterString, tabFilter, position);
            GUI.EndGroup();
        }

        public static void DoThingFilterConfigWindow(ref ThingFilter filter)
        {
            IStoreSettingsParent storeSettingsParent = GenUtils.GetSelectedStoreSettingsParent();
            if (storeSettingsParent != null)
            {
                StorageSettings settings = storeSettingsParent.GetStoreSettings();
                if (settings != null && settings.filter == filter)
                {
                    ExtraThingFilters tabFilters = StorageFiltersData.Filters.TryGetValue(storeSettingsParent);
                    string tabFilter = StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent);
                    int tabFilterDepth = !(Find.WindowStack.WindowOfType<Dialog_EditFilter>() is null)
                        ? StorageFiltersData.CurrentFilterDepth.TryGetValue(storeSettingsParent) : 0;
                    if (!(tabFilters is null) && !(tabFilter is null))
                    {
                        ExtraThingFilter _filter = tabFilters.Get(tabFilter);
                        while (!(_filter is null) && _filter.FilterDepth < tabFilterDepth)
                            _filter = _filter.NextInPriorityFilter;
                        if (!(_filter is null))
                            filter = _filter;
                    }
                }
            }
        }

        public static bool AllowsThingOrThingDef(this ThingFilter thingFilter, object thingOrThingDef)
        {
            if (thingOrThingDef is Thing)
                return thingFilter.Allows(thingOrThingDef as Thing);
            else if (thingOrThingDef is ThingDef)
                return thingFilter.Allows(thingOrThingDef as ThingDef);
            return false;
        }

        public static bool AllowedToAcceptThingOrThingDef(this StorageSettings storageSettings, object thingOrThingDef)
        {
            if (thingOrThingDef is Thing)
                return storageSettings.AllowedToAccept(thingOrThingDef as Thing);
            else if (thingOrThingDef is ThingDef)
                return storageSettings.AllowedToAccept(thingOrThingDef as ThingDef);
            return false;
        }

        public static bool IsCurrentDestinationWorseThan(this Thing thing, IStoreSettingsParent owner) => !(StoreUtility.CurrentHaulDestinationOf(thing)
            is IHaulDestination haulDestination) || haulDestination != owner || haulDestination.GetStoreSettings().Priority < owner.GetStoreSettings().Priority;

        public static void AllowedToAccept(IStoreSettingsParent owner, ThingFilter filter, object thingOrThingDef, ref bool result)
        {
            result = filter.AllowsThingOrThingDef(thingOrThingDef);
            if (!(owner is null))
                if (result && owner.GetParentStoreSettings() is StorageSettings parentStoreSettings && !parentStoreSettings.AllowedToAcceptThingOrThingDef(thingOrThingDef))
                    result = false;
                else if (!result && StorageFiltersData.Filters.TryGetValue(owner) is ExtraThingFilters extraFilters && extraFilters.Count > 0)
                    foreach (ExtraThingFilter extraFilter in extraFilters.Values)
                        if (extraFilter.Enabled && extraFilter is ExtraThingFilter currentFilter)
                            while (!(currentFilter is null))
                                if (result = currentFilter.AllowsThingOrThingDef(thingOrThingDef))
                                    return;
                                else if (currentFilter.NextInPriorityFilter is ExtraThingFilter nextFilter)
                                {
                                    bool currentFilterNeedsToStoreAnyThingOnMap = false;
                                    foreach (ThingDef allowedThingDef in currentFilter.AllowedThingDefs)
                                        if (currentFilterNeedsToStoreAnyThingOnMap = Find.CurrentMap.listerThings.ThingsOfDef(allowedThingDef).Any(thing => currentFilter.Allows(thing) && thing.IsCurrentDestinationWorseThan(owner)))
                                            break;
                                    if (currentFilterNeedsToStoreAnyThingOnMap)
                                        break;
                                    else
                                        currentFilter = nextFilter;
                                }
                                else
                                    break;
        }

        public static void AllowedToAccept(IStoreSettingsParent owner, ThingFilter filter, ThingDef thingDef, ref bool result) => AllowedToAccept(owner, filter, thingDef as object, ref result);

        public static void AllowedToAccept(IStoreSettingsParent owner, ThingFilter filter, Thing thing, ref bool result) => AllowedToAccept(owner, filter, thing as object, ref result);

        private static ExtraThingFilters copiedFilters = null;
        private static string copiedMainFilterString = null;
        private static string copiedCurrentFilterKey = null;

        public static void Copy(StorageSettings storageSettings)
        {
            if (storageSettings.owner is IStoreSettingsParent storeSettingsParent)
            {
                if (StorageFiltersData.Filters.TryGetValue(storeSettingsParent) is ExtraThingFilters filters)
                {
                    copiedFilters = new ExtraThingFilters();
                    foreach (KeyValuePair<string, ExtraThingFilter> entry in filters)
                        copiedFilters.Set(entry.Key, entry.Value.Copy());
                }
                copiedMainFilterString = StorageFiltersData.MainFilterString.TryGetValue(storeSettingsParent);
                copiedCurrentFilterKey = StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent);
            }
        }

        public static void Paste(StorageSettings storageSettings)
        {
            if (storageSettings.owner is IStoreSettingsParent storeSettingsParent)
            {
                if (!(copiedFilters is null))
                    StorageFiltersData.Filters.SetOrAdd(storeSettingsParent, copiedFilters);
                if (!(copiedMainFilterString is null))
                    StorageFiltersData.MainFilterString.SetOrAdd(storeSettingsParent, copiedMainFilterString);
                if (!(copiedCurrentFilterKey is null))
                    StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, copiedCurrentFilterKey);
            }
        }
    }
}