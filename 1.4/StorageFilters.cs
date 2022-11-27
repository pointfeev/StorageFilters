using RimWorld;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

using Verse;
using Verse.AI;

namespace StorageFilters
{
    public static class StorageFilters
    {
        public static Rect? StorageTabRect = null;

        private static MethodInfo get_TopAreaHeight;
        public static Rect? FillTab(ITab_Storage instance, Vector2 size)
        {
            try
            {
                if (get_TopAreaHeight is null)
                    get_TopAreaHeight = typeof(ITab_Storage).GetMethod("get_TopAreaHeight", (BindingFlags)(-1));
                _ = (float)get_TopAreaHeight.Invoke(instance, new object[] { });
            }
            catch
            {
                return null;
            }
            IStoreSettingsParent storeSettingsParent = GenUtils.GetSelectedStoreSettingsParent();
            if (storeSettingsParent is null)
            {
                Log.Warning("ASF_ModPrefix".Translate() + "ASF_StoreSettingsParentError".Translate());
                return null;
            }
            if (!(storeSettingsParent is IHaulDestination) || !(storeSettingsParent is ISlotGroupParent))
                return null;
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
            Rect position = new Rect(166f, 0, Math.Min(Text.CalcSize(tabFilter).x + 16f, StorageFiltersData.MaxFilterStringWidth), 29f);
            GenUtils.FilterSelectionButton(instance, storeSettingsParent, tabFilters, mainFilterString, tabFilter, position);
            GUI.EndGroup();
            return position;
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

        public static void GetStackLimitsForThing(IStoreSettingsParent owner, Thing thing, out int stackCountLimit, out int stackSizeLimit, ExtraThingFilters extraFilters = null)
        {
            stackCountLimit = 0;
            stackSizeLimit = 0;
            if (owner.GetStoreSettings().filter.Allows(thing)) return;
            if (extraFilters is null)
                extraFilters = StorageFiltersData.Filters.TryGetValue(owner);
            if (!(extraFilters is null) && extraFilters.Count > 0)
                foreach (ExtraThingFilter extraFilter in extraFilters.Values.Where(f => f.Enabled))
                {
                    bool applicable = extraFilter.Allows(thing);
                    if (!applicable)
                    {
                        ExtraThingFilter currentFilter = extraFilter;
                        while (!(currentFilter is null))
                        {
                            if (currentFilter.Allows(thing))
                            {
                                applicable = true;
                                break;
                            }
                            currentFilter = currentFilter.NextInPriorityFilter;
                        }
                    }
                    if (applicable)
                    {
                        //stackCountLimit += extraFilter.StackCountLimit;
                        stackSizeLimit += extraFilter.StackSizeLimit;
                    }
                }
            stackSizeLimit = Math.Min(stackSizeLimit, thing.def.stackLimit);
        }

        public static bool IsCurrentDestinationEqualTo(this Thing thing, IStoreSettingsParent owner)
            => !(StoreUtility.CurrentHaulDestinationOf(thing) is IHaulDestination haulDestination) || haulDestination == owner;

        public static bool IsCurrentDestinationEqualToOrWorseThan(this Thing thing, IStoreSettingsParent owner) => thing.IsCurrentDestinationEqualTo(owner)
            || (StoreUtility.CurrentHaulDestinationOf(thing).GetStoreSettings()?.Priority ?? StoragePriority.Unstored) < (owner.GetStoreSettings()?.Priority ?? StoragePriority.Unstored);

        private static IEnumerable<Thing> haulables = null;
        private static readonly Dictionary<Thing, bool> shouldStore = new Dictionary<Thing, bool>();
        public static bool AllowedToAccept(IStoreSettingsParent owner, ThingFilter filter, Thing thing, ref bool result)
        {
            if (!(owner is ISlotGroupParent slotGroupParent) || !((owner as IHaulDestination)?.Map is Map map)) return true;
            result = filter.Allows(thing);
            if (owner is null || result && (!(owner.GetParentStoreSettings() is StorageSettings parentStoreSettings) || parentStoreSettings.AllowedToAccept(thing)))
                return false;
            result = false;
            if (StorageFiltersData.Filters.TryGetValue(owner) is ExtraThingFilters extraFilters && extraFilters.Count > 0)
            {
                GetStackLimitsForThing(owner, thing, out _, out int stackSizeLimit, extraFilters);
                int cellCount = slotGroupParent.AllSlotCells()?.Count() ?? 0;
                shouldStore.Clear();
                foreach (ExtraThingFilter extraFilter in extraFilters.Values)
                    if (extraFilter.Enabled && extraFilter is ExtraThingFilter currentFilter)
                        while (!(currentFilter is null))
                            if (result = currentFilter.Allows(thing))
                            {
                                if (currentFilter != extraFilter) // is NIPF
                                {
                                    if (haulables is null)
                                        haulables = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways);
                                    foreach (Thing _thing in haulables.Where(_thing => !_thing.IsForbidden(Faction.OfPlayer)
                                        && currentFilter.Allows(_thing) && _thing.IsCurrentDestinationEqualTo(owner)))
                                    {
                                        if (_thing == thing)
                                            continue;
                                        shouldStore[_thing] = true;
                                        if (shouldStore.Count >= cellCount)
                                            break;
                                    }
                                    if (shouldStore.Count >= cellCount && !shouldStore.Any(_thing
                                        => _thing.Key.stackCount <
                                            (stackSizeLimit <= 0 ? _thing.Key.def.stackLimit : Math.Min(_thing.Key.def.stackLimit, stackSizeLimit))
                                        && _thing.Key.CanStackWith(thing)))
                                        result = false;
                                }
                                return false;
                            }
                            else if (currentFilter.NextInPriorityFilter is ExtraThingFilter nextFilter)
                            {
                                if (haulables is null)
                                    haulables = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways);
                                foreach (Thing _thing in haulables.Where(_thing => !_thing.IsForbidden(Faction.OfPlayer)
                                    && currentFilter.Allows(_thing) && _thing.IsCurrentDestinationEqualToOrWorseThan(owner)))
                                {
                                    shouldStore[_thing] = true;
                                    if (shouldStore.Count >= cellCount)
                                        break;
                                }
                                if (shouldStore.Count >= cellCount)
                                    break; // do not consider the NIPF
                                else currentFilter = nextFilter;
                            }
                            else break;
            }
            return false;
        }

        public static void HaulToStorageJob(Thing thing, ref Job job)
        {
            if (job is null || !(thing.Map is Map map)) return;
            IntVec3 destination;
            if (job.def == JobDefOf.HaulToContainer && job.targetB.Thing is Thing _thing)
                destination = _thing.Position;
            else if (job.targetB.Cell is IntVec3 cell)
                destination = cell;
            else return;
            if (thing.GetSlotGroup()?.parent is IStoreSettingsParent ownerFrom)
            {
                GetStackLimitsForThing(ownerFrom, thing, out _, out int stackSizeLimitFrom);
                if (stackSizeLimitFrom > 0)
                {
                    int shouldTake = thing.stackCount - stackSizeLimitFrom;
                    if (shouldTake <= 0)
                    {
                        job = null;
                        return;
                    }
                    job.count = Math.Min(job.count, shouldTake);
                }
            }
            if (!(destination.GetSlotGroup(map)?.parent is IStoreSettingsParent ownerTo)) return;
            GetStackLimitsForThing(ownerTo, thing, out _, out int stackSizeLimitTo);
            if (stackSizeLimitTo > 0)
            {
                int amountAt = 0;
                foreach (Thing __thing in map.thingGrid.ThingsListAt(destination))
                    if (__thing.CanStackWith(thing))
                        amountAt += __thing.stackCount;
                int shouldPlace = stackSizeLimitTo - amountAt;
                if (shouldPlace <= 0)
                {
                    job = null;
                    return;
                }
                job.count = Math.Min(job.count, stackSizeLimitTo - amountAt);
            }
        }

        public static void NoStorageBlockersIn(IntVec3 position, Map map, Thing thing, ref bool result)
        {
            if (!result || !(position.GetSlotGroup(map)?.parent is IStoreSettingsParent owner)) return;
            GetStackLimitsForThing(owner, thing, out _, out int stackSizeLimit);
            if (stackSizeLimit > 0)
                foreach (Thing _thing in map.thingGrid.ThingsListAt(position))
                    if (_thing.CanStackWith(thing) && _thing.stackCount >= stackSizeLimit)
                    {
                        result = false;
                        return;
                    }
        }

        public static void PlaceSpotQualityAt(Thing thing, bool allowStacking, ref object result)
        {
            if (!allowStacking || !(thing.GetSlotGroup()?.parent is IStoreSettingsParent owner)) return;
            GetStackLimitsForThing(owner, thing, out _, out int stackSizeLimit);
            if (stackSizeLimit > 0 && thing.stackCount >= stackSizeLimit)
                result = 0;
        }

        public static void TryFindBestBetterStoreCellFor(Thing thing, ref StoragePriority currentPriority)
        {
            if (!(thing.GetSlotGroup()?.parent is IStoreSettingsParent owner)) return;
            GetStackLimitsForThing(owner, thing, out _, out int stackSizeLimit);
            if (stackSizeLimit > 0 && thing.stackCount > stackSizeLimit)
                currentPriority = StoragePriority.Unstored;
        }

        public static void TryAbsorbStackNumToTake(Thing thing, Thing other, bool respectStackLimit, ref int result)
        {
            if (!respectStackLimit || !(thing.GetSlotGroup()?.parent is IStoreSettingsParent owner)) return;
            GetStackLimitsForThing(owner, thing, out _, out int stackSizeLimit);
            if (stackSizeLimit > 0)
                result = Math.Min(Math.Max(0, Math.Min(other.stackCount, stackSizeLimit - thing.stackCount)), thing.def.stackLimit);
        }

        public static void ShouldBeMergeable(Thing thing, ref bool result)
        {
            if (!result || !(thing.GetSlotGroup()?.parent is IStoreSettingsParent owner)) return;
            GetStackLimitsForThing(owner, thing, out _, out int stackSizeLimit);
            if (stackSizeLimit > 0 && thing.stackCount >= stackSizeLimit)
                result = false;
        }

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