using Verse;
using RimWorld;
using System.Collections.Generic;

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
                if (nextInPriorityFilterParent != null)
                {
                    return nextInPriorityFilterParent;
                }
                else
                {
                    foreach (KeyValuePair<IStoreSettingsParent, ExtraThingFilters> filters in StorageFiltersData.Filters)
                    {
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
                                {
                                    currentFilter = currentFilter.NextInPriorityFilter;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                    return null;
                }
            }
        }
        public ExtraThingFilter NextInPriorityFilter = null;
        public int NextInPriorityFilterDepth = 0;

        public ExtraThingFilter() : base() { }

        public ExtraThingFilter(ThingFilter filter) : this()
        {
            CopyAllowancesFrom(filter);
        }

        public void CopyFrom(ExtraThingFilter otherFilter)
        {
            Enabled = otherFilter.Enabled;
            NextInPriorityFilterDepth = otherFilter.NextInPriorityFilterDepth;
            if (otherFilter.NextInPriorityFilter != null)
            {
                NextInPriorityFilter = otherFilter.NextInPriorityFilter.Copy();
            }
            CopyAllowancesFrom(otherFilter as ThingFilter);
        }

        public ExtraThingFilter Copy()
        {
            ExtraThingFilter copy = new ExtraThingFilter();
            copy.CopyFrom(this);
            return copy;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref Enabled, "Enabled", true);
            Scribe_Deep.Look(ref NextInPriorityFilter, false, "NextInPriorityFilter");
            Scribe_Values.Look(ref NextInPriorityFilterDepth, "NextInPriorityFilterDepth", 0);
        }
    }
}
