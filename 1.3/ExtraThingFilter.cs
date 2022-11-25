using RimWorld;

using System.Collections.Generic;

using Verse;

namespace StorageFilters
{
    public class ExtraThingFilter : ThingFilter
    {
        public bool Enabled = true;

        private ExtraThingFilter nextInPriorityFilterParent;

        public ExtraThingFilter NextInPriorityFilterParent
        {
            get
            {
                if (!(nextInPriorityFilterParent is null))
                    return nextInPriorityFilterParent;
                else
                {
                    foreach (KeyValuePair<IStoreSettingsParent, ExtraThingFilters> filters in StorageFiltersData.Filters)
                        foreach (KeyValuePair<string, ExtraThingFilter> filter in filters.Value)
                        {
                            ExtraThingFilter currentFilter = filter.Value;
                            while (true)
                            {
                                if (currentFilter.NextInPriorityFilter == this)
                                {
                                    nextInPriorityFilterParent = currentFilter;
                                    return currentFilter;
                                }
                                else if (currentFilter.NextInPriorityFilter != null)
                                    currentFilter = currentFilter.NextInPriorityFilter;
                                else
                                    break;
                            }
                        }
                }
                return null;
            }
        }

        public ExtraThingFilter NextInPriorityFilter = null;
        public int FilterDepth = 0;

        public ExtraThingFilter() : base() { }

        public ThingFilter OriginalFilter = null;

        public ExtraThingFilter(ThingFilter originalFilter) : this()
        {
            CopyAllowancesFrom(originalFilter);
            OriginalFilter = originalFilter;
        }

        public void CopyFrom(ExtraThingFilter otherFilter)
        {
            Enabled = otherFilter.Enabled;
            FilterDepth = otherFilter.FilterDepth;
            if (!(otherFilter.NextInPriorityFilter is null))
                NextInPriorityFilter = otherFilter.NextInPriorityFilter.Copy();
            CopyAllowancesFrom(otherFilter);
            OriginalFilter?.CopyAllowancesFrom(this);
        }

        public ExtraThingFilter Copy()
        {
            ExtraThingFilter copy = new ExtraThingFilter();
            copy.CopyFrom(this);
            return copy;
        }

        private void SyncWithMainFilter() => OriginalFilter?.CopyAllowancesFrom(this);

        public new void SetAllow(ThingDef thingDef, bool allow)
        {
            base.SetAllow(thingDef, allow);
            SyncWithMainFilter();
        }

        public new void SetAllow(SpecialThingFilterDef sfDef, bool allow)
        {
            base.SetAllow(sfDef, allow);
            SyncWithMainFilter();
        }

        public new void SetAllow(ThingCategoryDef categoryDef, bool allow, IEnumerable<ThingDef> exceptedDefs = null, IEnumerable<SpecialThingFilterDef> exceptedFilters = null)
        {
            base.SetAllow(categoryDef, allow, exceptedDefs, exceptedFilters);
            SyncWithMainFilter();
        }

        public new void SetAllow(StuffCategoryDef cat, bool allow)
        {
            base.SetAllow(cat, allow);
            SyncWithMainFilter();
        }

        public new void SetAllowAllWhoCanMake(ThingDef thing)
        {
            base.SetAllowAllWhoCanMake(thing);
            SyncWithMainFilter();
        }

        public new void SetFromPreset(StorageSettingsPreset preset)
        {
            base.SetFromPreset(preset);
            SyncWithMainFilter();
        }

        public new void SetDisallowAll(IEnumerable<ThingDef> exceptedDefs = null, IEnumerable<SpecialThingFilterDef> exceptedFilters = null)
        {
            base.SetDisallowAll(exceptedDefs, exceptedFilters);
            SyncWithMainFilter();
        }

        public new void SetAllowAll(ThingFilter parentFilter, bool includeNonStorable = false)
        {
            base.SetAllowAll(parentFilter, includeNonStorable);
            SyncWithMainFilter();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (OriginalFilter is null)
            {
                Scribe_Values.Look(ref Enabled, "Enabled", true);
                Scribe_Deep.Look(ref NextInPriorityFilter, false, "NextInPriorityFilter");
                Scribe_Values.Look(ref FilterDepth, "NextInPriorityFilterDepth", 0);
            }
        }
    }
}