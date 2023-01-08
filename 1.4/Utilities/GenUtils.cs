using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using StorageFilters.Dialogs;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace StorageFilters.Utilities
{
    public static class GenUtils
    {
        public static bool IsStorageTabOpen(ITab_Storage storageTab, IStoreSettingsParent storeSettingsParent)
        {
            if (storageTab is null || !storageTab.IsVisible
                                   || GetSelectedStoreSettingsParent().GetStorageGroupOwner() != storeSettingsParent.GetStorageGroupOwner())
                return false;
            return (from window in Find.WindowStack.Windows where window.ID == -235086 select window.IsOpen).FirstOrDefault();
        }

        public static void PlayClick() => SoundDefOf.Click.PlayOneShotOnCamera();

        public static Vector2 GetDialogPosition(float x = 0, float y = 0, Window editDialog = null)
        {
            if (MainButtonDefOf.Inspect.TabWindow is MainTabWindow_Inspect inspectPane && StorageFilters.StorageTabRect.HasValue)
            {
                x = StorageFilters.StorageTabRect.Value.xMax - 1f;
                y = inspectPane.PaneTopY - 30f - StorageFilters.StorageTabRect.Value.height;
                if (!(editDialog is null))
                    x += editDialog.InitialSize.x - 1f;
            }
            return new Vector2(x, y).Rounded();
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
            if (obj is ThingWithComps thingWithComps)
                foreach (ThingComp thingComp in thingWithComps.AllComps)
                    if (thingComp is IStoreSettingsParent)
                        return thingComp as IStoreSettingsParent;
            return null;
        }

        public static IStoreSettingsParent GetSelectedStoreSettingsParent()
            => !(Find.UIRoot is UIRoot_Play) || Find.MapUI is null ? null : GetStoreSettingsParent(Find.Selector.SingleSelectedObject);

        public static void FilterSelectionButton(ITab_Storage instance, IStoreSettingsParent storeSettingsParent, ExtraThingFilters tabFilters,
            string mainFilterString, string tabFilter, Rect position)
        {
            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(position, tabFilter))
            {
                ModCompatibility.SetMaterialFilterWindowActive(toggle: false, active: false);
                Dictionary<FloatMenuOption, int> floatMenuOptionOrder = new Dictionary<FloatMenuOption, int>();
                FloatMenuOption NewFilterOption(FloatMenuOption floatMenuOption)
                {
                    floatMenuOptionOrder.SetOrAdd(floatMenuOption, floatMenuOptionOrder.Count);
                    return floatMenuOption;
                }
                List<FloatMenuOption> filterFloatMenuOptions = new List<FloatMenuOption>();
                FloatMenu filterFloatMenu = null;
                string editString = "ASF_EditFilter".Translate();
                float editX = Text.CalcSize(editString).x + 8f;
                filterFloatMenuOptions.Add(NewFilterOption(new FloatMenuOption(mainFilterString, delegate
                {
                    if (!(Find.WindowStack.WindowOfType<Dialog_EditFilter>() is null))
                        Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, mainFilterString, true, tabFilters));
                    StorageFiltersData.SetCurrentFilterKey(storeSettingsParent, mainFilterString);
                    StorageFiltersData.SetCurrentFilterDepth(storeSettingsParent, 0);
                }, extraPartWidth: editX, extraPartOnGUI: delegate(Rect extraRect)
                {
                    _ = new FloatMenuOption(editString, delegate
                    {
                        filterFloatMenu.Close();
                        Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, mainFilterString, true, tabFilters));
                        StorageFiltersData.SetCurrentFilterKey(storeSettingsParent, mainFilterString);
                        StorageFiltersData.SetCurrentFilterDepth(storeSettingsParent, 0);
                    }).DoGUI(extraRect, false, null);
                    return false;
                })));
                if (tabFilters.Count > 0)
                    foreach (KeyValuePair<string, ExtraThingFilter> entry in tabFilters)
                    {
                        void Action()
                        {
                            if (!(Find.WindowStack.WindowOfType<Dialog_EditFilter>() is null))
                                Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, entry.Key, entry.Value, tabFilters));
                            StorageFiltersData.SetCurrentFilterKey(storeSettingsParent, entry.Key);
                            StorageFiltersData.SetCurrentFilterDepth(storeSettingsParent, 0);
                        }
                        FloatMenuOption floatMenuOption = null;
                        string enableString = "ASF_EnableFilter".Translate();
                        string disableString = "ASF_DisableFilter".Translate();
                        float toggleX = Math.Max(Text.CalcSize(enableString).x, Text.CalcSize(disableString).x) + 8f;
                        string removeString = "ASF_RemoveFilter".Translate();
                        float removeX = Text.CalcSize(removeString).x + 8f;
                        floatMenuOption = NewFilterOption(new FloatMenuOption(entry.Key, Action, extraPartWidth: editX + toggleX + removeX,
                            extraPartOnGUI: delegate(Rect extraRect)
                            {
                                Rect renameRect = extraRect;
                                renameRect.width = editX;
                                _ = new FloatMenuOption(editString, delegate
                                {
                                    filterFloatMenu.Close();
                                    Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, entry.Key, entry.Value, tabFilters));
                                    StorageFiltersData.SetCurrentFilterKey(storeSettingsParent, entry.Key);
                                    StorageFiltersData.SetCurrentFilterDepth(storeSettingsParent, 0);
                                }).DoGUI(renameRect, false, null);
                                Rect toggleRect = extraRect;
                                toggleRect.width = toggleX;
                                toggleRect.x += renameRect.width;
                                _ = new FloatMenuOption(entry.Value.Enabled ? disableString : enableString, delegate
                                {
                                    entry.Value.Enabled = !entry.Value.Enabled;
                                    if (entry.Value.Enabled)
                                        floatMenuOption.action = Action;
                                    else
                                        floatMenuOption.Disabled = true;
                                    PlayClick();
                                }).DoGUI(toggleRect, false, null);
                                Rect removeRect = extraRect;
                                removeRect.width = removeX;
                                removeRect.x += renameRect.width + toggleRect.width;
                                _ = new FloatMenuOption(removeString, delegate
                                {
                                    filterFloatMenu.Close();
                                    Find.WindowStack.Add(new Dialog_Confirmation(instance, storeSettingsParent, "ASF_ConfirmRemoveFilter".Translate(entry.Key),
                                        delegate
                                        {
                                            tabFilters.Remove(entry.Key);
                                            if (StorageFiltersData.GetCurrentFilterKey(storeSettingsParent) == entry.Key)
                                            {
                                                _ = Find.WindowStack.TryRemove(typeof(Dialog_EditFilter));
                                                StorageFiltersData.SetCurrentFilterKey(storeSettingsParent, mainFilterString);
                                                StorageFiltersData.SetCurrentFilterDepth(storeSettingsParent, 0);
                                            }
                                        }));
                                }).DoGUI(removeRect, false, null);
                                return false;
                            }));
                        floatMenuOption.Disabled = !entry.Value.Enabled;
                        filterFloatMenuOptions.Add(floatMenuOption);
                    }
                filterFloatMenuOptions.Add(NewFilterOption(new FloatMenuOption("ASF_NewFilter".Translate(), delegate
                {
                    _ = Find.WindowStack.TryRemove(typeof(Dialog_EditFilter), false);
                    Find.WindowStack.Add(new Dialog_NewFilter(instance, storeSettingsParent, tabFilters));
                })));
                filterFloatMenu = new FloatMenu(filterFloatMenuOptions);
                FieldInfo optionsFieldInfo = filterFloatMenu.GetType().GetField("options", BindingFlags.NonPublic | BindingFlags.Instance);
                List<FloatMenuOption> options = optionsFieldInfo?.GetValue(filterFloatMenu) as List<FloatMenuOption>;
                optionsFieldInfo?.SetValue(filterFloatMenu, (from option in options orderby floatMenuOptionOrder.TryGetValue(option) select option).ToList());
                Find.WindowStack.Add(filterFloatMenu);
            }
            UIHighlighter.HighlightOpportunity(position, "StorageFilters");
        }
    }
}