using System.Windows.Forms;

namespace ProgramLogic.Edit
{
	internal abstract class ToolObject : Tool
	{
		private Cursor cursor;

		protected Cursor Cursor
		{
			get { return cursor; }
			set { cursor = value; }
		}

		/// Left mouse is released.
		/// New object is created and resized.
		public override void OnMouseUp(DrawArea drawArea, MouseEventArgs e)
		{
			int al = drawArea.TheLayers.ActiveLayerIndex;
			if (drawArea.TheLayers[al].Graphics.Count > 0)
				drawArea.TheLayers[al].Graphics[0].Normalize();

			drawArea.Capture = false;
			drawArea.Refresh();
		}

        /// Добавить новый объект в область рисования.
        /// Функция вызывается, когда пользователь щелкает левой кнопкой мыши область рисования,
        /// и один из инструментов, производных от ToolObject, активен.
        protected void AddNewObject(DrawArea drawArea, DrawObject o)
		{
			int al = drawArea.TheLayers.ActiveLayerIndex;
			drawArea.TheLayers[al].Graphics.UnselectAll();

			o.Selected = true;
			o.Dirty = true;
			int objectID = 0;
			for (int i = 0; i < drawArea.TheLayers.Count; i++)
			{
				objectID = +drawArea.TheLayers[i].Graphics.Count;
			}
			objectID++;
			o.ID = objectID;
			drawArea.TheLayers[al].Graphics.Add(o);

			drawArea.Capture = true;
			drawArea.Refresh();
		}

		#region Destruction
		private bool _disposed = false;

		protected override void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				if (disposing)
				{
					if (this.cursor != null)
					{
						this.cursor.Dispose();
					}
				}
				
				this._disposed = true;
			}
			base.Dispose(disposing);
		}

		#endregion
	}
}