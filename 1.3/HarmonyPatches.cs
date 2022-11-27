using CompatUtils;

using HarmonyLib;

using RimWorld;

using StorageFilters.Utilities;

using System;
using System.Reflection;

using UnityEngine;

using Verse;
using Verse.AI;

namespace StorageFilters
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        internal static readonly bool MaterialFilterActive;
        private static readonly Type materialFilterWindowType;
        private static readonly ConstructorInfo materialFilterWindowCtor;

        internal static readonly bool PickUpAndHaulActive;

        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("pointfeev.storagefilters");
            MaterialFilterActive = Compatibility.IsModActive("kamikatze.materialfilter");
            if (MaterialFilterActive)
            {
                MethodInfo drawFilterButton = Compatibility.GetConsistentMethod("kamikatze.materialfilter", "MaterialFilter.ITab_Storage_FillTab_Patch", "drawFilterButton", new Type[] {
                    typeof(ITab_Storage), typeof(Vector2)
                }, logError: true);
                if (!(drawFilterButton is null)
                    && AccessTools.TypeByName("MaterialFilter.MaterialFilterWindow") is Type type
                    && AccessTools.Constructor(type, new Type[] {
                        typeof(ThingFilter), typeof(float), typeof(float), typeof(WindowLayer)
                    }) is ConstructorInfo ctor)
                {
                    _ = harmony.Patch(
                        original: drawFilterButton,
                        prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(DrawFilterButton))
                    );
                    materialFilterWindowType = type;
                    materialFilterWindowCtor = ctor;
                }
            }
            PickUpAndHaulActive = Compatibility.IsModActive("mehni.pickupandhaul");
            if (PickUpAndHaulActive)
            {
                MethodInfo capacityAt = Compatibility.GetConsistentMethod("mehni.pickupandhaul", "PickUpAndHaul.WorkGiver_HaulToInventory", "CapacityAt", new Type[] {
                    typeof(Thing), typeof(IntVec3), typeof(Map)
                }, logError: true);
                if (!(capacityAt is null))
                    _ = harmony.Patch(
                        original: capacityAt,
                        prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CapacityAt))
                    );
                MethodInfo tryFindBestBetterStoreCellFor = Compatibility.GetConsistentMethod("mehni.pickupandhaul", "PickUpAndHaul.WorkGiver_HaulToInventory", "TryFindBestBetterStoreCellFor", new Type[] {
                    typeof(Thing), typeof(Pawn), typeof(Map), typeof(StoragePriority), typeof(Faction), typeof(IntVec3)
                }, logError: true);
                if (!(tryFindBestBetterStoreCellFor is null))
                    _ = harmony.Patch(
                    original: tryFindBestBetterStoreCellFor,
                        prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(PUAH_TryFindBestBetterStoreCellFor))
                    );
            }
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(ITab_Storage), "FillTab"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(FillTab))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(ITab_Storage), "get_TopAreaHeight"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(TopAreaHeight))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(ThingFilterUI), "DoThingFilterConfigWindow"),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(DoThingFilterConfigWindow))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(StorageSettings), "AllowedToAccept", new Type[] { typeof(Thing) }),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(AllowedToAccept))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(HaulAIUtility), "HaulToStorageJob"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HaulToStorageJob))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(StoreUtility), "NoStorageBlockersIn"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(NoStorageBlockersIn))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(GenPlace), "PlaceSpotQualityAt"),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(PlaceSpotQualityAt))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(StoreUtility), "TryFindBestBetterStoreCellFor"),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(TryFindBestBetterStoreCellFor))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(ThingUtility), "TryAbsorbStackNumToTake"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(TryAbsorbStackNumToTake))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(ListerMergeables), "ShouldBeMergeable"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ShouldBeMergeable))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(StorageSettingsClipboard), "Copy"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Copy))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(StorageSettingsClipboard), "PasteInto"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(PasteInto))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(Log), "Error", new Type[] { typeof(string) }),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Error))
            );
        }

        private static float oldMaxFilterStringWidth = 0;
        private static Rect? lastButton;

        private static MethodInfo windowOfType;
        public static void SetMaterialFilterWindowActive(ThingFilter filter = null, Vector2 position = default, bool toggle = true, bool active = false)
        {
            if (!MaterialFilterActive)
                return;
            if (windowOfType is null)
                windowOfType = typeof(WindowStack)?.GetMethod("WindowOfType", (BindingFlags)(-1))?.MakeGenericMethod(materialFilterWindowType);
            if (windowOfType is null)
                return;
            bool shouldShow = !toggle && active;
            if (windowOfType.Invoke(Find.WindowStack, new object[] { }) is Window window)
                window.Close();
            else if (toggle)
                shouldShow = true;
            if (shouldShow)
            {
                _ = Find.WindowStack.TryRemove(typeof(Dialog_Confirmation), true);
                _ = Find.WindowStack.TryRemove(typeof(Dialog_EditFilter), true);
                _ = Find.WindowStack.TryRemove(typeof(Dialog_NewFilter), true);
                _ = Find.WindowStack.TryRemove(typeof(Dialog_RenameSavedFilter), true);
                Find.WindowStack.Add(materialFilterWindowCtor.Invoke(new object[] {
                    filter, position.y, position.x, WindowLayer.GameUI
                }) as Window);
            }
        }

        public static bool DrawFilterButton()
        {
            if (!StorageFilters.StorageTabRect.HasValue || lastButton is null)
                return true;
            IStoreSettingsParent storeSettingsParent = GenUtils.GetSelectedStoreSettingsParent();
            if (storeSettingsParent is null)
                return true;
            Rect TabRect = StorageFilters.StorageTabRect.Value;
            ThingFilter filter = storeSettingsParent.GetStoreSettings()?.filter;
            if (filter is null)
                return true;
            string text = ">>";
            GUI.BeginGroup(TabRect.ContractedBy(10f));
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

        public static void PUAH_TryFindBestBetterStoreCellFor(Thing thing, ref StoragePriority currentPriority)
            => StorageFilters.TryFindBestBetterStoreCellFor(thing, ref currentPriority);

        public static void FillTab(ITab_Storage __instance, Vector2 ___size)
        {
            if (MaterialFilterActive)
                lastButton = StorageFilters.FillTab(__instance, ___size);
            else _ = StorageFilters.FillTab(__instance, ___size);
        }

        public static void TopAreaHeight(float __result) => _ = Math.Max(__result, 35f);

        public static void DoThingFilterConfigWindow(ref ThingFilter filter)
            => StorageFilters.DoThingFilterConfigWindow(ref filter);

        public static bool AllowedToAccept(Thing t, ref bool __result, ThingFilter ___filter, IStoreSettingsParent ___owner)
            => StorageFilters.AllowedToAccept(___owner, ___filter, t, ref __result);

        public static void HaulToStorageJob(Thing t, ref Job __result)
            => StorageFilters.HaulToStorageJob(t, ref __result);

        public static void NoStorageBlockersIn(IntVec3 c, Map map, Thing thing, ref bool __result)
            => StorageFilters.NoStorageBlockersIn(c, map, thing, ref __result);

        public static void PlaceSpotQualityAt(Thing thing, bool allowStacking, ref object __result)
            => StorageFilters.PlaceSpotQualityAt(thing, allowStacking, ref __result);

        public static void TryFindBestBetterStoreCellFor(Thing t, ref StoragePriority currentPriority)
            => StorageFilters.TryFindBestBetterStoreCellFor(t, ref currentPriority);

        public static void ShouldBeMergeable(Thing t, ref bool __result)
            => StorageFilters.ShouldBeMergeable(t, ref __result);

        public static void TryAbsorbStackNumToTake(Thing thing, Thing other, bool respectStackLimit, ref int __result)
            => StorageFilters.TryAbsorbStackNumToTake(thing, other, respectStackLimit, ref __result);

        public static void Copy(StorageSettings s) => StorageFilters.Copy(s);

        public static void PasteInto(StorageSettings s) => StorageFilters.Paste(s);

        public static bool Error(string text) => !text.StartsWith("Could not load reference to ")
            && !ReflectionUtils.IsMethodInCallStack(AccessTools.Method(typeof(ExtraThingFilter), nameof(ExtraThingFilter.ExposeData)));
    }
}