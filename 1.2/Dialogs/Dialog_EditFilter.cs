using System.Collections.Generic;
using RimWorld;
using StorageFilters.Utilities;
using UnityEngine;
using Verse;

namespace StorageFilters.Dialogs
{
    internal class Dialog_EditFilter : Window
    {
        private readonly bool keyIsMainFilterString;
        private readonly Dialog_EditFilter previousDialog;

        private readonly ITab_Storage storageTab;
        private readonly IStoreSettingsParent storeSettingsParent;
        private readonly ExtraThingFilters tabFilters;
        private string curName;

        public ExtraThingFilter Filter;
        private string key;

        public Dialog_EditFilter(ITab_Storage instance, IStoreSettingsParent storeSettingsParent)
        {
            layer = WindowLayer.GameUI;
            preventCameraMotion = false;
            soundAppear = SoundDefOf.TabOpen;
            soundClose = SoundDefOf.TabClose;
            doCloseX = true;
            forcePause = false;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = false;
            storageTab = instance;
            this.storeSettingsParent = storeSettingsParent;
        }

        public Dialog_EditFilter
        (ITab_Storage instance, IStoreSettingsParent storeSettingsParent, string key, ExtraThingFilter value, ExtraThingFilters tabFilters = null,
            Dialog_EditFilter previousEditFilterDialog = null) : this(instance, storeSettingsParent)
        {
            this.key = key;
            Filter = value;
            this.tabFilters = tabFilters;
            curName = key;
            previousDialog = previousEditFilterDialog;
        }

        public Dialog_EditFilter
            (ITab_Storage instance, IStoreSettingsParent storeSettingsParent, string key, bool keyIsMainFilterString, ExtraThingFilters tabFilters) : this(
            instance, storeSettingsParent, key, null, tabFilters)
        {
            this.keyIsMainFilterString = keyIsMainFilterString;
            Filter = new ExtraThingFilter(storeSettingsParent.GetStoreSettings().filter);
        }

        public override Vector2 InitialSize => new Vector2(320f, 230f); //273f);

        protected override void SetInitialSizeAndPosition() => windowRect = GenUtils.GetDialogSizeAndPosition(this);

        private bool CheckCurName()
        {
            if (NamePlayerFactionDialogUtility.IsValidName(curName))
            {
                if (key == curName || (StorageFiltersData.GetMainFilterName(storeSettingsParent) != curName && !tabFilters.ContainsKey(curName)))
                {
                    if (key != curName)
                    {
                        if (keyIsMainFilterString)
                        {
                            StorageFiltersData.SetMainFilterName(storeSettingsParent, curName);
                        }
                        else
                        {
                            tabFilters.Remove(key);
                            tabFilters.Add(curName, Filter);
                        }
                        if (StorageFiltersData.GetCurrentFilterKey(storeSettingsParent) == key)
                            StorageFiltersData.SetCurrentFilterKey(storeSettingsParent, curName);
                        key = curName;
                        GenUtils.PlayClick();
                        return true;
                    }
                }
                else
                {
                    Messages.Message("ASF_StorageAreaAlreadyHasFilterNamed".Translate(curName), MessageTypeDefOf.RejectInput, false);
                }
            }
            else
            {
                Messages.Message("ASF_InvalidString".Translate(), MessageTypeDefOf.RejectInput, false);
            }
            return false;
        }

        public override void DoWindowContents(Rect winRect)
        {
            if (!GenUtils.IsStorageTabOpen(storageTab, storeSettingsParent))
            {
                _ = Find.WindowStack.TryRemove(this, false);
                return;
            }
            if (Widgets.CloseButtonFor(windowRect.AtZero()))
            {
                _ = Find.WindowStack.TryRemove(this);
                Event.current.Use();
                return;
            }
            if (Filter is null || (Event.current.type == EventType.KeyDown
                                && (Event.current.keyCode == KeyCode.Escape || Event.current.keyCode == KeyCode.Return)))
            {
                _ = Find.WindowStack.TryRemove(this);
                Event.current.Use();
                return;
            }
            if (tabFilters != null)
            {
                string mainFilterString = StorageFiltersData.GetMainFilterName(storeSettingsParent);
                if (keyIsMainFilterString && !(mainFilterString is null))
                    StorageFiltersData.SetCurrentFilterKey(storeSettingsParent, mainFilterString);
                else if (!(key is null))
                    StorageFiltersData.SetCurrentFilterKey(storeSettingsParent, key);
                StorageFiltersData.SetCurrentFilterDepth(storeSettingsParent, Filter.FilterDepth);
            }
            Text.Font = GameFont.Small;
            string editString = "ASF_EditingFilter".Translate(key);
            float editStringY = Text.CalcSize(editString).y;
            Widgets.Label(new Rect(0f, 0f, winRect.width, editStringY), editString);
            float renameY = editStringY + 8f;
            //float stackCountY = renameY + 35f + 8f;
            float stackLimitY = renameY + 35f + 8f; //stackCountY + 35f + 8f;
            float saveLoadY = stackLimitY + 35f + 8f;
            if (tabFilters != null)
            {
                string renameString = "ASF_RenameFilter".Translate();
                float renameStringX = Text.CalcSize(renameString).x + 30f;
                curName = Widgets.TextField(new Rect(0f, renameY, winRect.width - 8f - renameStringX, 35f), curName);
                if (Widgets.ButtonText(new Rect(winRect.width - renameStringX, renameY, renameStringX, 35f), renameString))
                {
                    _ = CheckCurName();
                    Event.current.Use();
                }
                if (!keyIsMainFilterString)
                {
                    /*string stackCountString = "ASF_StackCountLimit".Translate();
                    Vector2 stackCountStringSize = Text.CalcSize(stackCountString);
                    Widgets.Label(new Rect(0f, stackCountY + 35f / 2 - stackCountStringSize.y / 2, stackCountStringSize.x, 35f), stackCountString);
                    string stackCountLimitString = Widgets.TextField(new Rect(stackCountStringSize.x + 8f, stackCountY, winRect.width - stackCountStringSize.x - 8f, 35f), Filter.StackCountLimit.ToString());
                    if (int.TryParse(stackCountLimitString, out int stackCountLimit))
                        Filter.StackCountLimit = stackCountLimit;*/
                    string stackSizeString = "ASF_StackSizeLimit".Translate();
                    Vector2 stackSizeStringSize = Text.CalcSize(stackSizeString);
                    Widgets.Label(new Rect(0f, stackLimitY + 35f / 2 - stackSizeStringSize.y / 2, stackSizeStringSize.x, 35f), stackSizeString);
                    string stackSizeLimitString
                        = Widgets.TextField(new Rect(stackSizeStringSize.x + 8f, stackLimitY, winRect.width - stackSizeStringSize.x - 8f, 35f),
                            Filter.StackSizeLimit.ToString());
                    if (int.TryParse(stackSizeLimitString, out int stackSizeLimit))
                        Filter.StackSizeLimit = stackSizeLimit;
                }
                if (Widgets.ButtonText(new Rect(0f, saveLoadY, winRect.width / 2f - 4f, 35f), "ASF_SaveFilter".Translate()))
                {
                    if (StorageFiltersData.SavedFilters.TryGetValue(key) != null)
                    {
                        Find.WindowStack.Add(new Dialog_Confirmation(storageTab, storeSettingsParent, "ASF_ConfirmOverwriteSavedFilter".Translate(key), delegate
                        {
                            StorageFiltersData.SavedFilters.SetOrAdd(key, Filter);
                            SaveUtils.Save();
                            Messages.Message("ASF_SavedFilter".Translate(key), MessageTypeDefOf.TaskCompletion, false);
                        }, this));
                    }
                    else
                    {
                        StorageFiltersData.SavedFilters.SetOrAdd(key, Filter);
                        SaveUtils.Save();
                        Messages.Message("ASF_SavedFilter".Translate(key), MessageTypeDefOf.TaskCompletion, false);
                    }
                    Event.current.Use();
                }
                if (Widgets.ButtonText(new Rect(winRect.width / 2f + 4f, saveLoadY, winRect.width / 2f - 4f, 35f), "ASF_LoadSavedFilter".Translate()))
                {
                    if (StorageFiltersData.SavedFilters.Count > 0)
                    {
                        List<FloatMenuOption> filterFloatMenuOptions = new List<FloatMenuOption>();
                        FloatMenu filterFloatMenu = null;
                        string removeString = "ASF_DeleteSavedFilter".Translate();
                        float renameX = Text.CalcSize(renameString).x + 8f;
                        float removeX = Text.CalcSize(removeString).x + 8f;
                        SaveUtils.Load();
                        foreach (KeyValuePair<string, ExtraThingFilter> entry in StorageFiltersData.SavedFilters)
                            filterFloatMenuOptions.Add(new FloatMenuOption(entry.Key, delegate
                            {
                                string oldCurName = curName;
                                curName = entry.Key;
                                if (CheckCurName())
                                    Filter.CopyFrom(entry.Value);
                                else
                                    curName = oldCurName;
                            }, extraPartWidth: renameX + removeX, extraPartOnGUI: delegate(Rect extraRect)
                            {
                                Rect renameRect = extraRect;
                                renameRect.width = renameX;
                                _ = new FloatMenuOption(renameString, delegate
                                {
                                    filterFloatMenu.Close();
                                    Find.WindowStack.Add(new Dialog_RenameSavedFilter(this, entry.Key, entry.Value));
                                }).DoGUI(renameRect, false, null);
                                Rect removeRect = extraRect;
                                removeRect.width = removeX;
                                removeRect.x += renameRect.width;
                                _ = new FloatMenuOption(removeString, delegate
                                {
                                    filterFloatMenu.Close();
                                    Find.WindowStack.Add(new Dialog_Confirmation(storageTab, storeSettingsParent,
                                        "ASF_ConfirmDeleteSavedFilter".Translate(entry.Key), delegate
                                        {
                                            _ = StorageFiltersData.SavedFilters.Remove(entry.Key);
                                            SaveUtils.Save();
                                            Messages.Message("ASF_DeletedSavedFilter".Translate(entry.Key), MessageTypeDefOf.TaskCompletion, false);
                                        }, this));
                                }).DoGUI(removeRect, false, null);
                                return false;
                            }));
                        filterFloatMenu = new FloatMenu(filterFloatMenuOptions);
                        Find.WindowStack.Add(filterFloatMenu);
                    }
                    else
                    {
                        Messages.Message("ASF_NoSavedFilters".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                    Event.current.Use();
                }
            }
            else
            {
                if (Widgets.ButtonText(new Rect(0f, renameY, winRect.width, 35f), "ASF_RemoveThisNIPF".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_Confirmation(storageTab, storeSettingsParent, "ASF_ConfirmRemoveNIPF".Translate(key),
                        !(Filter.NextInPriorityFilter is null) ? "ASF_ConfirmRemoveNIPF_RemoveMore".Translate() : null, delegate
                        {
                            Filter.NextInPriorityFilterParent.NextInPriorityFilter = null;
                            Filter = null;
                            if (!(previousDialog is null))
                                StorageFiltersData.SetCurrentFilterDepth(storeSettingsParent, previousDialog.Filter.FilterDepth);
                            Find.WindowStack.Add(previousDialog);
                        }, this));
                    Event.current.Use();
                }
            }
            if (!keyIsMainFilterString)
            {
                float priorityY = saveLoadY + 35f + 8f;
                float x = 0f;
                float width = winRect.width;
                if (!(previousDialog is null))
                {
                    string backString = "ASF_PreviousNIPF".Translate();
                    float backStringX = Text.CalcSize(backString).x + 30f;
                    if (Widgets.ButtonText(new Rect(x, priorityY, backStringX, 35f), backString))
                    {
                        StorageFiltersData.SetCurrentFilterDepth(storeSettingsParent, previousDialog.Filter.FilterDepth);
                        Find.WindowStack.Add(previousDialog);
                        Event.current.Use();
                    }
                    x += backStringX + 8f;
                    width -= backStringX + 8f;
                }
                void EditNIPF()
                {
                    string nipKey = Filter.NextInPriorityFilterParent is null
                        ? "ASF_HierarchyNIPF".Translate(key, Filter.NextInPriorityFilter.FilterDepth).ToString()
                        : key?.Substring(0, key.Length - (Filter.NextInPriorityFilter.FilterDepth - 1).ToString().Length)
                        + Filter.NextInPriorityFilter.FilterDepth;
                    StorageFiltersData.SetCurrentFilterDepth(storeSettingsParent, Filter.NextInPriorityFilter.FilterDepth);
                    Find.WindowStack.Add(new Dialog_EditFilter(storageTab, storeSettingsParent, nipKey, Filter.NextInPriorityFilter,
                        previousEditFilterDialog: this));
                }
                if (Filter.NextInPriorityFilter is null)
                {
                    if (Widgets.ButtonText(new Rect(x, priorityY, width, 35f), "ASF_AddNIPF".Translate()))
                    {
                        Filter.NextInPriorityFilter = new ExtraThingFilter(Filter, Filter.FilterDepth + 1);
                        EditNIPF();
                        Event.current.Use();
                    }
                }
                else
                {
                    if (Widgets.ButtonText(new Rect(x, priorityY, width, 35f), "ASF_EditNIPF".Translate()))
                    {
                        EditNIPF();
                        Event.current.Use();
                    }
                }
            }
        }
    }
}