using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using Verse.Sound;

namespace StorageFilters
{
    public static class StorageFiltersUtils
    {
		public static bool IsStorageTabOpen(ITab_Storage storageTab, IStoreSettingsParent storeSettingsParent)
        {
			if (storageTab is null || !storageTab.IsVisible)
				return false;
			if (GetSelectedStoreSettingsParent() != storeSettingsParent)
				return false;
			foreach (Window window in Find.WindowStack.Windows)
				if (window.ID == -235086)
					return window.IsOpen;
			return false;
		}

		public static void PlayClick()
        {
			SoundDefOf.Click.PlayOneShotOnCamera(null);
		}

		public static Rect GetDialogSizeAndPosition(Window dialog, Window editDialog = null)
        {
			Vector2 initialSize = dialog.InitialSize;
			float X = (UI.screenWidth - initialSize.x) / 2f;
			float Y = (UI.screenHeight - initialSize.y) / 2f;
			MainTabWindow_Inspect inspectPane = MainButtonDefOf.Inspect.TabWindow as MainTabWindow_Inspect;
			if (!(inspectPane is null) && StorageFilters.StorageTabRect.HasValue)
			{
				X = StorageFilters.StorageTabRect.Value.xMax - 1f;
				Y = inspectPane.PaneTopY - 30f - StorageFilters.StorageTabRect.Value.height;
				if (!(editDialog is null))
				{
					X += editDialog.InitialSize.x - 1f;
				}
			}
			return new Rect(X, Y, initialSize.x, initialSize.y).Rounded();
		}

		public static IStoreSettingsParent GetStoreSettingsParent(object obj)
		{
			if (!(obj is null))
            {
				IStoreSettingsParent storeSettingsParent = obj as IStoreSettingsParent;
				if (storeSettingsParent != null)
					return storeSettingsParent;
				ThingWithComps thingWithComps = obj as ThingWithComps;
				if (thingWithComps != null)
				{
					List<ThingComp> allComps = thingWithComps.AllComps;
					for (int i = 0; i < allComps.Count; i++)
					{
						storeSettingsParent = allComps[i] as IStoreSettingsParent;
						if (storeSettingsParent != null)
						{
							return storeSettingsParent;
						}
					}
				}
			}
			return null;
		}

		public static IStoreSettingsParent GetSelectedStoreSettingsParent()
		{
			return GetStoreSettingsParent(Find.Selector.SingleSelectedObject);
		}

		public static void FilterSelectionButton(ITab_Storage instance, IStoreSettingsParent storeSettingsParent, ExtraThingFilters tabFilters, string mainFilterString, string tabFilter, Rect position)
		{
			Text.Font = GameFont.Small;
			if (Widgets.ButtonText(position, tabFilter, true, true, true))
			{
				Dictionary<FloatMenuOption, int> floatMenuOptionOrder = new Dictionary<FloatMenuOption, int>();
				Func<FloatMenuOption, FloatMenuOption> newFilterOption = delegate (FloatMenuOption floatMenuOption)
				{
					floatMenuOptionOrder.SetOrAdd(floatMenuOption, floatMenuOptionOrder.Count);
					return floatMenuOption;
				};
				List<FloatMenuOption> filterFloatMenuOptions = new List<FloatMenuOption>();
				FloatMenu filterFloatMenu = null;
				filterFloatMenuOptions.Add(newFilterOption(new FloatMenuOption(mainFilterString, delegate ()
				{
					if (!(Find.WindowStack.WindowOfType<Dialog_EditFilter>() is null))
					{
						Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, mainFilterString, true, tabFilters));
					}
					StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
					StorageFiltersData.CurrentlyEditingFilter = null;
				}, extraPartWidth: 60f, extraPartOnGUI: delegate (Rect extraRect)
				{
					new FloatMenuOption("Edit", delegate ()
					{
						filterFloatMenu.Close();
						Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, mainFilterString, true, tabFilters));
						StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
						StorageFiltersData.CurrentlyEditingFilter = null;
					}).DoGUI(extraRect, false, null);
					return false;
				})));
				if (tabFilters.Count > 0)
				{
					foreach (KeyValuePair<string, ExtraThingFilter> entry in tabFilters)
					{
						Action action = delegate ()
						{
							if (!(Find.WindowStack.WindowOfType<Dialog_EditFilter>() is null))
                            {
								Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, entry.Key, entry.Value, tabFilters));
							}
							StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, entry.Key);
							StorageFiltersData.CurrentlyEditingFilter = entry.Value;
						};
						FloatMenuOption floatMenuOption = null;
						floatMenuOption = newFilterOption(new FloatMenuOption(entry.Key, action, extraPartWidth: 180f, extraPartOnGUI: delegate (Rect extraRect)
						{
							Rect renameRect = extraRect;
							renameRect.width /= 3f;
							new FloatMenuOption("Edit", delegate ()
							{
								filterFloatMenu.Close();
								Find.WindowStack.Add(new Dialog_EditFilter(instance, storeSettingsParent, entry.Key, entry.Value, tabFilters));
								StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, entry.Key);
								StorageFiltersData.CurrentlyEditingFilter = entry.Value;
							}).DoGUI(renameRect, false, null);
							Rect toggleRect = extraRect;
							toggleRect.width /= 3f;
							toggleRect.x += renameRect.width;
							new FloatMenuOption(entry.Value.Enabled ? "Disable" : "Enable", delegate ()
							{
								entry.Value.Enabled = !entry.Value.Enabled;
								if (entry.Value.Enabled)
								{
									floatMenuOption.action = action;
								}
								else
								{
									floatMenuOption.Disabled = true;
								}
								PlayClick();
							}).DoGUI(toggleRect, false, null);
							Rect removeRect = extraRect;
							removeRect.width /= 3f;
							removeRect.x += renameRect.width + toggleRect.width;
							new FloatMenuOption("Remove", delegate ()
							{
								filterFloatMenu.Close();
								Find.WindowStack.Add(new Dialog_Confirmation(instance, storeSettingsParent, "Are you sure you want to remove the filter '" + entry.Key + "'?", delegate ()
								{
									tabFilters.Remove(entry.Key);
									if (StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent) == entry.Key)
									{
										Find.WindowStack.TryRemove(typeof(Dialog_EditFilter), true);
										StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
									}
								}));
							}).DoGUI(removeRect, false, null);
							return false;
						}));
						floatMenuOption.Disabled = !entry.Value.Enabled;
						filterFloatMenuOptions.Add(floatMenuOption);
					}
				}
				filterFloatMenuOptions.Add(newFilterOption(new FloatMenuOption("New filter", delegate ()
				{
					Find.WindowStack.TryRemove(typeof(Dialog_EditFilter), false);
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
