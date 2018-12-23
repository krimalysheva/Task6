using System.Collections.Generic;

namespace ProgramLogic.Edit
{
	/// <summary>
	/// ������� ��� � ������ ��������
    /// ������� �����, ������� �������� ��������� :(
	/// </summary>
	internal class CommandDeleteAll : Command
	{
		private List<DrawObject> cloneList;

		public CommandDeleteAll(Layers list)
		{
			cloneList = new List<DrawObject>();

            //������� ���� ����� ������.
            // ��������� ������� � �������� �������!1111!1!!
            int n = list[list.ActiveLayerIndex].Graphics.Count;

			for (int i = n - 1; i >= 0; i--)
			{
				cloneList.Add(list[list.ActiveLayerIndex].Graphics[i].Clone());
			}
		}

		public override void Undo(Layers list)
		{
			// ������� ��� ���������
			foreach (DrawObject o in cloneList)
			{
				list[list.ActiveLayerIndex].Graphics.Add(o);
			}
		}

		public override void Redo(Layers list)
		{
			//�������� ���� - ���������
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