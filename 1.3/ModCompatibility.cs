using System;
using System.Reflection;
using CompatUtils;
using HarmonyLib;
using RimWorld;
using StorageFilters.Dialogs;
using StorageFilters.Utilities;
using UnityEngine;
using Verse;

namespace StorageFilters;

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
        bool pickUpAndHaulActive = Compatibility.IsModActive("mehni.pickupandhaul");
        if (pickUpAndHaulActive)
        {
            MethodInfo capacityAt = Compatibility.GetConsistentMethod("mehni.pickupandhaul", "PickUpAndHaul.WorkGiver_HaulToInventory", "CapacityAt",
                new[] { typeof(Thing), typeof(IntVec3), typeof(Map) }, true);
            if (capacityAt != null)
                _ = harmony.Patch(capacityAt, new(typeof(ModCompatibility), nameof(CapacityAt)));
            MethodInfo tryFindBestBetterStoreCellFor = Compatibility.GetConsistentMethod("mehni.pickupandhaul", "PickUpAndHaul.WorkGiver_HaulToInventory",
                "TryFindBestBetterStoreCellFor", new[] { typeof(Thing), typeof(Pawn), typeof(Map), typeof(StoragePriority), typeof(Faction), typeof(IntVec3) },
                true);
            if (tryFindBestBetterStoreCellFor != null)
                _ = harmony.Patch(tryFindBestBetterStoreCellFor, new(typeof(ModCompatibility), nameof(TryFindBestBetterStoreCellFor)));
        }
        materialFilterActive = Compatibility.IsModActive("kamikatze.materialfilter");
        if (materialFilterActive)
        {
            MethodInfo drawFilterButton = Compatibility.GetConsistentMethod("kamikatze.materialfilter", "MaterialFilter.ITab_Storage_FillTab_Patch",
                "drawFilterButton", new[] { typeof(ITab_Storage), typeof(Vector2) }, true);
            if (drawFilterButton == null || AccessTools.TypeByName("MaterialFilter.MaterialFilterWindow") is not { } type
                                         || AccessTools.Constructor(type, new[] { typeof(ThingFilter), typeof(float), typeof(float), typeof(WindowLayer) }) is
                                                not { } ctor)
                return;
            _ = harmony.Patch(drawFilterButton, new(typeof(ModCompatibility), nameof(DrawMaterialFilterButton)));
            materialFilterWindowType = type;
            materialFilterWindowCtor = ctor;
        }
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
        if (!StorageFilters.StorageTabRect.HasValue || LastButton is null || StorageFilters.GetCurrentFilter() is not { } filter)
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
        Rect position = new(LastButton.Value.x + oldMaxFilterStringWidth - width, 0, width, 29f);
        if (Widgets.ButtonText(position, text))
            SetMaterialFilterWindowActive(filter, GenUtils.GetDialogPosition());
        GUI.EndGroup();
        return false;
    }

    internal static void CapacityAt(Thing thing, IntVec3 storeCell, Map map, ref int __result)
    {
        if (__result <= 0 || storeCell.GetSlotGroup(map)?.parent is not IStoreSettingsParent owner)
            return;
        owner.GetStackLimitsForThing(thing, out _, out int stackSizeLimit);
        if (stackSizeLimit > 0)
            __result = 0;
    }

    internal static void TryFindBestBetterStoreCellFor(Thing thing, Map map, ref StoragePriority currentPriority)
        => StorageFilters.TryFindBestBetterStoreCellFor(thing, map, ref currentPriority);
}