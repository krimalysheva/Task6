using System;

namespace ProgramLogic.Edit
{
	/// <summary>
	/// ����������� ����� ��� ������
	/// </summary>
	public abstract class Command:IDisposable
	{
		// �������� "��������"
		//public abstract void Undo(GraphicsList list);
		public abstract void Undo(Layers list);
        //�������� "�������"
		//public abstract void Redo(GraphicsList list);
		public abstract void Redo(Layers list);

		public abstract void Dispose(bool disposing);

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);       
		}
	}
}