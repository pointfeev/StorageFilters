using RimWorld;

using UnityEngine;

using Verse;

namespace StorageFilters
{
    internal class Dialog_RenameSavedFilter : Window
    {
        public override Vector2 InitialSize => new Vector2(260f, 150f);

        private readonly Dialog_EditFilter editFilterDialog;

        protected override void SetInitialSizeAndPosition() => windowRect = GenUtils.GetDialogSizeAndPosition(this, editFilterDialog);

        public Dialog_RenameSavedFilter(Dialog_EditFilter editDialog)
        {
            forcePause = false;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;
            focusWhenOpened = true;
            forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            editFilterDialog = editDialog;
        }

        private readonly string key;
        private readonly ExtraThingFilter value;
        private string curName;

        public Dialog_RenameSavedFilter(Dialog_EditFilter editDialog, string key, ExtraThingFilter value) : this(editDialog)
        {
            this.key = key;
            this.value = value;
            curName = key;
        }

        private void CheckCurName()
        {
            if (NamePlayerFactionDialogUtility.IsValidName(curName) && Text.CalcSize(curName).x <= StorageFiltersData.MaxFilterStringWidth)
            {
                if (key == curName || !StorageFiltersData.SavedFilter.ContainsKey(curName))
                {
                    if (key != curName)
                    {
                        _ = StorageFiltersData.SavedFilterNoLoad.Remove(key);
                        StorageFiltersData.SavedFilterNoLoad.Add(curName, value);
                        SaveUtils.Save();
                    }
                    Messages.Message("ASF_RenamedSavedFilter".Translate(key, curName), MessageTypeDefOf.TaskCompletion, false);
                    _ = Find.WindowStack.TryRemove(this, true);
                }
                else
                {
                    Messages.Message("ASF_SavedFilterExists".Translate(curName), MessageTypeDefOf.RejectInput, false);
                }
            }
            else
            {
                Messages.Message("ASF_InvalidString".Translate(), MessageTypeDefOf.RejectInput, false);
            }
        }

        public override void DoWindowContents(Rect winRect)
        {
            if (editFilterDialog is null || !editFilterDialog.IsOpen)
            {
                _ = Find.WindowStack.TryRemove(this, false);
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
            string renameString = "ASF_RenamingSavedFilter".Translate(key);
            float renameStringY = Text.CalcSize(renameString).y;
            Widgets.Label(new Rect(0f, 0f, winRect.width, renameStringY), renameString);
            float nameY = renameStringY + 8f;
            curName = Widgets.TextField(new Rect(0f, nameY, winRect.width, 35f), curName);
            if (Text.CalcSize(curName).x > StorageFiltersData.MaxFilterStringWidth)
            {
                curName = curName.Substring(0, curName.Length - 1);
            }
            float cancelRenameY = nameY + 35f + 12f;
            if (Widgets.ButtonText(new Rect(0f, cancelRenameY, winRect.width / 2f - 4f, 35f), "ASF_Cancel".Translate()) || esc)
            {
                _ = Find.WindowStack.TryRemove(this, true);
                Event.current.Use();
            }
            if (Widgets.ButtonText(new Rect(winRect.width / 2f + 4f, cancelRenameY, winRect.width / 2f - 4f, 35f), "ASF_RenameFilter".Translate()) || enter)
            {
                CheckCurName();
                Event.current.Use();
            }
        }
    }
}