using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace StorageFilters
{
    internal class Dialog_Confirmation : Window
    {
        private Vector2 initialSize = new Vector2(600f, 108f);
        public override Vector2 InitialSize => initialSize;

        private readonly Dialog_EditFilter editFilterDialog;
        protected override void SetInitialSizeAndPosition()
        {
            windowRect = GenUtils.GetDialogSizeAndPosition(this, editFilterDialog);
        }

        private readonly ITab_Storage storageTab;
        private readonly IStoreSettingsParent storeSettingsParent;
        public Dialog_Confirmation(ITab_Storage instance, IStoreSettingsParent storeSettingsParent, Dialog_EditFilter editDialog = null)
        {
            forcePause = true;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;
            focusWhenOpened = true;
            storageTab = instance;
            this.storeSettingsParent = storeSettingsParent;
            editFilterDialog = editDialog;
        }

        private readonly string question;
        private readonly Action action;
        public Dialog_Confirmation(ITab_Storage instance, IStoreSettingsParent storeSettingsParent, string confirmQuestion, Action confirmAction, Dialog_EditFilter editDialog = null) : this(instance, storeSettingsParent, editDialog)
        {
            question = confirmQuestion;
            action = confirmAction;
            Text.Font = GameFont.Small;
            Vector2 size = Text.CalcSize(confirmQuestion);
            initialSize = new Vector2(size.x + 36f, initialSize.y);
            SetInitialSizeAndPosition();
        }

        private readonly string questionExtra;
        public Dialog_Confirmation(ITab_Storage instance, IStoreSettingsParent storeSettingsParent, string confirmQuestion, string confirmQuestionExtra, Action confirmAction, Dialog_EditFilter editDialog = null) : this(instance, storeSettingsParent, editDialog)
        {
            question = confirmQuestion;
            questionExtra = confirmQuestionExtra;
            action = confirmAction;
            Text.Font = GameFont.Small;
            Vector2 size1 = Text.CalcSize(confirmQuestion);
            Vector2 size2 = Text.CalcSize(confirmQuestionExtra);
            if (confirmQuestionExtra is null || confirmQuestionExtra.Length == 0)
            {
                size2 = Vector2.zero;
            }

            initialSize = new Vector2(Math.Max(size1.x, size2.x) + 36f, initialSize.y + size2.y + 2f);
            SetInitialSizeAndPosition();
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
            string confirmString = question;
            Vector2 confirmStringSize = Text.CalcSize(confirmString);
            float confirmStringY = confirmStringSize.y;
            Widgets.Label(new Rect(0f, 0f, winRect.width, confirmStringY), confirmString);
            float yesNoY = confirmStringY + 12f;
            if (!(questionExtra is null) && questionExtra.Length > 0)
            {
                string confirmExtraString = questionExtra;
                Vector2 confirmExtraStringSize = Text.CalcSize(confirmExtraString);
                float confirmExtraStringY = confirmExtraStringSize.y;
                float X = 0f;
                if (confirmExtraStringSize.x < confirmStringSize.x)
                {
                    X = (winRect.width / 2) - (confirmExtraStringSize.x / 2);
                }
                Widgets.Label(new Rect(X, yesNoY - 6f, confirmExtraStringSize.x, confirmExtraStringY), confirmExtraString);
                yesNoY += confirmExtraStringY + 6f;
            }
            string yesString = "Yes".Translate();
            float yesStringX = Text.CalcSize(yesString).x;
            string noString = "No".Translate();
            float noStringX = Text.CalcSize(noString).x;
            if (Widgets.ButtonText(new Rect(winRect.width / 2f - yesStringX - 28f, yesNoY, yesStringX + 24f, 35f), yesString) || enter)
            {
                action();
                Find.WindowStack.TryRemove(this, true);
                Event.current.Use();
            }
            if (Widgets.ButtonText(new Rect(winRect.width / 2f + 4f, yesNoY, noStringX + 24f, 35f), noString) || esc)
            {
                Find.WindowStack.TryRemove(this, true);
                Event.current.Use();
            }
        }
    }
}
