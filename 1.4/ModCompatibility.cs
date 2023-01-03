using System;
using System.Reflection;
using CompatUtils;
using HarmonyLib;
using RimWorld;
using StorageFilters.Dialogs;
using StorageFilters.Utilities;
using UnityEngine;
using Verse;

namespace StorageFilters
{
    public static class ModCompatibility
    {
        private static bool materialFilterActive;
        private static Type materialFilterWindowType;
        private static ConstructorInfo materialFilterWindowCtor;

        private static float oldMaxFilterStringWidth;
        internal static Rect? LastButton;

        private static MethodInfo windowOfType;

        internal static void PatchForMods(this Harmony harmony)
        {
            materialFilterActive = Compatibility.IsModActive("kamikatze.materialfilter");
            if (!materialFilterActive)
                return;
            MethodInfo drawFilterButton = Compatibility.GetConsistentMethod("kamikatze.materialfilter", "MaterialFilter.ITab_Storage_FillTab_Patch",
                "drawFilterButton", new[] { typeof(ITab_Storage), typeof(Vector2) }, true);
            if (drawFilterButton is null || !(AccessTools.TypeByName("MaterialFilter.MaterialFilterWindow") is Type type)
                                         || !(AccessTools.Constructor(type, new[] { typeof(ThingFilter), typeof(float), typeof(float), typeof(WindowLayer) }) is
                                                ConstructorInfo ctor))
                return;
            _ = harmony.Patch(drawFilterButton, new HarmonyMethod(typeof(HarmonyPatches), nameof(DrawMaterialFilterButton)));
            materialFilterWindowType = type;
            materialFilterWindowCtor = ctor;
            bool pickUpAndHaulActive = Compatibility.IsModActive("mehni.pickupandhaul");
            if (!pickUpAndHaulActive)
                return;
            MethodInfo capacityAt = Compatibility.GetConsistentMethod("mehni.pickupandhaul", "PickUpAndHaul.WorkGiver_HaulToInventory", "CapacityAt",
                new[] { typeof(Thing), typeof(IntVec3), typeof(Map) }, true);
            if (!(capacityAt is null))
                _ = harmony.Patch(capacityAt, new HarmonyMethod(typeof(HarmonyPatches), nameof(CapacityAt)));
            MethodInfo tryFindBestBetterStoreCellFor = Compatibility.GetConsistentMethod("mehni.pickupandhaul", "PickUpAndHaul.WorkGiver_HaulToInventory",
                "TryFindBestBetterStoreCellFor", new[] { typeof(Thing), typeof(Pawn), typeof(Map), typeof(StoragePriority), typeof(Faction), typeof(IntVec3) },
                true);
            if (!(tryFindBestBetterStoreCellFor is null))
                _ = harmony.Patch(tryFindBestBetterStoreCellFor, new HarmonyMethod(typeof(HarmonyPatches), nameof(TryFindBestBetterStoreCellFor)));
        }

        internal static void SetMaterialFilterWindowActive(ThingFilter filter = null, Vector2 position = default, bool toggle = true, bool active = false)
        {
            if (!materialFilterActive)
                return;
            if (windowOfType is null)
                windowOfType = typeof(WindowStack).GetMethod("WindowOfType", (BindingFlags)(-1))?.MakeGenericMethod(materialFilterWindowType);
            if (windowOfType is null)
                return;
            bool shouldShow = !toggle && active;
            if (windowOfType.Invoke(Find.WindowStack, new object[] { }) is Window window)
                window.Close();
            else if (toggle)
                shouldShow = true;
            if (!shouldShow)
                return;
            _ = Find.WindowStack.TryRemove(typeof(Dialog_Confirmation));
            _ = Find.WindowStack.TryRemove(typeof(Dialog_EditFilter));
            _ = Find.WindowStack.TryRemove(typeof(Dialog_NewFilter));
            _ = Find.WindowStack.TryRemove(typeof(Dialog_RenameSavedFilter));
            Find.WindowStack.Add(materialFilterWindowCtor.Invoke(new object[] { filter, position.y, position.x, WindowLayer.GameUI }) as Window);
        }

        internal static bool DrawMaterialFilterButton()
        {
            if (!StorageFilters.StorageTabRect.HasValue || LastButton is null || !(StorageFilters.GetCurrentFilter() is ThingFilter filter))
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
            Rect position = new Rect(LastButton.Value.x + oldMaxFilterStringWidth - width, 0, width, 29f);
            if (Widgets.ButtonText(position, text))
                SetMaterialFilterWindowActive(filter, GenUtils.GetDialogPosition());
            GUI.EndGroup();
            return false;
        }

        internal static void CapacityAt(Thing thing, IntVec3 storeCell, Map map, ref int __result)
        {
            if (__result <= 0 || !(storeCell.GetSlotGroup(map)?.parent is IStoreSettingsParent owner)) return;
            owner.GetStackLimitsForThing(thing, out _, out int stackSizeLimit);
            if (stackSizeLimit > 0)
                __result = 0;
        }

        internal static void TryFindBestBetterStoreCellFor(Thing thing, Map map, ref StoragePriority currentPriority)
            => StorageFilters.TryFindBestBetterStoreCellFor(thing, map, ref currentPriority);
    }
}