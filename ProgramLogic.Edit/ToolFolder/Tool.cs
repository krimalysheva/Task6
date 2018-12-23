using System;
using System.Windows.Forms;

namespace ProgramLogic.Edit
{

	internal abstract class Tool:IDisposable
	{
		public virtual void OnMouseDown(DrawArea drawArea, MouseEventArgs e)
		{
		}

		public virtual void OnMouseMove(DrawArea drawArea, MouseEventArgs e)
		{
		}

		public virtual void OnMouseUp(DrawArea drawArea, MouseEventArgs e)
		{
		}

		#region Destruction
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);       
		}

		private bool _disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				if (disposing)
				{
				}
				
				this._disposed = true;
			}
		}
		#endregion
	}
}