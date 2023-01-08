using System.Collections.Generic;
using Verse;

namespace StorageFilters
{
    public sealed class ExtraThingFilter : ThingFilter
    {
        private readonly ThingFilter originalFilter;
        public bool Enabled = true;
        public int FilterDepth;
        public ExtraThingFilter NextInPriorityFilter;

        private ExtraThingFilter nextInPriorityFilterParent;

        //public int StackCountLimit;
        public int StackSizeLimit;

        public ExtraThingFilter() { }

        public ExtraThingFilter(ExtraThingFilter parent = null, int filterDepth = 0) : this()
        {
            nextInPriorityFilterParent = parent;
            FilterDepth = filterDepth;
        }

        public ExtraThingFilter(ThingFilter originalFilter) : this()
        {
            CopyAllowancesFrom(originalFilter);
            this.originalFilter = originalFilter;
        }

        public ExtraThingFilter NextInPriorityFilterParent
        {
            get
            {
                if (!(nextInPriorityFilterParent is null))
                    return nextInPriorityFilterParent;
                foreach (ExtraThingFilter filter in StorageFiltersData.AllFilters())
                {
                    ExtraThingFilter currentFilter = filter;
                    while (nextInPriorityFilterParent is null && !(currentFilter.NextInPriorityFilter is null))
                        if (currentFilter.NextInPriorityFilter == this)
                        {
                            nextInPriorityFilterParent = currentFilter;
                            return currentFilter;
                        }
                        else
                            currentFilter = currentFilter.NextInPriorityFilter;
                }
                return nextInPriorityFilterParent;
            }
            private set => nextInPriorityFilterParent = value;
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
            originalFilter?.CopyAllowancesFrom(this);
        }

        public ExtraThingFilter Copy()
        {
            ExtraThingFilter copy = new ExtraThingFilter();
            copy.CopyFrom(this);
            return copy;
        }

        private void SyncWithMainFilter() => originalFilter?.CopyAllowancesFrom(this);

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
            if (!(originalFilter is null))
                return;
            Scribe_Values.Look(ref Enabled, "Enabled", true);
            //Scribe_Values.Look(ref StackCountLimit, "StackCountLimit");
            Scribe_Values.Look(ref StackSizeLimit, "StackSizeLimit");
            Scribe_Deep.Look(ref NextInPriorityFilter, false, "NextInPriorityFilter");
            Scribe_Values.Look(ref FilterDepth, "NextInPriorityFilterDepth");
        }
    }
}