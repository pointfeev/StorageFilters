using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using System.Reflection;

namespace StorageFilters
{
    public static class StorageFilters
    {
		public static Rect? StorageTabRect = null;

		public static void FillTab(ITab_Storage instance, Vector2 size)
        {
			try
            {
				_ = (float)instance.GetType().GetMethod("get_TopAreaHeight", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance, new object[] { });
			}
			catch
			{
				return; // if it can't get the TopAreaHeight, it means it's a container without priority and thus not actually storage
			}

			IStoreSettingsParent storeSettingsParent = StorageFiltersUtils.GetSelectedStoreSettingsParent();
			if (storeSettingsParent == null)
			{
				Log.Warning("[StorageFilters] Unable to retrieve selected storeSettingsParent");
				return;
			}
			ExtraThingFilters tabFilters = StorageFiltersData.Filters.TryGetValue(storeSettingsParent);
			if (tabFilters == null)
			{
				StorageFiltersData.Filters.SetOrAdd(storeSettingsParent, new ExtraThingFilters());
				tabFilters = StorageFiltersData.Filters.TryGetValue(storeSettingsParent);
			}
			string mainFilterString = StorageFiltersData.MainFilterString.TryGetValue(storeSettingsParent);
			if (mainFilterString == null)
			{
				StorageFiltersData.MainFilterString.SetOrAdd(storeSettingsParent, StorageFiltersData.DefaultMainFilterString);
				mainFilterString = StorageFiltersData.MainFilterString.TryGetValue(storeSettingsParent);
			}
			string tabFilter = StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent);
			if (tabFilter == null)
			{
				StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, mainFilterString);
				tabFilter = StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent);
			}

			Rect window = new Rect(0, 0, size.x, size.y);
			StorageTabRect = window;
			GUI.BeginGroup(window.ContractedBy(10f));
			Rect position = new Rect(166f, 0, Text.CalcSize(tabFilter).x + 16f, 29f);
			StorageFiltersUtils.FilterSelectionButton(instance, storeSettingsParent, tabFilters, mainFilterString, tabFilter, position);
			GUI.EndGroup();
		}

		public static void DoThingFilterConfigWindow(ref ThingFilter filter)
        {
			IStoreSettingsParent storeSettingsParent = StorageFiltersUtils.GetSelectedStoreSettingsParent();
			if (storeSettingsParent != null)
			{
				StorageSettings settings = storeSettingsParent.GetStoreSettings();
				if (settings != null && settings.filter == filter)
				{ 
					ExtraThingFilters tabFilters = StorageFiltersData.Filters.TryGetValue(storeSettingsParent);
					string tabFilter = StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent);
					if (tabFilters != null && tabFilter != null)
					{
						ExtraThingFilter _filter = tabFilters.Get(tabFilter);
						if (_filter != null)
						{
							filter = _filter;
						}
					}
				}
			}
		}

		public static bool AllowsThingOrThingDef(this ThingFilter thingFilter, object thingOrThingDef)
        {
			if (thingOrThingDef is Thing)
				return thingFilter.Allows(thingOrThingDef as Thing);
			else if (thingOrThingDef is ThingDef)
				return thingFilter.Allows(thingOrThingDef as ThingDef);
			Log.Error("[StorageFilters] AllowsThingOrThingDef called with an object that is not a Thing nor ThingDef");
			return false;
        }

		public static bool AllowedToAcceptThingOrThingDef(this StorageSettings storageSettings, object thingOrThingDef)
		{
			if (thingOrThingDef is Thing)
				return storageSettings.AllowedToAccept(thingOrThingDef as Thing);
			else if (thingOrThingDef is ThingDef)
				return storageSettings.AllowedToAccept(thingOrThingDef as ThingDef);
			Log.Error("[StorageFilters] AllowedToAcceptThingOrThingDef called with an object that is not a Thing nor ThingDef");
			return false;
		}

		public static bool IsInBetterStorageThan(this Thing thing, IStoreSettingsParent owner)
        {
			IHaulDestination haulDestination = StoreUtility.CurrentHaulDestinationOf(thing);
			return haulDestination != null && haulDestination.GetStoreSettings().Priority > owner.GetStoreSettings().Priority;
		}

		public static void AllowedToAccept(IStoreSettingsParent owner, ThingFilter filter, object thingOrThingDef, ref bool result)
        {
			if (!(thingOrThingDef is Thing) && !(thingOrThingDef is ThingDef))
				Log.Error("[StorageFilters] AllowedToAccept called with an object that is not a Thing nor ThingDef");
			ExtraThingFilters tabFilters = null;
			if (owner != null)
            {
				tabFilters = StorageFiltersData.Filters.TryGetValue(owner);
			}
			if (tabFilters != null && tabFilters.Count > 0)
			{
				bool accepted = false;
				if (filter.AllowsThingOrThingDef(thingOrThingDef))
				{
					accepted = true;
				}
				if (!accepted)
				{
					foreach (KeyValuePair<string, ExtraThingFilter> entry in tabFilters)
					{
						if (accepted)
						{
							break;
						}
						if (entry.Value.Enabled)
                        {
							ExtraThingFilter currentFilter = entry.Value;
							while (currentFilter != null)
							{
								if (currentFilter.AllowsThingOrThingDef(thingOrThingDef))
								{
									accepted = true;
									break;
								}
								else if (currentFilter.NextInPriorityFilter != null)
								{
									bool allowedItemExistsAndCanBeUsed = false;
									foreach (ThingDef _thingDef in currentFilter.AllowedThingDefs)
                                    {
										bool allowedItemCanBeUsed = false;
										foreach (Thing thingOfDef in Find.CurrentMap.listerThings.ThingsOfDef(_thingDef))
                                        {
											if (currentFilter.Allows(thingOfDef) && !thingOfDef.IsInBetterStorageThan(owner))
                                            {
												allowedItemCanBeUsed = true;
												break;
											}
                                        }
										if (allowedItemCanBeUsed)
                                        {
											allowedItemExistsAndCanBeUsed = true;
											break;
										}
									}
									if (allowedItemExistsAndCanBeUsed)
									{
										accepted = false;
										break;
									}
									else
									{
										currentFilter = currentFilter.NextInPriorityFilter;
									}
								}
								else
								{
									break;
								}
							}
						}
					}
				}
				if (!accepted)
				{
					result = false;
					return;
				}
			}
			else
			{
				if (!filter.AllowsThingOrThingDef(thingOrThingDef))
				{
					result = false;
					return;
				}
			}
			if (owner != null)
			{
				StorageSettings parentStoreSettings = owner.GetParentStoreSettings();
				if (parentStoreSettings != null && !parentStoreSettings.AllowedToAcceptThingOrThingDef(thingOrThingDef))
				{
					result = false;
					return;
				}
			}
			result = true;
		}

		public static void AllowedToAccept(IStoreSettingsParent owner, ThingFilter filter, ThingDef thingDef, ref bool result)
		{
			AllowedToAccept(owner, filter, thingDef as object, ref result);
		}

		public static void AllowedToAccept(IStoreSettingsParent owner, ThingFilter filter, Thing thing, ref bool result)
		{
			AllowedToAccept(owner, filter, thing as object, ref result);
		}

		private static ExtraThingFilters copiedFilters = null;
		private static string copiedMainFilterString = null;
		private static string copiedCurrentFilterKey = null;

		public static void Copy(StorageSettings storageSettings)
        {
			IStoreSettingsParent storeSettingsParent = storageSettings.owner;
			if (storeSettingsParent == null) { return; }
			ExtraThingFilters filters = StorageFiltersData.Filters.TryGetValue(storeSettingsParent);
			if (filters != null)
            {
				copiedFilters = new ExtraThingFilters();
				foreach (KeyValuePair<string, ExtraThingFilter> entry in filters)
                {
					copiedFilters.Set(entry.Key, entry.Value.Copy());
                }
            }
			copiedMainFilterString = StorageFiltersData.MainFilterString.TryGetValue(storeSettingsParent);
			copiedCurrentFilterKey = StorageFiltersData.CurrentFilterKey.TryGetValue(storeSettingsParent);
		}

		public static void Paste(StorageSettings storageSettings)
        {
			IStoreSettingsParent storeSettingsParent = storageSettings.owner;
			if (storeSettingsParent == null) { return; }
			if (copiedFilters != null)
            {
				StorageFiltersData.Filters.SetOrAdd(storeSettingsParent, copiedFilters);
			}
			if (copiedMainFilterString != null)
			{
				StorageFiltersData.MainFilterString.SetOrAdd(storeSettingsParent, copiedMainFilterString);
			}
			if (copiedCurrentFilterKey != null)
			{
				StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, copiedCurrentFilterKey);
			}
		}
	}
}
