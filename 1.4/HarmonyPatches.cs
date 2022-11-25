using CompatUtils;

using HarmonyLib;

using RimWorld;

using StorageFilters.Utilities;

using System;
using System.Reflection;

using UnityEngine;

using Verse;

namespace StorageFilters
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        internal static readonly bool MaterialFilterActive;
        private static readonly Type materialFilterWindowType;
        private static readonly ConstructorInfo materialFilterWindowCtor;

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
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(AllowedToAcceptThing))
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(StorageSettings), "AllowedToAccept", new Type[] { typeof(ThingDef) }),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(AllowedToAcceptThingDef))
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
        private static ITab_Storage lastInstance;
        private static Vector2? lastSize;
        private static Rect? lastButton;

        public static bool DrawFilterButton()
        {
            if (StorageFilters.StorageTabRect is null || lastInstance is null || lastSize is null || lastButton is null)
                return true;
            Rect TabRect = Traverse.Create(lastInstance).Property("TabRect").GetValue<Rect>();
            ThingFilter filter = Traverse.Create(lastInstance).Property("SelStoreSettingsParent").GetValue<IStoreSettingsParent>().GetStoreSettings().filter;
            string text = ">>";
            GUI.BeginGroup(StorageFilters.StorageTabRect.Value.ContractedBy(10f));
            float width = Text.CalcSize(text).x + 10f;
            if (oldMaxFilterStringWidth is 0)
            {
                oldMaxFilterStringWidth = StorageFiltersData.MaxFilterStringWidth;
                StorageFiltersData.MaxFilterStringWidth -= width;
            }
            Rect position = new Rect(lastButton.Value.x + oldMaxFilterStringWidth - width, 0, width, 29f);
            if (Widgets.ButtonText(position, text))
            {
                if (!(typeof(WindowStack)?.GetMethod("WindowOfType", (BindingFlags)(-1))?.MakeGenericMethod(materialFilterWindowType) is MethodInfo windowOfType))
                    return true;
                if (windowOfType.Invoke(Find.WindowStack, new object[] { }) is Window w)
                    w.Close();
                else
                    Find.WindowStack.Add(materialFilterWindowCtor.Invoke(new object[] {
                        filter, TabRect.y, lastSize.Value.x, WindowLayer.GameUI
                    }) as Window);
            }
            GUI.EndGroup();
            return false;
        }

        public static void FillTab(ITab_Storage __instance, Vector2 ___size)
        {
            if (MaterialFilterActive)
            {
                lastInstance = __instance;
                lastSize = ___size;
                lastButton = StorageFilters.FillTab(__instance, ___size);
            }
            else _ = StorageFilters.FillTab(__instance, ___size);
        }

        public static void TopAreaHeight(float __result) => _ = Math.Max(__result, 35f);

        public static bool DoThingFilterConfigWindow(ref ThingFilter filter)
        {
            StorageFilters.DoThingFilterConfigWindow(ref filter);
            return true;
        }

        public static bool AllowedToAcceptThing(Thing t, ref bool __result, ThingFilter ___filter, IStoreSettingsParent ___owner)
        {
            StorageFilters.AllowedToAccept(___owner, ___filter, t, ref __result);
            return false;
        }

        public static bool AllowedToAcceptThingDef(ThingDef t, ref bool __result, ThingFilter ___filter, IStoreSettingsParent ___owner)
        {
            StorageFilters.AllowedToAccept(___owner, ___filter, t, ref __result);
            return false;
        }

        public static void Copy(StorageSettings s) => StorageFilters.Copy(s);

        public static void PasteInto(StorageSettings s) => StorageFilters.Paste(s);

        public static bool Error(string text) => !text.StartsWith("Could not load reference to ")
            && !ReflectionUtils.IsMethodInCallStack(AccessTools.Method(typeof(ExtraThingFilter), nameof(ExtraThingFilter.ExposeData)));
    }
}