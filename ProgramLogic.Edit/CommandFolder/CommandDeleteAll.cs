using System.Collections.Generic;

namespace ProgramLogic.Edit
{
	/// <summary>
	/// удалить все к черт€м собачьим
    /// человек устал, пустите человека отдохнуть :(
	/// </summary>
	internal class CommandDeleteAll : Command
	{
		private List<DrawObject> cloneList;

		public CommandDeleteAll(Layers list)
		{
			cloneList = new List<DrawObject>();

            //сделать клон всего списка.
            // добавл€ть объекты в обратном пор€дке!1111!1!!
            int n = list[list.ActiveLayerIndex].Graphics.Count;

			for (int i = n - 1; i >= 0; i--)
			{
				cloneList.Add(list[list.ActiveLayerIndex].Graphics[i].Clone());
			}
		}

		public override void Undo(Layers list)
		{
			// вернуть все удаленное
			foreach (DrawObject o in cloneList)
			{
				list[list.ActiveLayerIndex].Graphics.Add(o);
			}
		}

		public override void Redo(Layers list)
		{
			//очистить лист - полностью
			list[list.ActiveLayerIndex].Graphics.Clear();
		}

		#region Destruction
		bool _disposed = false;

		public override void Dispose(bool disposing)
		{
			if (!this._disposed)
			{

				if (disposing)
				{
					if (this.cloneList != null)
					{
						foreach (DrawObject drawObject in cloneList)
						{
							if (drawObject != null)
							{
								drawObject.Dispose();
							}
						}
					}
				}
				this._disposed = true;
			}
		}
		#endregion
	}
}