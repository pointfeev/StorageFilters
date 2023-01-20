using RimWorld;
using StorageFilters.Utilities;
using UnityEngine;
using Verse;

namespace StorageFilters.Dialogs;

internal class Dialog_NewFilter : Window
{
    private readonly ITab_Storage storageTab;
    private readonly IStoreSettingsParent storeSettingsParent;

    private readonly ExtraThingFilters tabFilters;
    private string curName;

    public Dialog_NewFilter(ITab_Storage instance, IStoreSettingsParent storeSettingsParent)
    {
        layer = WindowLayer.GameUI;
        preventCameraMotion = false;
        soundAppear = SoundDefOf.TabOpen;
        soundClose = SoundDefOf.TabClose;
        forcePause = false;
        closeOnAccept = false;
        closeOnCancel = false;
        absorbInputAroundWindow = false;
        storageTab = instance;
        this.storeSettingsParent = storeSettingsParent;
    }

    public Dialog_NewFilter(ITab_Storage instance, IStoreSettingsParent storeSettingsParent, ExtraThingFilters tabFilters) : this(instance, storeSettingsParent)
    {
        this.tabFilters = tabFilters;
        curName = "ASF_DefaultName".Translate(tabFilters.Count + 1);
    }

    public override Vector2 InitialSize => new(240f, 150f);

    protected override void SetInitialSizeAndPosition() => windowRect = GenUtils.GetDialogSizeAndPosition(this);

    private void CheckCurName()
    {
        if (NamePlayerFactionDialogUtility.IsValidName(curName))
        {
            if (StorageFiltersData.GetMainFilterName(storeSettingsParent) != curName && !tabFilters.ContainsKey(curName))
            {
                tabFilters.Add(curName, new());
                StorageFiltersData.SetCurrentFilterKey(storeSettingsParent, curName);
                //Messages.Message("Added new filter named '" + curName + "' to the specified storage area", MessageTypeDefOf.TaskCompletion, false);
                _ = Find.WindowStack.TryRemove(this);
            }
            else
                Messages.Message("ASF_StorageAreaAlreadyHasFilter".Translate(curName), MessageTypeDefOf.RejectInput, false);
        }
        else
            Messages.Message("ASF_InvalidString".Translate(), MessageTypeDefOf.RejectInput, false);
    }

    public override void DoWindowContents(Rect winRect)
    {
        if (!GenUtils.IsStorageTabOpen(storageTab, storeSettingsParent))
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
        string newString = "ASF_NewFilter".Translate();
        float newStringY = Text.CalcSize(newString).y;
        Widgets.Label(new(0f, 0f, winRect.width, newStringY), newString);
        float nameY = newStringY + 8f;
        curName = Widgets.TextField(new(0f, nameY, winRect.width, 35f), curName);
        float cancelOkY = nameY + 35f + 12f;
        if (Widgets.ButtonText(new(0f, cancelOkY, winRect.width / 2f - 4f, 35f), "ASF_Cancel".Translate()) || esc)
        {
            _ = Find.WindowStack.TryRemove(this);
            Event.current.Use();
        }
        if (Widgets.ButtonText(new(winRect.width / 2f + 4f, cancelOkY, winRect.width / 2f - 4f, 35f), "ASF_Accept".Translate()) || enter)
        {
            CheckCurName();
            Event.current.Use();
        }
    }
}