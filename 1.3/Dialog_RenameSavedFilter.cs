using RimWorld;
using Verse;
using UnityEngine;

namespace StorageFilters
{
	internal class Dialog_RenameSavedFilter : Window
	{
		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(260f, 150f);
			}
		}

		public Dialog_RenameSavedFilter()
		{
			forcePause = false;
			closeOnAccept = false;
			closeOnCancel = false;
			absorbInputAroundWindow = true;
		}

		private string key;
		private ExtraThingFilter value;
		private string curName;

		public Dialog_RenameSavedFilter(string key, ExtraThingFilter value) : this()
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
						StorageFiltersData.SavedFilterNoLoad.Remove(key);
						StorageFiltersData.SavedFilterNoLoad.Add(curName, value);
						SaveUtils.Save();
					}
					Find.WindowStack.TryRemove(this, true);
				}
				else
				{
					Messages.Message("A saved filter named '" + curName + "' already exists", MessageTypeDefOf.RejectInput, false);
				}
			}
			else
			{
				Messages.Message("Invalid string", MessageTypeDefOf.RejectInput, false);
			}
		}

		public override void DoWindowContents(Rect winRect)
		{
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
			string renameString = "Renaming saved filter: '" + key + "'";
			float renameStringY = Text.CalcSize(renameString).y;
			Widgets.Label(new Rect(0f, 0f, winRect.width, renameStringY), renameString);
			float nameY = renameStringY + 8f;
			curName = Widgets.TextField(new Rect(0f, nameY, winRect.width, 35f), curName);
			if (Text.CalcSize(curName).x > StorageFiltersData.MaxFilterStringWidth)
			{
				curName = curName.Substring(0, curName.Length - 1);
			}
			float cancelRenameY = nameY + 35f + 12f;
			if (Widgets.ButtonText(new Rect(0f, cancelRenameY, winRect.width / 2f - 4f, 35f), "CancelButton".Translate()) || esc)
			{
				Find.WindowStack.TryRemove(this, true);
				Event.current.Use();
			}
			if (Widgets.ButtonText(new Rect(winRect.width / 2f + 4f, cancelRenameY, winRect.width / 2f - 4f, 35f), "Rename".Translate()) || enter)
			{
				CheckCurName();
				Event.current.Use();
			}
		}
	}
}
