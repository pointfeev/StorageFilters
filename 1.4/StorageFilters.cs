﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using StorageFilters.Dialogs;
using StorageFilters.Utilities;
using UnityEngine;
using Verse;
using Verse.AI;

namespace StorageFilters;

public static class StorageFilters
{
    public static Rect? StorageTabRect;

    private static ExtraThingFilters copiedFilters;
    private static string copiedMainFilterString;
    private static string copiedCurrentFilterKey;

    private static readonly HashSet<int> ThingIDsReturned = new();

    private static readonly HashSet<Thing> ThingsAllowed = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StorageGroup GetStorageGroup(this IStoreSettingsParent owner) => owner as StorageGroup ?? (owner as IStorageGroupMember)?.Group;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IStoreSettingsParent GetStorageGroupOrSelf(this IStoreSettingsParent owner) => owner.GetStorageGroup() ?? owner;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StorageSettings GetStorageGroupSettings(this IStoreSettingsParent owner) => owner.GetStorageGroupOrSelf().GetStoreSettings();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IStoreSettingsParent GetStorageGroupOwner(this IStoreSettingsParent owner)
        => owner.GetStorageGroup()?.members?.FirstOrFallback() as IStoreSettingsParent ?? owner;

    public static Rect? FillTab(ITab_Storage instance, Vector2 size)
    {
        IStoreSettingsParent owner = GenUtils.GetSelectedStoreSettingsParent().GetStorageGroupOwner();
        if (owner is null)
        {
            Log.Warning("ASF_ModPrefix".Translate() + "ASF_StoreSettingsParentError".Translate());
            return null;
        }
        if (owner is not ISlotGroupParent) //&& !(owner is Blueprint_Storage)) need to add support for migrating settings when blueprint finishes
            return null;
        ExtraThingFilters tabFilters = StorageFiltersData.GetExtraThingFilters(owner);
        if (tabFilters is null)
        {
            tabFilters = new();
            StorageFiltersData.SetExtraThingFilters(owner, tabFilters);
        }
        string mainFilterString = StorageFiltersData.GetMainFilterName(owner);
        if (mainFilterString is null)
        {
            mainFilterString = StorageFiltersData.DefaultMainFilterString;
            StorageFiltersData.SetMainFilterName(owner, mainFilterString);
        }
        string tabFilter = StorageFiltersData.GetCurrentFilterKey(owner);
        if (tabFilter is null)
        {
            tabFilter = mainFilterString;
            StorageFiltersData.SetCurrentFilterKey(owner, mainFilterString);
            StorageFiltersData.SetCurrentFilterDepth(owner, 0);
        }
        Rect window = new(0, 0, size.x, size.y);
        StorageTabRect = window;
        GUI.BeginGroup(window.ContractedBy(10f));
        Rect position = new(166f, 0, Math.Min(Text.CalcSize(tabFilter).x + 16f, StorageFiltersData.MaxFilterStringWidth), 29f);
        GenUtils.FilterSelectionButton(instance, owner, tabFilters, mainFilterString, tabFilter, position);
        GUI.EndGroup();
        return position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ThingFilter GetCurrentFilter(ThingFilter filter = null, ThingFilter parentFilter = null)
    {
        IStoreSettingsParent storeGroupParent = GenUtils.GetSelectedStoreSettingsParent()?.GetStorageGroupOwner();
        StorageSettings settings = storeGroupParent?.GetStoreSettings();
        if (settings == null || filter != null && settings.filter != filter && parentFilter != null && settings.filter != parentFilter)
            return filter;
        ExtraThingFilters tabFilters = StorageFiltersData.GetExtraThingFilters(storeGroupParent);
        string tabFilter = StorageFiltersData.GetCurrentFilterKey(storeGroupParent);
        int tabFilterDepth = Find.WindowStack?.WindowOfType<Dialog_EditFilter>() is not null ? StorageFiltersData.GetCurrentFilterDepth(storeGroupParent) : 0;
        if (tabFilters is null || tabFilter is null)
            return filter ?? settings.filter;
        ExtraThingFilter extraFilter = tabFilters.Get(tabFilter);
        while (extraFilter is not null && extraFilter.FilterDepth < tabFilterDepth)
            extraFilter = extraFilter.NextInPriorityFilter;
        return extraFilter ?? filter ?? settings.filter;
    }

    public static void DoThingFilterConfigWindow(ref ThingFilter filter, ThingFilter parentFilter = null) => filter = GetCurrentFilter(filter, parentFilter);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetStackLimitsForThing(this IStoreSettingsParent owner, Thing thing, out int stackCountLimit, out int stackSizeLimit,
        ExtraThingFilters extraFilters = null)
    {
        stackCountLimit = 0;
        stackSizeLimit = 0;
        if (owner.GetStorageGroupSettings().filter.Allows(thing))
            return;
        extraFilters ??= StorageFiltersData.GetExtraThingFilters(owner);
        if (extraFilters is not null && extraFilters.Count > 0)
            foreach (ExtraThingFilter extraFilter in extraFilters.Filters.Where(f => f.Enabled))
            {
                bool applicable = extraFilter.Allows(thing);
                if (!applicable)
                {
                    ExtraThingFilter currentFilter = extraFilter;
                    while (currentFilter is not null)
                    {
                        if (currentFilter.Allows(thing))
                        {
                            applicable = true;
                            break;
                        }
                        currentFilter = currentFilter.NextInPriorityFilter;
                    }
                }
                if (!applicable)
                    continue;
                //stackCountLimit += extraFilter.StackCountLimit;
                stackSizeLimit += extraFilter.StackSizeLimit;
            }
        stackSizeLimit = Math.Min(stackSizeLimit, thing.def.stackLimit);
    }

    private static bool IsCurrentDestinationEqualTo(this Thing thing, IStoreSettingsParent owner, IStoreSettingsParent haulDestination = null)
    {
        haulDestination ??= StoreUtility.CurrentHaulDestinationOf(thing);
        return haulDestination is not null && haulDestination.GetStorageGroupOrSelf() == owner.GetStorageGroupOrSelf();
    }

    private static bool IsCurrentDestinationEqualToOrWorseThan(this Thing thing, IStoreSettingsParent owner)
    {
        IStoreSettingsParent haulDestination = StoreUtility.CurrentHaulDestinationOf(thing);
        if (haulDestination is null)
            return false;
        if (thing.IsCurrentDestinationEqualTo(owner, haulDestination))
            return true;
        return (haulDestination.GetStorageGroupSettings()?.Priority ?? StoragePriority.Unstored)
             < (owner.GetStorageGroupSettings()?.Priority ?? StoragePriority.Unstored);
    }

    private static IEnumerable<Thing> GetThingsForStorage(this Map map, IStoreSettingsParent owner, bool destEqualTo = false,
        bool destEqualToOrWorseThan = false)
    {
        IStoreSettingsParent storageGroup = owner.GetStorageGroupOrSelf();
        switch (storageGroup)
        {
            case StorageGroup group:
            {
                ThingIDsReturned.Clear();
                foreach (IStorageGroupMember member in group.members)
                    if (member is ISlotGroupParent memberSlotGroupParent)
                        foreach (IntVec3 position in memberSlotGroupParent.AllSlotCells())
                            foreach (Thing thing in map.thingGrid.ThingsAt(position))
                                if (ThingIDsReturned.Add(thing.thingIDNumber))
                                    yield return thing;
                break;
            }
            case ISlotGroupParent slotGroupParent:
            {
                ThingIDsReturned.Clear();
                foreach (IntVec3 position in slotGroupParent.AllSlotCells())
                    foreach (Thing thing in map.thingGrid.ThingsAt(position))
                        if (ThingIDsReturned.Add(thing.thingIDNumber))
                            yield return thing;
                break;
            }
            default:
                yield break;
        }
        if (destEqualToOrWorseThan)
        {
            foreach (Thing thing in map.listerHaulables.ThingsPotentiallyNeedingHauling()
                                       .Where(t => !t.IsForbidden(Faction.OfPlayer) && t.IsCurrentDestinationEqualToOrWorseThan(storageGroup)))
                if (ThingIDsReturned.Add(thing.thingIDNumber))
                    yield return thing;
        }
        else if (destEqualTo)
            foreach (Thing thing in map.listerHaulables.ThingsPotentiallyNeedingHauling()
                                       .Where(t => !t.IsForbidden(Faction.OfPlayer) && t.IsCurrentDestinationEqualTo(storageGroup)))
                if (ThingIDsReturned.Add(thing.thingIDNumber))
                    yield return thing;
    }

    public static void AllowedToAccept(StorageSettings settings, Thing thing, ref bool result)
    {
        if (result)
            return;
        IStoreSettingsParent owner = settings.owner.GetStorageGroupOwner();
        if (StorageFiltersData.GetExtraThingFilters(owner) is not { Count: > 0 } extraFilters)
            return;
        Map map;
        int maxThings = 0;
        switch (settings.owner)
        {
            case StorageGroup group:
            {
                map = group.Map;
                if (map == null)
                    break;
                foreach (IStorageGroupMember member in group.members)
                    if (member is ISlotGroupParent memberSlotGroupParent)
                        maxThings += memberSlotGroupParent.AllSlotCells().Sum(cell => cell.GetMaxItemsAllowedInCell(map));
                break;
            }
            case ISlotGroupParent slotGroupParent:
                map = slotGroupParent.Map;
                if (map == null)
                    return;
                maxThings = slotGroupParent.AllSlotCells().Sum(c => c.GetMaxItemsAllowedInCell(map));
                break;
            default:
                return;
        }
        owner.GetStackLimitsForThing(thing, out _, out int stackSizeLimit, extraFilters);
        ThingsAllowed.Clear();
        foreach (ExtraThingFilter extraFilter in extraFilters.Filters)
            if (extraFilter.Enabled && extraFilter is { } currentFilter)
                while (currentFilter is not null)
                    if (result = currentFilter.Allows(thing))
                    {
                        if (currentFilter == extraFilter)
                            return; // is NIPF
                        if (ThingsAllowed.Count < maxThings)
                            foreach (Thing t in map.GetThingsForStorage(owner, true).Where(t => t != thing && currentFilter.Allows(t)))
                                if (ThingsAllowed.Add(t) && ThingsAllowed.Count >= maxThings)
                                    break;
                        if (ThingsAllowed.Count >= maxThings && !ThingsAllowed.Any(t
                                => t.stackCount < Math.Min(t.def.stackLimit, stackSizeLimit > 0 ? stackSizeLimit : int.MaxValue) && t.CanStackWith(thing)))
                            result = false;
                        return;
                    }
                    else if (currentFilter.NextInPriorityFilter is { } nextFilter)
                    {
                        if (ThingsAllowed.Count < maxThings)
                            foreach (Thing t in map.GetThingsForStorage(owner, true, true).Where(t => currentFilter.Allows(t)))
                                if (ThingsAllowed.Add(t) && ThingsAllowed.Count >= maxThings)
                                    break;
                        if (ThingsAllowed.Count >= maxThings)
                            break; // do not consider the NIPF
                        currentFilter = nextFilter;
                    }
                    else
                        break;
    }

    public static void HaulToStorageJob(Thing thing, ref Job job)
    {
        if (job is null || thing.Map is not { } map)
            return;
        IntVec3 destination;
        if (job.def == JobDefOf.HaulToContainer && job.targetB.Thing is { } t)
            destination = t.Position;
        else if (job.targetB.Cell is { } cell)
            destination = cell;
        else
            return;
        if (thing.GetSlotGroup()?.parent?.GetStorageGroupOwner() is { } ownerFrom)
        {
            ownerFrom.GetStackLimitsForThing(thing, out _, out int stackSizeLimitFrom);
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
        if (destination.GetSlotGroup(map)?.parent?.GetStorageGroupOwner() is not { } ownerTo)
            return;
        ownerTo.GetStackLimitsForThing(thing, out _, out int stackSizeLimitTo);
        if (stackSizeLimitTo <= 0)
            return;
        int shouldPlace = stackSizeLimitTo - (job.targetB.Thing?.stackCount
                                           ?? map.thingGrid.ThingsAt(destination).Where(th => th.CanStackWith(thing)).Sum(th => th.stackCount));
        if (shouldPlace <= 0)
        {
            job = null;
            return;
        }
        job.count = Math.Min(job.count, shouldPlace);
    }

    public static void NoStorageBlockersIn(IntVec3 position, Map map, Thing thing, ref bool result)
    {
        if (!result || position.GetSlotGroup(map)?.parent?.GetStorageGroupOwner() is not { } owner)
            return;
        owner.GetStackLimitsForThing(thing, out _, out int stackSizeLimit);
        if (stackSizeLimit > 0 && map.thingGrid.ThingsAt(position).Any(t => t.CanStackWith(thing) && t.stackCount >= stackSizeLimit))
            result = false; // return;
        /*if (stackCountLimit <= 0)
            return;
        int stackCount = 0;
        foreach (Thing t in map.GetThingsForStorage(owner))
        {
            if (!t.CanStackWith(thing))
                continue;
            stackCount++;
            if (stackCount >= stackCountLimit)
                break;
        }
        if (stackCount >= stackCountLimit)
            result = false;*/
    }

    public static void TryFindBestBetterStoreCellFor(Thing thing, Map map, ref StoragePriority currentPriority)
    {
        if (currentPriority == StoragePriority.Unstored || thing.GetSlotGroup()?.parent?.GetStorageGroupOwner() is not { } owner)
            return;
        owner.GetStackLimitsForThing(thing, out _, out int stackSizeLimit);
        if (stackSizeLimit > 0 && thing.stackCount > stackSizeLimit)
            currentPriority = StoragePriority.Unstored; // return;
        /*if (stackCountLimit <= 0)
            return;
        int stackCount = 0;
        foreach (Thing t in map.GetThingsForStorage(owner))
        {
            if (!t.CanStackWith(thing))
                continue;
            stackCount++;
            if (stackCount > stackCountLimit)
                break;
        }
        if (stackCount > stackCountLimit)
            currentPriority = StoragePriority.Unstored;*/
    }

    public static void TryAbsorbStackNumToTake(Thing thing, Thing other, bool respectStackLimit, ref int result)
    {
        if (!respectStackLimit || thing.GetSlotGroup()?.parent?.GetStorageGroupOwner() is not { } owner)
            return;
        owner.GetStackLimitsForThing(thing, out _, out int stackSizeLimit);
        if (stackSizeLimit > 0)
            result = Math.Min(Math.Max(0, Math.Min(other.stackCount, stackSizeLimit - thing.stackCount)), thing.def.stackLimit);
    }

    public static void ShouldBeMergeable(Thing thing, ref bool result)
    {
        if (!result || thing.GetSlotGroup()?.parent?.GetStorageGroupOwner() is not { } owner)
            return;
        owner.GetStackLimitsForThing(thing, out _, out int stackSizeLimit);
        if (stackSizeLimit > 0 && thing.stackCount >= stackSizeLimit)
            result = false;
    }

    public static void Copy(StorageSettings storageSettings)
    {
        if (storageSettings.owner?.GetStorageGroupOwner() is not { } owner)
            return;
        if (StorageFiltersData.GetExtraThingFilters(owner) is { } filters)
        {
            copiedFilters = new();
            foreach (KeyValuePair<string, ExtraThingFilter> entry in filters)
                copiedFilters.Set(entry.Key, entry.Value.Copy());
        }
        copiedMainFilterString = StorageFiltersData.GetMainFilterName(owner);
        copiedCurrentFilterKey = StorageFiltersData.GetCurrentFilterKey(owner);
    }

    public static void Paste(StorageSettings storageSettings)
    {
        if (storageSettings.owner?.GetStorageGroupOwner() is not { } owner)
            return;
        if (copiedFilters is not null)
            StorageFiltersData.SetExtraThingFilters(owner, copiedFilters);
        if (copiedMainFilterString is not null)
            StorageFiltersData.SetMainFilterName(owner, copiedMainFilterString);
        if (copiedCurrentFilterKey is not null)
            StorageFiltersData.SetCurrentFilterKey(owner, copiedCurrentFilterKey);
    }
}