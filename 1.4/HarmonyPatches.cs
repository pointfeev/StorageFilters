using System;
using System.Reflection;
using CompatUtils;
using HarmonyLib;
using RimWorld;
using StorageFilters.Dialogs;
using StorageFilters.Utilities;
using UnityEngine;
using Verse;
using Verse.AI;

namespace StorageFilters
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly bool MaterialFilterActive;
        private static readonly Type MaterialFilterWindowType;
        private static readonly ConstructorInfo MaterialFilterWindowCtor;

        private static float oldMaxFilterStringWidth;
        private static Rect? lastButton;

        private static MethodInfo windowOfType;

        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("pointfeev.storagefilters");
            MaterialFilterActive = Compatibility.IsModActive("kamikatze.materialfilter");
            if (MaterialFilterActive)
            {
                MethodInfo drawFilterButton = Compatibility.GetConsistentMethod("kamikatze.materialfilter", "MaterialFilter.ITab_Storage_FillTab_Patch",
                    "drawFilterButton", new[] { typeof(ITab_Storage), typeof(Vector2) }, true);
                if (!(drawFilterButton is null) && AccessTools.TypeByName("MaterialFilter.MaterialFilterWindow") is Type type
                                                && AccessTools.Constructor(type,
                                                           new[] { typeof(ThingFilter), typeof(float), typeof(float), typeof(WindowLayer) }) is ConstructorInfo
                                                       ctor)
                {
                    _ = harmony.Patch(drawFilterButton, new HarmonyMethod(typeof(HarmonyPatches), nameof(DrawMaterialFilterButton)));
                    MaterialFilterWindowType = type;
                    MaterialFilterWindowCtor = ctor;
                }
            }
            bool pickUpAndHaulActive = Compatibility.IsModActive("mehni.pickupandhaul");
            if (pickUpAndHaulActive)
            {
                MethodInfo capacityAt = Compatibility.GetConsistentMethod("mehni.pickupandhaul", "PickUpAndHaul.WorkGiver_HaulToInventory", "CapacityAt",
                    new[] { typeof(Thing), typeof(IntVec3), typeof(Map) }, true);
                if (!(capacityAt is null))
                    _ = harmony.Patch(capacityAt, new HarmonyMethod(typeof(HarmonyPatches), nameof(CapacityAt)));
                MethodInfo tryFindBestBetterStoreCellFor = Compatibility.GetConsistentMethod("mehni.pickupandhaul", "PickUpAndHaul.WorkGiver_HaulToInventory",
                    "TryFindBestBetterStoreCellFor",
                    new[] { typeof(Thing), typeof(Pawn), typeof(Map), typeof(StoragePriority), typeof(Faction), typeof(IntVec3) }, true);
                if (!(tryFindBestBetterStoreCellFor is null))
                    _ = harmony.Patch(tryFindBestBetterStoreCellFor, new HarmonyMethod(typeof(HarmonyPatches), nameof(PUAH_TryFindBestBetterStoreCellFor)));
            }
            _ = harmony.Patch(AccessTools.Method(typeof(ITab_Storage), "FillTab"), postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(FillTab)));
            _ = harmony.Patch(AccessTools.Method(typeof(ITab_Storage), "get_TopAreaHeight"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(TopAreaHeight)));
            _ = harmony.Patch(AccessTools.Method(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow)),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(DoThingFilterConfigWindow)));
            _ = harmony.Patch(AccessTools.Method(typeof(StorageSettings), nameof(StorageSettings.AllowedToAccept), new[] { typeof(Thing) }),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(AllowedToAccept)));
            _ = harmony.Patch(AccessTools.Method(typeof(HaulAIUtility), nameof(HaulAIUtility.HaulToStorageJob)),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HaulToStorageJob)));
            _ = harmony.Patch(AccessTools.Method(typeof(StoreUtility), "NoStorageBlockersIn"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(NoStorageBlockersIn)));
            _ = harmony.Patch(AccessTools.Method(typeof(StoreUtility), nameof(StoreUtility.TryFindBestBetterStoreCellFor)),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(TryFindBestBetterStoreCellFor)));
            _ = harmony.Patch(AccessTools.Method(typeof(StoreUtility), nameof(StoreUtility.TryFindBestBetterStoreCellForIn)),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(TryFindBestBetterStoreCellForIn)));
            _ = harmony.Patch(AccessTools.Method(typeof(ThingUtility), nameof(ThingUtility.TryAbsorbStackNumToTake)),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(TryAbsorbStackNumToTake)));
            _ = harmony.Patch(AccessTools.Method(typeof(ListerMergeables), "ShouldBeMergeable"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ShouldBeMergeable)));
            _ = harmony.Patch(AccessTools.Method(typeof(StorageSettingsClipboard), nameof(StorageSettingsClipboard.Copy)),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Copy)));
            _ = harmony.Patch(AccessTools.Method(typeof(StorageSettingsClipboard), nameof(StorageSettingsClipboard.PasteInto)),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(PasteInto)));
            _ = harmony.Patch(AccessTools.Method(typeof(Log), nameof(Log.Error), new[] { typeof(string) }),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(Error)));
        }

        public static void SetMaterialFilterWindowActive(ThingFilter filter = null, Vector2 position = default, bool toggle = true, bool active = false)
        {
            if (!MaterialFilterActive)
                return;
            if (windowOfType is null)
                windowOfType = typeof(WindowStack).GetMethod("WindowOfType", (BindingFlags)(-1))?.MakeGenericMethod(MaterialFilterWindowType);
            if (windowOfType is null)
                return;
            bool shouldShow = !toggle && active;
            if (windowOfType.Invoke(Find.WindowStack, new object[] { }) is Window window)
                window.Close();
            else if (toggle)
                shouldShow = true;
            if (shouldShow)
            {
                _ = Find.WindowStack.TryRemove(typeof(Dialog_Confirmation));
                _ = Find.WindowStack.TryRemove(typeof(Dialog_EditFilter));
                _ = Find.WindowStack.TryRemove(typeof(Dialog_NewFilter));
                _ = Find.WindowStack.TryRemove(typeof(Dialog_RenameSavedFilter));
                Find.WindowStack.Add(MaterialFilterWindowCtor.Invoke(new object[] { filter, position.y, position.x, WindowLayer.GameUI }) as Window);
            }
        }

        public static bool DrawMaterialFilterButton()
        {
            if (!StorageFilters.StorageTabRect.HasValue || lastButton is null || !(StorageFilters.GetCurrentFilter() is ThingFilter filter))
                return true;
            Rect tabRect = StorageFilters.StorageTabRect.Value;
            const string text = ">>";
            GUI.BeginGroup(tabRect.ContractedBy(10f));
            float width = Text.CalcSize(text).x + 10f;
            if (oldMaxFilterStringWidth is 0)
            {
                oldMaxFilterStringWidth = StorageFiltersData.MaxFilterStringWidth;
                StorageFiltersData.MaxFilterStringWidth -= width;
            }
            Rect position = new Rect(lastButton.Value.x + oldMaxFilterStringWidth - width, 0, width, 29f);
            if (Widgets.ButtonText(position, text))
                SetMaterialFilterWindowActive(filter, GenUtils.GetDialogPosition());
            GUI.EndGroup();
            return false;
        }

        public static void CapacityAt(Thing thing, IntVec3 storeCell, Map map, ref int __result)
        {
            if (__result <= 0 || !(storeCell.GetSlotGroup(map)?.parent is IStoreSettingsParent owner)) return;
            StorageFilters.GetStackLimitsForThing(owner, thing, out _, out int stackSizeLimit);
            if (stackSizeLimit > 0)
                __result = 0;
        }

        public static void PUAH_TryFindBestBetterStoreCellFor
            (Thing thing, Map map, ref StoragePriority currentPriority)
            => StorageFilters.TryFindBestBetterStoreCellFor(thing, map, ref currentPriority);

        public static void FillTab(ITab_Storage __instance, Vector2 ___size)
        {
            if (MaterialFilterActive)
                lastButton = StorageFilters.FillTab(__instance, ___size);
            else _ = StorageFilters.FillTab(__instance, ___size);
        }

        public static void TopAreaHeight(float __result) => _ = Math.Max(__result, 35f);

        public static void DoThingFilterConfigWindow
            (ref ThingFilter filter, ThingFilter parentFilter)
            => StorageFilters.DoThingFilterConfigWindow(ref filter, parentFilter);

        public static void AllowedToAccept
            (Thing t, StorageSettings __instance, ref bool __result)
            => StorageFilters.AllowedToAccept(__instance, t, ref __result);

        public static void HaulToStorageJob(Thing t, ref Job __result) => StorageFilters.HaulToStorageJob(t, ref __result);

        public static void NoStorageBlockersIn
            (IntVec3 c, Map map, Thing thing, ref bool __result)
            => StorageFilters.NoStorageBlockersIn(c, map, thing, ref __result);

        public static void TryFindBestBetterStoreCellFor
            (Thing t, Map map, ref StoragePriority currentPriority)
            => StorageFilters.TryFindBestBetterStoreCellFor(t, map, ref currentPriority);

        public static void TryFindBestBetterStoreCellForIn
            (Thing t, Map map, ref StoragePriority currentPriority)
            => StorageFilters.TryFindBestBetterStoreCellFor(t, map, ref currentPriority);

        public static void ShouldBeMergeable(Thing t, ref bool __result) => StorageFilters.ShouldBeMergeable(t, ref __result);

        public static void TryAbsorbStackNumToTake
            (Thing thing, Thing other, bool respectStackLimit, ref int __result)
            => StorageFilters.TryAbsorbStackNumToTake(thing, other, respectStackLimit, ref __result);

        public static void Copy(StorageSettings s) => StorageFilters.Copy(s);

        public static void PasteInto(StorageSettings s) => StorageFilters.Paste(s);

        public static bool Error
            (string text)
            => !text.StartsWith("Could not load reference to ")
            && !ReflectionUtils.IsMethodInCallStack(AccessTools.Method(typeof(ExtraThingFilter), nameof(ExtraThingFilter.ExposeData)));
    }
}