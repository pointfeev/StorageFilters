using RimWorld;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

using Verse;
using Verse.Sound;

namespace StorageFilters
{
    public static class GenUtils
    {
        public static bool IsStorageTabOpen(ITab_Storage storageTab, IStoreSettingsParent storeSettingsParent)
        {
            if (storageTab is null || !storageTab.IsVisible || GetSelectedStoreSettingsParent() != storeSettingsParent)
                return false;
            foreach (Window window in Find.WindowStack.Windows)
                if (window.ID == -235086)
                    return window.IsOpen;
            return false;
        }

        public static void PlayClick() => SoundDefOf.Click.PlayOneShotOnCamera(null);

        public static Vector2 GetDialogPosition(float X = 0, float Y = 0, Window editDialog = null)
        {
            if (MainButtonDefOf.Inspect.TabWindow is MainTabWindow_Inspect inspectPane && StorageFilters.StorageTabRect.HasValue)
            {
                X = StorageFilters.StorageTabRect.Value.xMax - 1f;
                Y = inspectPane.PaneTopY - 30f - StorageFilters.StorageTabRect.Value.height;
                if (!(editDialog is null))
                    X += editDialog.InitialSize.x - 1f;
            }
            return new Vector2(X, Y).Rounded();
        }

        public static Rect GetDialogSizeAndPosition(Window dialog, Window editDialog = null)
        {
            Vector2 initialSize = dialog.InitialSize;
            Vector2 position = GetDialogPosition((UI.screenWidth - initialSize.x) / 2f, (UI.screenHeight - initialSize.y) / 2f, editDialog);
            return new Rect(position, new Vector2(initialSize.x, initialSize.y)).Rounded();
        }

        public static IStoreSettingsParent GetStoreSettingsParent(object obj)
        {
            if (obj is IStoreSettingsParent storeSettingsParent)
                return storeSettingsParent;
            else if (obj is ThingWithComps thingWithComps)
                foreach (ThingComp thingComp in thingWithComps.AllComps)
                    if (thingComp is IStoreSettingsParent)
                        return thingComp as IStoreSettingsParent;
            return null;
        }

        public static IStoreSettingsParent GetSelectedStoreSettingsParent() => !(Find.UIRoot is UIRoot_Play) || !(Find.MapUI is MapInterface) ? null : GetStoreSettingsParent(Find.Selector.SingleSelectedObject);

        public static void FilterSelectionButton(ITab_Storage instance, IStoreSettingsParent storeSettingsParent, ExtraThingFilters tabFilters, string mainFilterString, string tabFilter, Rect position)
        {
            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(position, tabFilter, true, true, true))
            {
                HarmonyPatches.SetMaterialFilterWindowActive(toggle: false, active: false);
                Dictionary<FloatMenuOption, int> floatMenuOptionOrder = new Dictionary<FloatMenuOption, int>();
                FloatMenuOption newFilterOption(FloatMenuOption floatMenuOption)
                {
                    floatMenuOptionOrder.SetOrAdd(floatMenuOption, floatMenuOptionOrder.Count);
                    return floatMenuOption;
                }
                List<FloatMenuOption> filterFloatMenuOptions = new List<FloatMenuOption>();
                FloatMenu filterFloatMenu = null;
                string editString = "ASF_EditFilter".Translate();
                float editX = Text.CalcSize(editString).x + 8f;
                filterFloatMenuOptions.Add(newFilterOption(new FloatMenuOption(mainFilterString, delegate ()
                {
                    if (!(Find.WindowStack.WindowOfType<Dialog_EditFilter>() is null))
                        Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, mainFilterString, true, tabFilters));
                    StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
                    StorageFiltersData.CurrentFilterDepth.SetOrAdd(storeSettingsParent, 0);
                }, extraPartWidth: editX, extraPartOnGUI: delegate (Rect extraRect)
                {
                    _ = new FloatMenuOption(editString, delegate ()
                    {
                        filterFloatMenu.Close();
                        Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, mainFilterString, true, tabFilters));
                        StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
                        StorageFiltersData.CurrentFilterDepth.SetOrAdd(storeSettingsParent, 0);
                    }).DoGUI(extraRect, false, null);
                    return false;
                })));
                if (tabFilters.Count > 0)
                {
                    foreach (KeyValuePair<string, ExtraThingFilter> entry in tabFilters)
                    {
                        void action()
                        {
                            if (!(Find.WindowStack.WindowOfType<Dialog_EditFilter>() is null))
                                Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, entry.Key, entry.Value, tabFilters));
                            StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, entry.Key);
                            StorageFiltersData.CurrentFilterDepth.SetOrAdd(storeSettingsParent, 0);
                        }
                        FloatMenuOption floatMenuOption = null;
                        string enableString = "ASF_EnableFilter".Translate();
                        string disableString = "ASF_DisableFilter".Translate();
                        float toggleX = Math.Max(Text.CalcSize(enableString).x, Text.CalcSize(disableString).x) + 8f;
                        string removeString = "ASF_RemoveFilter".Translate();
                        float removeX = Text.CalcSize(removeString).x + 8f;
                        floatMenuOption = newFilterOption(new FloatMenuOption(entry.Key, action, extraPartWidth: editX + toggleX + removeX, extraPartOnGUI: delegate (Rect extraRect)
                        {
                            Rect renameRect = extraRect;
                            renameRect.width = editX;
                            _ = new FloatMenuOption(editString, delegate ()
                            {
                                filterFloatMenu.Close();
                                Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, entry.Key, entry.Value, tabFilters));
                                StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, entry.Key);
                                StorageFiltersData.CurrentFilterDepth.SetOrAdd(storeSettingsParent, 0);
                            }).DoGUI(renameRect, false, null);
                            Rect toggleRect = extraRect;
                            toggleRect.width = toggleX;
                            toggleRect.x += renameRect.width;
                            _ = new FloatMenuOption(entry.Value.Enabled ? disableString : enableString, delegate ()
                            {
                                entry.Value.Enabled = !entry.Value.Enabled;
                                if (entry.Value.Enabled)
                                    floatMenuOption.action = action;
                                else
                                    floatMenuOption.Disabled = true;
                                PlayClick();
                            }).DoGUI(toggleRect, false, null);
                            Rect removeRect = extraRect;
                            removeRect.width = removeX;
                            removeRect.x += renameRect.width + toggleRect.width;
                            _ = new FloatMenuOption(removeString, delegate ()
                            {
                                filterFloatMenu.Close();
                                Find.WindowStack.Add(new Dialog_Confirmation(instance, storeSettingsParent, "ASF_ConfirmRemoveFilter".Translate(entry.Key), delegate ()
                                {
                                    tabFilters.Remove(entry.Key);
                                    if (StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent) == entry.Key)
                                    {
                                        _ = Find.WindowStack.TryRemove(typeof(Dialog_EditFilter), true);
                                        StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
                                        StorageFiltersData.CurrentFilterDepth.SetOrAdd(storeSettingsParent, 0);
                                    }
                                }));
                            }).DoGUI(removeRect, false, null);
                            return false;
                        }));
                        floatMenuOption.Disabled = !entry.Value.Enabled;
                        filterFloatMenuOptions.Add(floatMenuOption);
                    }
                }
                filterFloatMenuOptions.Add(newFilterOption(new FloatMenuOption("ASF_NewFilter".Translate(), delegate ()
                {
                    _ = Find.WindowStack.TryRemove(typeof(Dialog_EditFilter), false);
                    Find.WindowStack.Add(new Dialog_NewFilter(instance, storeSettingsParent, tabFilters));
                })));
                filterFloatMenu = new FloatMenu(filterFloatMenuOptions);
                FieldInfo optionsFieldInfo = filterFloatMenu.GetType().GetField("options", BindingFlags.NonPublic | BindingFlags.Instance);
                List<FloatMenuOption> options = optionsFieldInfo.GetValue(filterFloatMenu) as List<FloatMenuOption>;
                optionsFieldInfo.SetValue(filterFloatMenu, (from option in options orderby floatMenuOptionOrder.TryGetValue(option) ascending select option).ToList());
                Find.WindowStack.Add(filterFloatMenu);
            }
            UIHighlighter.HighlightOpportunity(position, "StorageFilters");
        }
    }
}