using System.Collections.Generic;

namespace ProgramLogic.Edit
{
	/// <summary>
	///  �������� ��������������������� ��������: 
	///  �����������, ��������, �������� ���-��
	/// </summary>
	internal class CommandChangeState : Command
	{
		// ��������� ������� �� ���������� ��������
		private List<DrawObject> listBefore;

		// ��������� ������� ����� ���������� ��������
		private List<DrawObject> listAfter;

		//��������� ���� �� ������� ���������� ���������
		private int activeLayer;

		//������� ��� ������� �� �������� 
		public CommandChangeState(Layers layerList)
		{
			//������� ��������� ������� �� ��������
			activeLayer = layerList.ActiveLayerIndex;
			FillList(layerList[activeLayer].Graphics, ref listBefore);
		}

		//������� ������� ����� ���������� ��������
		public void NewState(Layers layerList)
		{
			// ������� ��������� �������� ����� ��������.
			FillList(layerList[activeLayer].Graphics, ref listAfter);
		}

		public override void Undo(Layers list)
		{
			//������� ���� �������� � ����� � ��������� �� ����.���������
			ReplaceObjects(list[activeLayer].Graphics, listBefore);
		}

		public override void Redo(Layers list)
		{
            // ������� ���� �������� � ����� � ��������� �� �������� �����
            ReplaceObjects(list[activeLayer].Graphics, listAfter);
		}

		//������� �������� � ��������� � ���������� �����
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

        // ��������� ����
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