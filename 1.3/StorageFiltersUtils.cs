using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;

namespace StorageFilters
{
    public static class StorageFiltersUtils
    {
		public static IStoreSettingsParent GetStoreSettingsParent(object obj)
		{
			IStoreSettingsParent storeSettingsParent = obj as IStoreSettingsParent;
			if (storeSettingsParent != null)
			{
				return storeSettingsParent;
			}
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
			return null;
		}

		public static IStoreSettingsParent GetSelectedStoreSettingsParent()
		{
			return GetStoreSettingsParent(Find.Selector.SingleSelectedObject);
		}

		public static void FilterSelectionButton(IStoreSettingsParent storeSettingsParent, ExtraThingFilters tabFilters, string mainFilterString, string tabFilter, Rect position)
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
					StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
				}, extraPartWidth: 60f, extraPartOnGUI: delegate (Rect extraRect)
				{
					new FloatMenuOption("Edit", delegate ()
					{
						filterFloatMenu.Close();
						StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
						Find.WindowStack.Add(new Dialog_EditFilter(mainFilterString, true, tabFilters, storeSettingsParent));
					}).DoGUI(extraRect, false, null);
					return false;
				})));
				if (tabFilters.Count > 0)
				{
					foreach (KeyValuePair<string, ExtraThingFilter> entry in tabFilters)
					{
						Action action = delegate ()
						{
							StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, entry.Key);
						};
						FloatMenuOption floatMenuOption = null;
						floatMenuOption = newFilterOption(new FloatMenuOption(entry.Key, action, extraPartWidth: 180f, extraPartOnGUI: delegate (Rect extraRect)
						{
							Rect renameRect = extraRect;
							renameRect.width /= 3f;
							new FloatMenuOption("Edit", delegate ()
							{
								filterFloatMenu.Close();
								Find.WindowStack.Add(new Dialog_EditFilter(entry.Key, entry.Value, tabFilters, storeSettingsParent));
								StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, entry.Key);
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
							}).DoGUI(toggleRect, false, null);
							Rect removeRect = extraRect;
							removeRect.width /= 3f;
							removeRect.x += renameRect.width + toggleRect.width;
							new FloatMenuOption("Remove", delegate ()
							{
								filterFloatMenu.Close();
								Find.WindowStack.Add(new Dialog_Confirmation("Are you sure you want to remove the filter '" + entry.Key + "'?", delegate ()
								{
									tabFilters.Remove(entry.Key);
									if (StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent) == entry.Key)
									{
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
					Find.WindowStack.Add(new Dialog_NewFilter(tabFilters, storeSettingsParent));
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
