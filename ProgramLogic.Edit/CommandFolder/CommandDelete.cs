using System.Collections.Generic;

namespace ProgramLogic.Edit
{
	/// <summary>
	/// ��������
	/// </summary>
	internal class CommandDelete : Command
	{
		private List<DrawObject> cloneList; // �������� ��������� ������� ��� �������

		public CommandDelete(Layers list)
		{
			cloneList = new List<DrawObject>();

			// ��������� ��������� ����

			foreach (DrawObject o in list[list.ActiveLayerIndex].Graphics.Selection)
			{
				cloneList.Add(o.Clone());
			}
		}

		public override void Undo(Layers list)
		{
			list[list.ActiveLayerIndex].Graphics.UnselectAll();

			//�������� ��� ������ � ��������
			foreach (DrawObject o in cloneList)
			{
				list[list.ActiveLayerIndex].Graphics.Add(o);
			}
		}

		public override void Redo(Layers list)
		{
			//������� ��� �������, �������� � ������������� �����

			int n = list[list.ActiveLayerIndex].Graphics.Count;

			for (int i = n - 1; i >= 0; i--)
			{
				bool toDelete = false;
				DrawObject objectToDelete = list[list.ActiveLayerIndex].Graphics[i];

				foreach (DrawObject o in cloneList)
				{
					if (objectToDelete.ID ==
					    o.ID)
					{
						toDelete = true;
						break;
					}
				}

				if (toDelete)
				{
					list[list.ActiveLayerIndex].Graphics.RemoveAt(i);
				}
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