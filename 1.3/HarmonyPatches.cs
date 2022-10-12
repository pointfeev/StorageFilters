﻿using HarmonyLib;

using RimWorld;

using System;

using UnityEngine;

using Verse;

namespace StorageFilters
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("pointfeev.storagefilters");
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(ITab_Storage), "FillTab"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), "FillTab")
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(ITab_Storage), "get_TopAreaHeight"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), "TopAreaHeight")
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(ThingFilterUI), "DoThingFilterConfigWindow"),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), "DoThingFilterConfigWindow")
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(StorageSettings), "AllowedToAccept", new Type[] { typeof(Thing) }),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), "AllowedToAcceptThing")
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(StorageSettings), "AllowedToAccept", new Type[] { typeof(ThingDef) }),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), "AllowedToAcceptThingDef")
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(StorageSettingsClipboard), "Copy"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), "Copy")
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(StorageSettingsClipboard), "PasteInto"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), "PasteInto")
            );
        }

        public static void FillTab(ITab_Storage __instance, Vector2 ___size) => StorageFilters.FillTab(__instance, ___size);

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
    }
}