using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace StorageFilters
{
	internal class Dialog_EditFilter : Window
	{
		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(320f, 187f);
			}
		}

		public Dialog_EditFilter()
		{
			forcePause = false;
			closeOnAccept = false;
			closeOnCancel = false;
			absorbInputAroundWindow = false;
		}

		private bool keyIsMainFilterString;
		private string key;
		private ExtraThingFilter value;
		private ThingFilter valueMain;
		private ExtraThingFilters tabFilters;
		private IStoreSettingsParent storeSettingsParent;
		private string curName;
		private Dialog_EditFilter previousDialog;

		public Dialog_EditFilter(string key, ExtraThingFilter value, ExtraThingFilters tabFilters, IStoreSettingsParent storeSettingsParent) : this()
        {
			this.key = key;
			this.value = value;
			this.tabFilters = tabFilters;
			this.storeSettingsParent = storeSettingsParent;
			curName = key;
			StorageFiltersData.CurrentlyEditingFilter = value;
		}

		public Dialog_EditFilter(string key, bool keyIsMainFilterString, ExtraThingFilters tabFilters, IStoreSettingsParent storeSettingsParent) : this(key, null, tabFilters, storeSettingsParent)
		{
			this.keyIsMainFilterString = keyIsMainFilterString;
			valueMain = storeSettingsParent.GetStoreSettings().filter;
			value = new ExtraThingFilter(valueMain);
		}

		public Dialog_EditFilter(string key, ExtraThingFilter value, Dialog_EditFilter previousEditFilterDialog) : this(key, value, null, null)
		{
			previousDialog = previousEditFilterDialog;
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
							tabFilters.Add(curName, value);
						}
						if (StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent) == key)
						{
							StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, curName);
						}
						key = curName;
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
			bool close = false;
			if (Widgets.CloseButtonFor(winRect.AtZero()) || value is null)
			{
				close = true;
			}
			else
            {
				StorageFiltersData.CurrentlyEditingFilter = value;
				if (storeSettingsParent != null)
                {
					string mainFilterString = StorageFiltersData.MainFilterString.TryGetValue(storeSettingsParent);
					if (keyIsMainFilterString && mainFilterString != null)
						StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
					else if (key != null)
						StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, key);
				}
				Text.Font = GameFont.Small;
				string editString = "Editing filter: '" + key + "'";
				float editStringY = Text.CalcSize(editString).y;
				Widgets.Label(new Rect(0f, 0f, winRect.width, editStringY), editString);
				float renameY = editStringY + 8f;
				string renameString = "Rename".Translate();
				float saveLoadY = renameY + 35f + 8f;
				if (tabFilters != null && storeSettingsParent != null)
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
							Find.WindowStack.Add(new Dialog_Confirmation("Are you sure you want to overwrite the saved filter '" + key + "'?", delegate ()
							{
								StorageFiltersData.SavedFilter.SetOrAdd(key, value);
								SaveUtils.Save();
								Messages.Message("Saved filter '" + key + "'", MessageTypeDefOf.TaskCompletion, false);
							}));
						}
						else
						{
							StorageFiltersData.SavedFilter.SetOrAdd(key, value);
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
										if (keyIsMainFilterString && valueMain != null)
										{
											valueMain.CopyAllowancesFrom(entry.Value);
										}
										else
										{
											value.CopyFrom(entry.Value);
										}
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
										Find.WindowStack.Add(new Dialog_RenameSavedFilter(entry.Key, entry.Value));
									}).DoGUI(renameRect, false, null);
									Rect removeRect = extraRect;
									removeRect.width /= 2f;
									removeRect.x += renameRect.width;
									new FloatMenuOption("Delete".Translate(), delegate ()
									{
										filterFloatMenu.Close();
										Find.WindowStack.Add(new Dialog_Confirmation("Are you sure you want to delete the saved filter '" + entry.Key + "'?", delegate ()
										{
											StorageFiltersData.SavedFilter.Remove(entry.Key);
											SaveUtils.Save();
											Messages.Message("Deleted saved filter '" + entry.Key + "'", MessageTypeDefOf.TaskCompletion, false);
										}));
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
						Find.WindowStack.Add(new Dialog_Confirmation("Are you sure you want to remove the next-in-priority filter '" + key + "'?", delegate ()
						{
							value.NextInPriorityFilterParent.NextInPriorityFilter = null;
							value = null;
						}));
						Event.current.Use();
					}
				}
				if (!keyIsMainFilterString)
                {
					float priorityY = saveLoadY + 35f + 8f;
					if (value.NextInPriorityFilter is null)
					{
						if (Widgets.ButtonText(new Rect(0f, priorityY, winRect.width, 35f), "Add Next-In-Priority Filter"))
						{
							value.NextInPriorityFilter = new ExtraThingFilter();
							value.NextInPriorityFilter.NextInPriorityFilter = null;
							value.NextInPriorityFilter.NextInPriorityFilterDepth = value.NextInPriorityFilterDepth + 1;
							Event.current.Use();
						}
					}
					else
					{
						if (Widgets.ButtonText(new Rect(0f, priorityY, winRect.width, 35f), "Edit Next-In-Priority Filter"))
						{
							string key_NIP;
							if (value.NextInPriorityFilterParent != null)
							{
								key_NIP = key.Substring(0, key.Length - (value.NextInPriorityFilter.NextInPriorityFilterDepth - 1).ToString().Length) + value.NextInPriorityFilter.NextInPriorityFilterDepth;
							}
							else
							{
								key_NIP = key + " - N.I.P.F. #" + value.NextInPriorityFilter.NextInPriorityFilterDepth;
							}
							Find.WindowStack.Add(new Dialog_EditFilter(key_NIP, value.NextInPriorityFilter, this));
							Event.current.Use();
						}
					}
				}
			}
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
			{
				close = true;
			}
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
			{
				close = true;
			}
			if (close)
			{
				Find.WindowStack.TryRemove(this, true);
				if (previousDialog != null)
				{
					Find.WindowStack.Add(previousDialog);
				}
				Event.current.Use();
			}
		}

        public override void PreClose()
        {
            base.PreClose();
			StorageFiltersData.CurrentlyEditingFilter = null;
		}
    }
}
