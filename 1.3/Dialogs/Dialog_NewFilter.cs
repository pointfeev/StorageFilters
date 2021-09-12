using RimWorld;
using UnityEngine;
using Verse;

namespace StorageFilters
{
    internal class Dialog_NewFilter : Window
    {
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(240f, 150f);
            }
        }

        protected override void SetInitialSizeAndPosition()
        {
            windowRect = GenUtils.GetDialogSizeAndPosition(this);
        }

        private ITab_Storage storageTab;
        private IStoreSettingsParent storeSettingsParent;
        public Dialog_NewFilter(ITab_Storage instance, IStoreSettingsParent storeSettingsParent)
        {
            forcePause = true;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = false;
            storageTab = instance;
            this.storeSettingsParent = storeSettingsParent;
        }

        private ExtraThingFilters tabFilters;
        private string curName;
        public Dialog_NewFilter(ITab_Storage instance, IStoreSettingsParent storeSettingsParent, ExtraThingFilters tabFilters) : this(instance, storeSettingsParent)
        {
            this.tabFilters = tabFilters;
            curName = "Filter " + (tabFilters.Count + 1);
        }

        private void CheckCurName()
        {
            if (NamePlayerFactionDialogUtility.IsValidName(curName) && Text.CalcSize(curName).x <= StorageFiltersData.MaxFilterStringWidth)
            {
                if (StorageFiltersData.MainFilterString.TryGetValue(storeSettingsParent) != curName && !tabFilters.ContainsKey(curName))
                {
                    tabFilters.Add(curName, new ExtraThingFilter());
                    StorageFiltersData.CurrentFilterKey.SetOrAdd(storeSettingsParent, curName);
                    //Messages.Message("Added new filter named '" + curName + "' to the specified storage area", MessageTypeDefOf.TaskCompletion, false);
                    Find.WindowStack.TryRemove(this, true);
                }
                else
                {
                    Messages.Message("A filter named '" + curName + "' already exists for the specified storage area", MessageTypeDefOf.RejectInput, false);
                }
            }
            else
            {
                Messages.Message("Invalid string", MessageTypeDefOf.RejectInput, false);
            }
        }

        public override void DoWindowContents(Rect winRect)
        {
            if (!GenUtils.IsStorageTabOpen(storageTab, storeSettingsParent))
            {
                Find.WindowStack.TryRemove(this, false);
                return;
            }
            bool esc = false;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                esc = true;
                Event.current.Use();
            }
            bool enter = false;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                enter = true;
                Event.current.Use();
            }
            Text.Font = GameFont.Small;
            string newString = "New filter";
            float newStringY = Text.CalcSize(newString).y;
            Widgets.Label(new Rect(0f, 0f, winRect.width, newStringY), newString);
            float nameY = newStringY + 8f;
            curName = Widgets.TextField(new Rect(0f, nameY, winRect.width, 35f), curName);
            if (Text.CalcSize(curName).x > StorageFiltersData.MaxFilterStringWidth)
            {
                curName = curName.Substring(0, curName.Length - 1);
            }
            float cancelOkY = nameY + 35f + 12f;
            if (Widgets.ButtonText(new Rect(0f, cancelOkY, winRect.width / 2f - 4f, 35f), "CancelButton".Translate()) || esc)
            {
                Find.WindowStack.TryRemove(this, true);
                Event.current.Use();
            }
            if (Widgets.ButtonText(new Rect(winRect.width / 2f + 4f, cancelOkY, winRect.width / 2f - 4f, 35f), "OK".Translate()) || enter)
            {
                CheckCurName();
                Event.current.Use();
            }
        }
    }
}
