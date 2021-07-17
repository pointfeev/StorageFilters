using Verse;
using UnityEngine;
using System;

namespace StorageFilters
{
	public class Dialog_Confirmation : Window
	{
		private Vector2 initialSize = new Vector2(600f, 108f);

		public override Vector2 InitialSize
		{
			get
			{
				return initialSize;
			}
		}

		public Dialog_Confirmation()
		{
			forcePause = false;
			closeOnAccept = false;
			closeOnCancel = false;
			absorbInputAroundWindow = true;
		}

		private string question;
		private Action action;

		public Dialog_Confirmation(string confirmQuestion, Action confirmAction) : this()
		{
			this.question = confirmQuestion;
			this.action = confirmAction;
			Text.Font = GameFont.Small;
			Vector2 size = Text.CalcSize(confirmQuestion);
			initialSize = new Vector2(size.x + 36f, initialSize.y);
			this.SetInitialSizeAndPosition();
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
			string confirmString = question;
			Vector2 confirmStringSize = Text.CalcSize(confirmString);
			float confirmStringY = confirmStringSize.y;
			Widgets.Label(new Rect(0f, 0f, winRect.width, confirmStringY), confirmString);
			float yesNoY = confirmStringY + 12f;
			string yesString = "Yes".Translate();
			float yesStringX = Text.CalcSize(yesString).x;
			string noString = "No".Translate();
			float noStringX = Text.CalcSize(noString).x;
			if (Widgets.ButtonText(new Rect(winRect.width / 2f - yesStringX - 28f, yesNoY, yesStringX + 24f, 35f), yesString) || enter)
			{
				this.action();
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
