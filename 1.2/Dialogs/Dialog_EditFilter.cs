using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace StorageFilters
{
    internal class Dialog_EditFilter : Window
    {
        public override Vector2 InitialSize => new Vector2(320f, 187f);

        protected override void SetInitialSizeAndPosition()
        {
            windowRect = GenUtils.GetDialogSizeAndPosition(this);
        }

        private readonly ITab_Storage storageTab;
        public Dialog_EditFilter(ITab_Storage instance, IStoreSettingsParent storeSettingsParent)
        {
            doCloseX = true;
            forcePause = true;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = false;
            storageTab = instance;
            this.storeSettingsParent = storeSettingsParent;
        }

        public ExtraThingFilter Filter;

        private readonly bool keyIsMainFilterString;
        private string key;
        private readonly ExtraThingFilters tabFilters;
        private readonly IStoreSettingsParent storeSettingsParent;
        private string curName;
        private readonly Dialog_EditFilter previousDialog;
        public Dialog_EditFilter(ITab_Storage instance, IStoreSettingsParent storeSettingsParent, string key, ExtraThingFilter value, ExtraThingFilters tabFilters = null, Dialog_EditFilter previousEditFilterDialog = null) : this(instance, storeSettingsParent)
        {
            this.key = key;
            Filter = value;
            this.tabFilters = tabFilters;
            curName = key;
            previousDialog = previousEditFilterDialog;
        }

        public Dialog_EditFilter(ITab_Storage instance, IStoreSettingsParent storeSettingsParent, string key, bool keyIsMainFilterString, ExtraThingFilters tabFilters) : this(instance, storeSettingsParent, key, null, tabFilters)
        {
            this.keyIsMainFilterString = keyIsMainFilterString;
            Filter = new ExtraThingFilter(storeSettingsParent.GetStoreSettings().filter);
        }

        private bool CheckCurName()
        {
            if (NamePlayerFactionDialogUtility.IsValidName(curName) && Text.CalcSize(curName).x <= StorageFiltersData.MaxFilterStringWidth)
            {
                if (key == curName || (StorageFiltersData.MainFilterString.TryGetValue(storeSettingsParent) != curName && !tabFilters.ContainsKey(curName)))
                {
                    if (key != curName)
                    {
                        if (keyIsMainFilterString)
                        {
                            StorageFiltersData.MainFilterString.SetOrAdd(storeSettingsParent, curName);
                        }
                        else
                        {
                            tabFilters.Remove(key);
                            tabFilters.Add(curName, Filter);
                        }
                        if (StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent) == key)
                        {
                            StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, curName);
                        }
                        key = curName;
                        GenUtils.PlayClick();
                        return true;
                    }
                }
                else
                {
                    Messages.Message("Storage area already has a filter named '" + curName + "'", MessageTypeDefOf.RejectInput, false);
                }
            }
            else
            {
                Messages.Message("Invalid string", MessageTypeDefOf.RejectInput, false);
            }
            return false;
        }

        public override void DoWindowContents(Rect winRect)
        {
            if (!GenUtils.IsStorageTabOpen(storageTab, storeSettingsParent))
            {
                Find.WindowStack.TryRemove(this, false);
                return;
            }
            if (Widgets.CloseButtonFor(windowRect.AtZero()))
            {
                Find.WindowStack.TryRemove(this, true);
                Event.current.Use();
                return;
            }
            if (Filter is null || (Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Escape || Event.current.keyCode == KeyCode.Return)))
            {
                Find.WindowStack.TryRemove(this, true);
                Event.current.Use();
                return;
            }
            if (tabFilters != null)
            {
                string mainFilterString = StorageFiltersData.MainFilterString.TryGetValue(storeSettingsParent);
                if (keyIsMainFilterString && mainFilterString != null)
                {
                    StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
                }
                else if (key != null)
                {
                    StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, key);
                }

                StorageFiltersData.CurrentFilterDepth.SetOrAdd(storeSettingsParent, Filter.FilterDepth);
            }
            Text.Font = GameFont.Small;
            string editString = "Editing filter: '" + key + "'";
            float editStringY = Text.CalcSize(editString).y;
            Widgets.Label(new Rect(0f, 0f, winRect.width, editStringY), editString);
            float renameY = editStringY + 8f;
            string renameString = "Rename".Translate();
            float saveLoadY = renameY + 35f + 8f;
            if (tabFilters != null)
            {
                float renameStringX = Text.CalcSize(renameString).x + 30f;
                curName = Widgets.TextField(new Rect(0f, renameY, winRect.width - 8f - renameStringX, 35f), curName);
                if (Text.CalcSize(curName).x > StorageFiltersData.MaxFilterStringWidth)
                {
                    curName = curName.Substring(0, curName.Length - 1);
                }
                if (Widgets.ButtonText(new Rect(winRect.width - renameStringX, renameY, renameStringX, 35f), renameString))
                {
                    CheckCurName();
                    Event.current.Use();
                }
                if (Widgets.ButtonText(new Rect(0f, saveLoadY, winRect.width / 2f - 4f, 35f), "Save".Translate()))
                {
                    if (StorageFiltersData.SavedFilter.TryGetValue(key) != null)
                    {
                        Find.WindowStack.Add(new Dialog_Confirmation(storageTab, storeSettingsParent, "Are you sure you want to overwrite the saved filter '" + key + "'?", delegate ()
                        {
                            StorageFiltersData.SavedFilter.SetOrAdd(key, Filter);
                            SaveUtils.Save();
                            Messages.Message("Saved filter '" + key + "'", MessageTypeDefOf.TaskCompletion, false);
                        }, this));
                    }
                    else
                    {
                        StorageFiltersData.SavedFilter.SetOrAdd(key, Filter);
                        SaveUtils.Save();
                        Messages.Message("Saved filter '" + key + "'", MessageTypeDefOf.TaskCompletion, false);
                    }
                    Event.current.Use();
                }
                if (Widgets.ButtonText(new Rect(winRect.width / 2f + 4f, saveLoadY, winRect.width / 2f - 4f, 35f), "Load".Translate()))
                {
                    if (StorageFiltersData.SavedFilter.Count > 0)
                    {
                        List<FloatMenuOption> filterFloatMenuOptions = new List<FloatMenuOption>();
                        FloatMenu filterFloatMenu = null;
                        foreach (KeyValuePair<string, ExtraThingFilter> entry in StorageFiltersData.SavedFilter)
                        {
                            filterFloatMenuOptions.Add(new FloatMenuOption(entry.Key, delegate ()
                            {
                                string oldCurName = curName;
                                curName = entry.Key;
                                if (CheckCurName())
                                {
                                    Filter.CopyFrom(entry.Value);
                                }
                                else
                                {
                                    curName = oldCurName;
                                }
                            }, extraPartWidth: 120f, extraPartOnGUI: delegate (Rect extraRect)
                            {
                                Rect renameRect = extraRect;
                                renameRect.width /= 2f;
                                new FloatMenuOption(renameString, delegate ()
                                {
                                    filterFloatMenu.Close();
                                    Find.WindowStack.Add(new Dialog_RenameSavedFilter(storageTab, this, entry.Key, entry.Value));
                                }).DoGUI(renameRect, false, null);
                                Rect removeRect = extraRect;
                                removeRect.width /= 2f;
                                removeRect.x += renameRect.width;
                                new FloatMenuOption("Delete".Translate(), delegate ()
                                {
                                    filterFloatMenu.Close();
                                    Find.WindowStack.Add(new Dialog_Confirmation(storageTab, storeSettingsParent, "Are you sure you want to delete the saved filter '" + entry.Key + "'?", delegate ()
                                    {
                                        StorageFiltersData.SavedFilter.Remove(entry.Key);
                                        SaveUtils.Save();
                                        Messages.Message("Deleted saved filter '" + entry.Key + "'", MessageTypeDefOf.TaskCompletion, false);
                                    }, this));
                                }).DoGUI(removeRect, false, null);
                                return false;
                            }));
                        }
                        filterFloatMenu = new FloatMenu(filterFloatMenuOptions);
                        Find.WindowStack.Add(filterFloatMenu);
                    }
                    else
                    {
                        Messages.Message("No saved filters", MessageTypeDefOf.RejectInput, false);
                    }
                    Event.current.Use();
                }
            }
            else
            {
                if (Widgets.ButtonText(new Rect(0f, renameY, winRect.width, 35f), "Remove This Next-In-Priority Filter"))
                {
                    Find.WindowStack.Add(new Dialog_Confirmation(storageTab, storeSettingsParent, "Are you sure you want to remove the next-in-priority filter '" + key + "'?", !(Filter.NextInPriorityFilter is null) ? "This will also remove all next-in-priority filters from this point onward!" : null, delegate ()
                    {
                        Filter.NextInPriorityFilterParent.NextInPriorityFilter = null;
                        Filter = null;
                        if (!(previousDialog is null))
                        {
                            StorageFiltersData.CurrentFilterDepth.SetOrAdd(storeSettingsParent, previousDialog.Filter.FilterDepth);
                        }

                        Find.WindowStack.Add(previousDialog);
                    }, this));
                    Event.current.Use();
                }
            }
            if (!keyIsMainFilterString)
            {
                float priorityY = saveLoadY + 35f + 8f;

                float X = 0f;
                float width = winRect.width;
                if (!(previousDialog is null))
                {
                    string backString = "Back";
                    float backStringX = Text.CalcSize(backString).x + 30f;
                    if (Widgets.ButtonText(new Rect(X, priorityY, backStringX, 35f), backString))
                    {
                        StorageFiltersData.CurrentFilterDepth.SetOrAdd(storeSettingsParent, previousDialog.Filter.FilterDepth);
                        Find.WindowStack.Add(previousDialog);
                        Event.current.Use();
                    }
                    X += backStringX + 8f;
                    width -= backStringX + 8f;
                }

                void EditNIPF()
                {
                    string key_NIP;
                    if (Filter.NextInPriorityFilterParent != null)
                    {
                        key_NIP = key.Substring(0, key.Length - (Filter.NextInPriorityFilter.FilterDepth - 1).ToString().Length) + Filter.NextInPriorityFilter.FilterDepth;
                    }
                    else
                    {
                        key_NIP = key + " - N.I.P.F. #" + Filter.NextInPriorityFilter.FilterDepth;
                    }
                    StorageFiltersData.CurrentFilterDepth.SetOrAdd(storeSettingsParent, Filter.NextInPriorityFilter.FilterDepth);
                    Find.WindowStack.Add(new Dialog_EditFilter(storageTab, storeSettingsParent, key_NIP, Filter.NextInPriorityFilter, previousEditFilterDialog: this));
                }
                if (Filter.NextInPriorityFilter is null)
                {
                    if (Widgets.ButtonText(new Rect(X, priorityY, width, 35f), "Add Next-In-Priority Filter"))
                    {
                        Filter.NextInPriorityFilter = new ExtraThingFilter
                        {
                            NextInPriorityFilter = null,
                            FilterDepth = Filter.FilterDepth + 1
                        };
                        EditNIPF();
                        Event.current.Use();
                    }
                }
                else
                {
                    if (Widgets.ButtonText(new Rect(X, priorityY, width, 35f), "Edit Next-In-Priority Filter"))
                    {
                        EditNIPF();
                        Event.current.Use();
                    }
                }
            }
        }
    }
}
