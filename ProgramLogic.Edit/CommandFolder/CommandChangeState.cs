using System.Collections.Generic;

namespace ProgramLogic.Edit
{
	/// <summary>
	///  изменить состояниесуществующих объектов: 
	///  передвинуть, обрезать, изменить хар-ки
	/// </summary>
	internal class CommandChangeState : Command
	{
		// выбранные объекты до проведения операции
		private List<DrawObject> listBefore;

		// выбранные объекты после проведения операции
		private List<DrawObject> listAfter;

		//отследить слой на котором происходит изменение
		private int activeLayer;

		//создать эту команду до операции 
		public CommandChangeState(Layers layerList)
		{
			//хранить состояние объетов до операции
			activeLayer = layerList.ActiveLayerIndex;
			FillList(layerList[activeLayer].Graphics, ref listBefore);
		}

		//вызвать функцию после проведения операции
		public void NewState(Layers layerList)
		{
			// хранить состояние объектов после операции.
			FillList(layerList[activeLayer].Graphics, ref listAfter);
		}

		public override void Undo(Layers list)
		{
			//реплейс всех объектов в листе с объектами из пред.осотояния
			ReplaceObjects(list[activeLayer].Graphics, listBefore);
		}

		public override void Redo(Layers list)
		{
            // реплейс всех объектов в листе с объектами из сотояния после
            ReplaceObjects(list[activeLayer].Graphics, listAfter);
		}

		//реплейс объектов в графлисте с объектамис листа
		private void ReplaceObjects(GraphicsList graphicsList, List<DrawObject> list)
		{
			for (int i = 0; i < graphicsList.Count; i++)
			{
				DrawObject replacement = null;

				foreach (DrawObject o in list)
				{
					if (o.ID ==
					    graphicsList[i].ID)
					{
						replacement = o;
						break;
					}
				}

				if (replacement != null)
				{
					graphicsList.Replace(i, replacement);
				}
			}
		}

        // заполнить лист
        private void FillList(GraphicsList graphicsList, ref List<DrawObject> listToFill)
        {
            listToFill = new List<DrawObject>();

            foreach (DrawObject o in graphicsList.Selection)
            {
                listToFill.Add(o.Clone());
            }
        }

        #region Destruction

        bool _disposed = false;

		public override void Dispose(bool disposing)
		{
			if (!this._disposed)
			{

				if (disposing)
				{
					if (this.listBefore != null)
					{
						foreach (DrawObject drawObject in listBefore)
						{
							if (drawObject != null)
							{
								drawObject.Dispose();
							}
						}
					}

					if (this.listAfter != null)
					{
						foreach (DrawObject drawObject in listAfter)
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