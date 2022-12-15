using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using StorageFilters.Dialogs;
using StorageFilters.Utilities;
using UnityEngine;
using Verse;
using Verse.AI;

namespace StorageFilters
{
    public static class StorageFilters
    {
        public static Rect? StorageTabRect;

        private static MethodInfo getTopAreaHeight;

        private static IEnumerable<Thing> haulables;
        private static readonly Dictionary<Thing, bool> ShouldStore = new Dictionary<Thing, bool>();

        private static ExtraThingFilters copiedFilters;
        private static string copiedMainFilterString;
        private static string copiedCurrentFilterKey;

        private static StorageGroup GetStorageGroup(this IStoreSettingsParent owner)
            => (owner as IStorageGroupMember)?.Group;

        private static IStoreSettingsParent GetStorageGroupParent(this IStoreSettingsParent owner)
            => owner.GetStorageGroup()?.GetParentStoreSettings()?.owner ?? owner;

        public static Rect? FillTab(ITab_Storage instance, Vector2 size)
        {
            try
            {
                if (getTopAreaHeight is null)
                    getTopAreaHeight = typeof(ITab_Storage).GetMethod("get_TopAreaHeight", (BindingFlags)(-1));
                _ = (float)getTopAreaHeight?.Invoke(instance, new object[] { });
            }
            catch
            {
                return null;
            }

            IStoreSettingsParent storeGroupParent = GenUtils.GetSelectedStoreSettingsParent().GetStorageGroupParent();
            if (storeGroupParent is null)
            {
                Log.Warning("ASF_ModPrefix".Translate() + "ASF_StoreSettingsParentError".Translate());
                return null;
            }

            if (!(storeGroupParent is IHaulDestination) || !(storeGroupParent is ISlotGroupParent))
                return null;
            ExtraThingFilters tabFilters = StorageFiltersData.Filters.TryGetValue(storeGroupParent);
            if (tabFilters is null)
            {
                StorageFiltersData.Filters.SetOrAdd(storeGroupParent, new ExtraThingFilters());
                tabFilters = StorageFiltersData.Filters.TryGetValue(storeGroupParent);
            }

            string mainFilterString = StorageFiltersData.MainFilterString.TryGetValue(storeGroupParent);
            if (mainFilterString is null)
            {
                StorageFiltersData.MainFilterString.SetOrAdd(storeGroupParent,
                                                             StorageFiltersData.DefaultMainFilterString);
                mainFilterString = StorageFiltersData.MainFilterString.TryGetValue(storeGroupParent);
            }

            string tabFilter = StorageFiltersData.CurrentFilterKey.TryGetValue(storeGroupParent);
            if (tabFilter is null)
            {
                StorageFiltersData.CurrentFilterKey.SetOrAdd(storeGroupParent, mainFilterString);
                StorageFiltersData.CurrentFilterDepth.SetOrAdd(storeGroupParent, 0);
                tabFilter = StorageFiltersData.CurrentFilterKey.TryGetValue(storeGroupParent);
            }

            Rect window = new Rect(0, 0, size.x, size.y);
            StorageTabRect = window;
            GUI.BeginGroup(window.ContractedBy(10f));
            Rect position = new Rect(166f, 0,
                                     Math.Min(Text.CalcSize(tabFilter).x + 16f,
                                              StorageFiltersData.MaxFilterStringWidth),
                                     29f);
            GenUtils.FilterSelectionButton(instance, storeGroupParent, tabFilters, mainFilterString, tabFilter,
                                           position);
            GUI.EndGroup();
            return position;
        }

        public static void DoThingFilterConfigWindow(ref ThingFilter filter, ThingFilter parentFilter)
        {
            IStoreSettingsParent storeGroupParent = GenUtils.GetSelectedStoreSettingsParent().GetStorageGroupParent();
            StorageSettings settings = storeGroupParent?.GetStoreSettings();
            if (settings is null || (settings.filter != filter && settings.filter != parentFilter))
                return;
            ExtraThingFilters tabFilters = StorageFiltersData.Filters.TryGetValue(storeGroupParent);
            string tabFilter = StorageFiltersData.CurrentFilterKey.TryGetValue(storeGroupParent);
            int tabFilterDepth = !(Find.WindowStack.WindowOfType<Dialog_EditFilter>() is null)
                ? StorageFiltersData.CurrentFilterDepth.TryGetValue(storeGroupParent)
                : 0;
            if (tabFilters is null || tabFilter is null)
                return;
            ExtraThingFilter extraFilter = tabFilters.Get(tabFilter);
            while (!(extraFilter is null) && extraFilter.FilterDepth < tabFilterDepth)
                extraFilter = extraFilter.NextInPriorityFilter;
            if (!(extraFilter is null))
                filter = extraFilter;
        }

        public static void GetStackLimitsForThing(IStoreSettingsParent owner, Thing thing, out int stackCountLimit,
                                                  out int stackSizeLimit, ExtraThingFilters extraFilters = null)
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
                        //stackCountLimit += extraFilter.StackCountLimit;
                        stackSizeLimit += extraFilter.StackSizeLimit;
                }

            stackSizeLimit = Math.Min(stackSizeLimit, thing.def.stackLimit);
        }

        private static bool IsCurrentDestinationEqualTo(this Thing thing, IStoreSettingsParent owner,
                                                        IStoreSettingsParent haulDestination = null)
        {
            haulDestination = haulDestination ?? StoreUtility.CurrentHaulDestinationOf(thing);
            return !(haulDestination is null) && (owner.GetStorageGroup() == haulDestination.GetStorageGroup() ||
                                                  haulDestination == owner);
        }

        private static bool IsCurrentDestinationEqualToOrWorseThan(this Thing thing, IStoreSettingsParent owner)
        {
            IStoreSettingsParent haulDestination = StoreUtility.CurrentHaulDestinationOf(thing);
            if (haulDestination is null)
                return false;
            if (thing.IsCurrentDestinationEqualTo(owner, haulDestination))
                return true;
            StoragePriority priority = haulDestination.GetStorageGroup()?.GetStoreSettings()?.Priority ??
                                       haulDestination.GetStoreSettings()?.Priority ?? StoragePriority.Unstored;
            StoragePriority destinationPriority = owner.GetStorageGroup()?.GetStoreSettings()?.Priority ??
                                                  owner.GetStoreSettings()?.Priority ?? StoragePriority.Unstored;
            return priority < destinationPriority;
        }

        public static bool AllowedToAccept(IStoreSettingsParent owner, ThingFilter filter, Thing thing, ref bool result)
        {
            if (!(owner is ISlotGroupParent slotGroupParent) || !(slotGroupParent.Map is Map map))
                return true;
            result = filter.Allows(thing);
            if (result && (!(slotGroupParent.GetParentStoreSettings() is StorageSettings parentStoreSettings) ||
                           parentStoreSettings.AllowedToAccept(thing)))
                return false;
            result = false;
            IStoreSettingsParent storeGroupParent = owner.GetStorageGroupParent();
            if (!(StorageFiltersData.Filters.TryGetValue(storeGroupParent) is ExtraThingFilters extraFilters) ||
                extraFilters.Count <= 0)
                return false;
            GetStackLimitsForThing(storeGroupParent, thing, out _, out int stackSizeLimit, extraFilters);
            int cellCount = storeGroupParent.GetStorageGroup() is StorageGroup group
                ? group.CellsList.Count
                : slotGroupParent.AllSlotCells()?.Count() ?? 0;
            ShouldStore.Clear();
            foreach (ExtraThingFilter extraFilter in extraFilters.Values)
                if (extraFilter.Enabled && extraFilter is ExtraThingFilter currentFilter)
                    while (!(currentFilter is null))
                        if (result = currentFilter.Allows(thing))
                        {
                            if (currentFilter == extraFilter)
                                return false; // is NIPF
                            if (haulables is null)
                                haulables = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways);
                            foreach (Thing t in haulables.Where(t => !t.IsForbidden(Faction.OfPlayer)
                                                                  && currentFilter.Allows(t) &&
                                                                     t.IsCurrentDestinationEqualTo(storeGroupParent)))
                            {
                                if (t == thing)
                                    continue;
                                ShouldStore[t] = true;
                                if (ShouldStore.Count >= cellCount)
                                    break;
                            }

                            if (ShouldStore.Count >= cellCount && !ShouldStore.Any(t
                                    => t.Key.stackCount <
                                       (stackSizeLimit <= 0
                                           ? t.Key.def.stackLimit
                                           : Math.Min(t.Key.def.stackLimit, stackSizeLimit))
                                    && t.Key.CanStackWith(thing)))
                                result = false;
                            return false;
                        }
                        else if (currentFilter.NextInPriorityFilter is ExtraThingFilter nextFilter)
                        {
                            if (haulables is null)
                                haulables = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways);
                            foreach (Thing t in haulables.Where(t => !t.IsForbidden(Faction.OfPlayer)
                                                                  && currentFilter.Allows(t) &&
                                                                     t.IsCurrentDestinationEqualToOrWorseThan(
                                                                         storeGroupParent)))
                            {
                                ShouldStore[t] = true;
                                if (ShouldStore.Count >= cellCount)
                                    break;
                            }

                            if (ShouldStore.Count >= cellCount)
                                break; // do not consider the NIPF
                            currentFilter = nextFilter;
                        }
                        else
                        {
                            break;
                        }

            return false;
        }

        public static void HaulToStorageJob(Thing thing, ref Job job)
        {
            if (job is null || !(thing.Map is Map map)) return;
            IntVec3 destination;
            if (job.def == JobDefOf.HaulToContainer && job.targetB.Thing is Thing t)
                destination = t.Position;
            else if (job.targetB.Cell is IntVec3 cell)
                destination = cell;
            else
                return;
            if (thing.GetSlotGroup()?.parent is IStoreSettingsParent ownerFrom)
            {
                GetStackLimitsForThing(ownerFrom.GetStorageGroupParent(), thing, out _, out int stackSizeLimitFrom);
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
            GetStackLimitsForThing(ownerTo.GetStorageGroupParent(), thing, out _, out int stackSizeLimitTo);
            if (stackSizeLimitTo <= 0)
                return;
            int amountAt = map.thingGrid.ThingsListAt(destination).Where(th => th.CanStackWith(thing))
                              .Sum(th => th.stackCount);
            int shouldPlace = stackSizeLimitTo - amountAt;
            if (shouldPlace <= 0)
            {
                job = null;
                return;
            }

            job.count = Math.Min(job.count, stackSizeLimitTo - amountAt);
        }

        public static void NoStorageBlockersIn(IntVec3 position, Map map, Thing thing, ref bool result)
        {
            if (!result || !(position.GetSlotGroup(map)?.parent is IStoreSettingsParent owner)) return;
            GetStackLimitsForThing(owner.GetStorageGroupParent(), thing, out _, out int stackSizeLimit);
            if (stackSizeLimit <= 0)
                return;
            if (!Enumerable.Any(map.thingGrid.ThingsListAt(position),
                                t => t.CanStackWith(thing) && t.stackCount >= stackSizeLimit)) return;
            result = false;
        }

        public static void PlaceSpotQualityAt(Thing thing, bool allowStacking, ref object result)
        {
            if (!allowStacking || !(thing.GetSlotGroup()?.parent is IStoreSettingsParent owner)) return;
            GetStackLimitsForThing(owner.GetStorageGroupParent(), thing, out _, out int stackSizeLimit);
            if (stackSizeLimit > 0 && thing.stackCount >= stackSizeLimit)
                result = 0;
        }

        public static void TryFindBestBetterStoreCellFor(Thing thing, ref StoragePriority currentPriority)
        {
            if (!(thing.GetSlotGroup()?.parent is IStoreSettingsParent owner)) return;
            GetStackLimitsForThing(owner.GetStorageGroupParent(), thing, out _, out int stackSizeLimit);
            if (stackSizeLimit > 0 && thing.stackCount > stackSizeLimit)
                currentPriority = StoragePriority.Unstored;
        }

        public static void TryAbsorbStackNumToTake(Thing thing, Thing other, bool respectStackLimit, ref int result)
        {
            if (!respectStackLimit || !(thing.GetSlotGroup()?.parent is IStoreSettingsParent owner)) return;
            GetStackLimitsForThing(owner.GetStorageGroupParent(), thing, out _, out int stackSizeLimit);
            if (stackSizeLimit > 0)
                result = Math.Min(Math.Max(0, Math.Min(other.stackCount, stackSizeLimit - thing.stackCount)),
                                  thing.def.stackLimit);
        }

        public static void ShouldBeMergeable(Thing thing, ref bool result)
        {
            if (!result || !(thing.GetSlotGroup()?.parent is IStoreSettingsParent owner)) return;
            GetStackLimitsForThing(owner.GetStorageGroupParent(), thing, out _, out int stackSizeLimit);
            if (stackSizeLimit > 0 && thing.stackCount >= stackSizeLimit)
                result = false;
        }

        public static void Copy(StorageSettings storageSettings)
        {
            if (!(storageSettings.owner is IStoreSettingsParent owner))
                return;
            IStoreSettingsParent storeGroupParent = owner.GetStorageGroupParent();
            if (StorageFiltersData.Filters.TryGetValue(storeGroupParent) is ExtraThingFilters filters)
            {
                copiedFilters = new ExtraThingFilters();
                foreach (KeyValuePair<string, ExtraThingFilter> entry in filters)
                    copiedFilters.Set(entry.Key, entry.Value.Copy());
            }

            copiedMainFilterString = StorageFiltersData.MainFilterString.TryGetValue(storeGroupParent);
            copiedCurrentFilterKey = StorageFiltersData.CurrentFilterKey.TryGetValue(storeGroupParent);
        }

        public static void Paste(StorageSettings storageSettings)
        {
            if (!(storageSettings.owner is IStoreSettingsParent owner))
                return;
            IStoreSettingsParent storeGroupParent = owner.GetStorageGroupParent();
            if (!(copiedFilters is null))
                StorageFiltersData.Filters.SetOrAdd(storeGroupParent, copiedFilters);
            if (!(copiedMainFilterString is null))
                StorageFiltersData.MainFilterString.SetOrAdd(storeGroupParent, copiedMainFilterString);
            if (!(copiedCurrentFilterKey is null))
                StorageFiltersData.CurrentFilterKey.SetOrAdd(storeGroupParent, copiedCurrentFilterKey);
        }
    }
}