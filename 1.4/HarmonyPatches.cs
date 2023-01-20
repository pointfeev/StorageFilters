using System;
using HarmonyLib;
using RimWorld;
using StorageFilters.Utilities;
using UnityEngine;
using Verse;
using Verse.AI;

namespace StorageFilters;

[StaticConstructorOnStartup]
public static class HarmonyPatches
{
    static HarmonyPatches()
    {
        Harmony harmony = new("pointfeev.storagefilters");
        _ = harmony.Patch(AccessTools.Method(typeof(ITab_Storage), "FillTab"), postfix: new(typeof(HarmonyPatches), nameof(FillTab)));
        _ = harmony.Patch(AccessTools.Method(typeof(ITab_Storage), "get_TopAreaHeight"), postfix: new(typeof(HarmonyPatches), nameof(TopAreaHeight)));
        _ = harmony.Patch(AccessTools.Method(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow)),
            new(typeof(HarmonyPatches), nameof(DoThingFilterConfigWindow)));
        _ = harmony.Patch(AccessTools.Method(typeof(StorageSettings), nameof(StorageSettings.AllowedToAccept), new[] { typeof(Thing) }),
            postfix: new(typeof(HarmonyPatches), nameof(AllowedToAccept)));
        _ = harmony.Patch(AccessTools.Method(typeof(HaulAIUtility), nameof(HaulAIUtility.HaulToStorageJob)),
            postfix: new(typeof(HarmonyPatches), nameof(HaulToStorageJob)));
        _ = harmony.Patch(AccessTools.Method(typeof(StoreUtility), "NoStorageBlockersIn"), postfix: new(typeof(HarmonyPatches), nameof(NoStorageBlockersIn)));
        _ = harmony.Patch(AccessTools.Method(typeof(StoreUtility), nameof(StoreUtility.TryFindBestBetterStoreCellFor)),
            new(typeof(HarmonyPatches), nameof(TryFindBestBetterStoreCellFor)));
        _ = harmony.Patch(AccessTools.Method(typeof(StoreUtility), nameof(StoreUtility.TryFindBestBetterStoreCellForIn)),
            new(typeof(HarmonyPatches), nameof(TryFindBestBetterStoreCellForIn)));
        _ = harmony.Patch(AccessTools.Method(typeof(ThingUtility), nameof(ThingUtility.TryAbsorbStackNumToTake)),
            postfix: new(typeof(HarmonyPatches), nameof(TryAbsorbStackNumToTake)));
        _ = harmony.Patch(AccessTools.Method(typeof(ListerMergeables), "ShouldBeMergeable"), postfix: new(typeof(HarmonyPatches), nameof(ShouldBeMergeable)));
        _ = harmony.Patch(AccessTools.Method(typeof(StorageSettingsClipboard), nameof(StorageSettingsClipboard.Copy)),
            postfix: new(typeof(HarmonyPatches), nameof(Copy)));
        _ = harmony.Patch(AccessTools.Method(typeof(StorageSettingsClipboard), nameof(StorageSettingsClipboard.PasteInto)),
            postfix: new(typeof(HarmonyPatches), nameof(PasteInto)));
        _ = harmony.Patch(AccessTools.Method(typeof(Log), nameof(Log.Error), new[] { typeof(string) }), new(typeof(HarmonyPatches), nameof(Error)));
        harmony.PatchForMods();
    }

    public static void FillTab(ITab_Storage __instance, Vector2 ___size) => ModCompatibility.LastButton = StorageFilters.FillTab(__instance, ___size);

    public static void TopAreaHeight(float __result) => _ = Math.Max(__result, 35f);

    public static void DoThingFilterConfigWindow(ref ThingFilter filter, ThingFilter parentFilter)
        => StorageFilters.DoThingFilterConfigWindow(ref filter, parentFilter);

    public static void AllowedToAccept(Thing t, StorageSettings __instance, ref bool __result) => StorageFilters.AllowedToAccept(__instance, t, ref __result);

    public static void HaulToStorageJob(Thing t, ref Job __result) => StorageFilters.HaulToStorageJob(t, ref __result);

    public static void NoStorageBlockersIn(IntVec3 c, Map map, Thing thing, ref bool __result)
        => StorageFilters.NoStorageBlockersIn(c, map, thing, ref __result);

    public static void TryFindBestBetterStoreCellFor(Thing t, Map map, ref StoragePriority currentPriority)
        => StorageFilters.TryFindBestBetterStoreCellFor(t, map, ref currentPriority);

    public static void TryFindBestBetterStoreCellForIn(Thing t, Map map, ref StoragePriority currentPriority)
        => StorageFilters.TryFindBestBetterStoreCellFor(t, map, ref currentPriority);

    public static void ShouldBeMergeable(Thing t, ref bool __result) => StorageFilters.ShouldBeMergeable(t, ref __result);

    public static void TryAbsorbStackNumToTake(Thing thing, Thing other, bool respectStackLimit, ref int __result)
        => StorageFilters.TryAbsorbStackNumToTake(thing, other, respectStackLimit, ref __result);

    public static void Copy(StorageSettings s) => StorageFilters.Copy(s);

    public static void PasteInto(StorageSettings s) => StorageFilters.Paste(s);

    public static bool Error(string text)
        => !text.StartsWith("Could not load reference to ")
        && !ReflectionUtils.IsMethodInCallStack(AccessTools.Method(typeof(ExtraThingFilter), nameof(ExtraThingFilter.ExposeData)));
}