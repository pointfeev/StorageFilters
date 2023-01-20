using RimWorld;
using StorageFilters.Utilities;
using UnityEngine;
using Verse;

namespace StorageFilters.Dialogs;

internal class Dialog_RenameSavedFilter : Window
{
    private readonly Dialog_EditFilter editFilterDialog;

    private readonly string key;
    private readonly ExtraThingFilter value;
    private string curName;

    public Dialog_RenameSavedFilter(Dialog_EditFilter editDialog)
    {
        layer = WindowLayer.GameUI;
        preventCameraMotion = false;
        soundAppear = SoundDefOf.TabOpen;
        soundClose = SoundDefOf.TabClose;
        forcePause = false;
        closeOnAccept = false;
        closeOnCancel = false;
        absorbInputAroundWindow = true;
        focusWhenOpened = true;
        forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
        editFilterDialog = editDialog;
    }

    public Dialog_RenameSavedFilter(Dialog_EditFilter editDialog, string key, ExtraThingFilter value) : this(editDialog)
    {
        this.key = key;
        this.value = value;
        curName = key;
    }

    public override Vector2 InitialSize => new(260f, 150f);

    protected override void SetInitialSizeAndPosition() => windowRect = GenUtils.GetDialogSizeAndPosition(this, editFilterDialog);

    private void CheckCurName()
    {
        if (NamePlayerFactionDialogUtility.IsValidName(curName))
        {
            if (key == curName || !StorageFiltersData.SavedFilters.ContainsKey(curName))
            {
                if (key != curName)
                {
                    _ = StorageFiltersData.SavedFilters.Remove(key);
                    StorageFiltersData.SavedFilters.Add(curName, value);
                    SaveUtils.Save();
                }
                Messages.Message("ASF_RenamedSavedFilter".Translate(key, curName), MessageTypeDefOf.TaskCompletion, false);
                _ = Find.WindowStack.TryRemove(this);
            }
            else
                Messages.Message("ASF_SavedFilterExists".Translate(curName), MessageTypeDefOf.RejectInput, false);
        }
        else
            Messages.Message("ASF_InvalidString".Translate(), MessageTypeDefOf.RejectInput, false);
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
        Widgets.Label(new(0f, 0f, winRect.width, renameStringY), renameString);
        float nameY = renameStringY + 8f;
        curName = Widgets.TextField(new(0f, nameY, winRect.width, 35f), curName);
        float cancelRenameY = nameY + 35f + 12f;
        if (Widgets.ButtonText(new(0f, cancelRenameY, winRect.width / 2f - 4f, 35f), "ASF_Cancel".Translate()) || esc)
        {
            _ = Find.WindowStack.TryRemove(this);
            Event.current.Use();
        }
        if (Widgets.ButtonText(new(winRect.width / 2f + 4f, cancelRenameY, winRect.width / 2f - 4f, 35f), "ASF_RenameFilter".Translate()) || enter)
        {
            CheckCurName();
            Event.current.Use();
        }
    }
}