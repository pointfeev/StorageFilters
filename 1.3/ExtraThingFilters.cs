using System.Collections.Generic;
using Verse;

namespace StorageFilters
{
    public class ExtraThingFilters : IExposable
    {
        private List<string> filterKeys = new List<string>();
        private Dictionary<string, ExtraThingFilter> filters;
        private List<ExtraThingFilter> filterValues = new List<ExtraThingFilter>();

        public ExtraThingFilters() => filters = new Dictionary<string, ExtraThingFilter>();

        public Dictionary<string, ExtraThingFilter>.KeyCollection Keys => filters.Keys;

        public Dictionary<string, ExtraThingFilter>.ValueCollection Values => filters.Values;

        public int Count => filters.Count;

        public void ExposeData() => Scribe_Collections.Look(ref filters, "filters", LookMode.Value, LookMode.Deep,
                                                            ref filterKeys, ref filterValues);

        public Dictionary<string, ExtraThingFilter>.Enumerator GetEnumerator() => filters.GetEnumerator();

        public ExtraThingFilter Get(string key) => filters.TryGetValue(key);

        public void Set(string key, ExtraThingFilter value) => filters.SetOrAdd(key, value);

        public void Add(string key, ExtraThingFilter value) => filters.Add(key, value);

        public void Remove(string key) => filters.Remove(key);

        public bool ContainsKey(string key) => filters.ContainsKey(key);
    }
}