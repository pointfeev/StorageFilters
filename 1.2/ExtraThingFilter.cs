using RimWorld;

using System.Collections.Generic;

using Verse;

namespace StorageFilters
{
    public class ExtraThingFilter : ThingFilter
    {
        public bool Enabled = true;
        //public int StackCountLimit = 0;
        public int StackSizeLimit = 0;
        public int FilterDepth = 0;
        private ExtraThingFilter nextInPriorityFilterParent = null;
        public ExtraThingFilter NextInPriorityFilterParent
        {
            get
            {
                if (nextInPriorityFilterParent is null)
                    foreach (KeyValuePair<IStoreSettingsParent, ExtraThingFilters> filters in StorageFiltersData.Filters)
                        foreach (KeyValuePair<string, ExtraThingFilter> filter in filters.Value)
                        {
                            ExtraThingFilter currentFilter = filter.Value;
                            while (nextInPriorityFilterParent is null && !(currentFilter.NextInPriorityFilter is null))
                                if (currentFilter.NextInPriorityFilter == this)
                                {
                                    nextInPriorityFilterParent = currentFilter;
                                    return currentFilter;
                                }
                                else currentFilter = currentFilter.NextInPriorityFilter;
                        }
                return nextInPriorityFilterParent;
            }
            set => nextInPriorityFilterParent = value;
        }
        public ExtraThingFilter NextInPriorityFilter = null;

        public ExtraThingFilter() : base() { }

        public ExtraThingFilter(ExtraThingFilter parent = null, int filterDepth = 0) : this()
        {
            nextInPriorityFilterParent = parent;
            FilterDepth = filterDepth;
        }

        public ThingFilter OriginalFilter = null;
        public ExtraThingFilter(ThingFilter originalFilter) : this()
        {
            CopyAllowancesFrom(originalFilter);
            OriginalFilter = originalFilter;
        }

        public void CopyFrom(ExtraThingFilter otherFilter)
        {
            Enabled = otherFilter.Enabled;
            //StackCountLimit = otherFilter.StackCountLimit;
            StackSizeLimit = otherFilter.StackSizeLimit;
            FilterDepth = otherFilter.FilterDepth;
            if (!(otherFilter.NextInPriorityFilter is null))
            {
                NextInPriorityFilter = otherFilter.NextInPriorityFilter.Copy();
                NextInPriorityFilter.NextInPriorityFilterParent = this;
            }
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

        public new void SetAllowAll(ThingFilter parentFilter, bool includeNonStorable = false)
        {
            base.SetAllowAll(parentFilter, includeNonStorable);
            SyncWithMainFilter();
        }

        public new void SetDisallowAll(IEnumerable<ThingDef> exceptedDefs = null, IEnumerable<SpecialThingFilterDef> exceptedFilters = null)
        {
            base.SetDisallowAll(exceptedDefs, exceptedFilters);
            SyncWithMainFilter();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (OriginalFilter is null)
            {
                Scribe_Values.Look(ref Enabled, "Enabled", true);
                //Scribe_Values.Look(ref StackCountLimit, "StackCountLimit", 0);
                Scribe_Values.Look(ref StackSizeLimit, "StackSizeLimit", 0);
                Scribe_Deep.Look(ref NextInPriorityFilter, false, "NextInPriorityFilter");
                Scribe_Values.Look(ref FilterDepth, "NextInPriorityFilterDepth", 0);
            }
        }
    }
}