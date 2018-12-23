using System.Drawing;
using System.Windows.Forms;

namespace ProgramLogic.Edit
{

	internal class ToolPointer : Tool
	{
		private enum SelectionMode
		{
			None,
			NetSelection, // group selection is active
			Move, // objects are moves
			Size // object is resized
		}

		private SelectionMode selectMode = SelectionMode.None;

		//объекты уже обрезанные
		private DrawObject resizedObject;
		private int resizedObjectHandle;

        //сохранять состояние последней и текущей точки (используется для перемещения и изменения размера объектов)
        private Point lastPoint = new Point(0, 0);
		private Point startPoint = new Point(0, 0);
		private CommandChangeState commandChangeState;
		private bool wasMove;
		private ToolTip toolTip = new ToolTip();

		//левая кнопка мышки
		public override void OnMouseDown(DrawArea drawArea, MouseEventArgs e)
		{
			commandChangeState = null;
			wasMove = false;

			selectMode = SelectionMode.None;
			Point point = drawArea.BackTrackMouse(new Point(e.X, e.Y));

			int al = drawArea.TheLayers.ActiveLayerIndex;
			int n = drawArea.TheLayers[al].Graphics.SelectionCount;

			for (int i = 0; i < n; i++)
			{
				DrawObject o = drawArea.TheLayers[al].Graphics.GetSelectedObject(i);
				int handleNumber = o.HitTest(point);

				if (handleNumber > 0)
				{
					selectMode = SelectionMode.Size;
					// keep resized object in class members
					resizedObject = o;
					resizedObjectHandle = handleNumber;
					// Since we want to resize only one object, unselect all other objects
					drawArea.TheLayers[al].Graphics.UnselectAll();
					o.Selected = true;
					commandChangeState = new CommandChangeState(drawArea.TheLayers);
					break;
				}
			}

			// Test for move (cursor is on the object)
			if (selectMode == SelectionMode.None)
			{
				int n1 = drawArea.TheLayers[al].Graphics.Count;
				DrawObject o = null;

				for (int i = 0; i < n1; i++)
				{
					if (drawArea.TheLayers[al].Graphics[i].HitTest(point) == 0)
					{
						o = drawArea.TheLayers[al].Graphics[i];
						break;
					}
				}

				if (o != null)
				{
					selectMode = SelectionMode.Move;
					if ((Control.ModifierKeys & Keys.Control) == 0 &&
						!o.Selected)
						drawArea.TheLayers[al].Graphics.UnselectAll();

					// Select clicked object
					o.Selected = true;
					commandChangeState = new CommandChangeState(drawArea.TheLayers);

					drawArea.Cursor = Cursors.SizeAll;
				}
			}

			// Net selection
			if (selectMode == SelectionMode.None)
			{
				// click on background
				if ((Control.ModifierKeys & Keys.Control) == 0)
					drawArea.TheLayers[al].Graphics.UnselectAll();

				selectMode = SelectionMode.NetSelection;
				drawArea.DrawNetRectangle = true;
			}

			lastPoint.X = point.X;
			lastPoint.Y = point.Y;
			startPoint.X = point.X;
			startPoint.Y = point.Y;

			drawArea.Capture = true;
			drawArea.Refresh();
		}

		//Mouse is moved.
		public override void OnMouseMove(DrawArea drawArea, MouseEventArgs e)
		{
			Point point = drawArea.BackTrackMouse(new Point(e.X, e.Y));
			int al = drawArea.TheLayers.ActiveLayerIndex;
			wasMove = true;

			if (e.Button ==
				MouseButtons.None)
			{
				Cursor cursor = null;

				if (drawArea.TheLayers[al].Graphics != null)
				{
					for (int i = 0; i < drawArea.TheLayers[al].Graphics.Count; i++)
					{
						int n = drawArea.TheLayers[al].Graphics[i].HitTest(point);
						if (n > 0)
						{
							cursor = drawArea.TheLayers[al].Graphics[i].GetHandleCursor(n);
							break;
						}
					}
				}

				if (cursor == null)
					cursor = Cursors.Default;

				drawArea.Cursor = cursor;
				return;
			}

			if (e.Button !=
				MouseButtons.Left)
				return;

			// Left button is pressed
			// Find difference between previous and current position
			int dx = point.X - lastPoint.X;
			int dy = point.Y - lastPoint.Y;

			lastPoint.X = point.X;
			lastPoint.Y = point.Y;

			// resize
			if (selectMode == SelectionMode.Size)
			{
				if (resizedObject != null)
				{
					resizedObject.MoveHandleTo(point, resizedObjectHandle);
					drawArea.Refresh();
				}
			}

			// move
			if (selectMode == SelectionMode.Move)
			{
				int n = drawArea.TheLayers[al].Graphics.SelectionCount;

				for (int i = 0; i < n; i++)
				{
					drawArea.TheLayers[al].Graphics.GetSelectedObject(i).Move(dx, dy);
				}

				drawArea.Cursor = Cursors.SizeAll;
				drawArea.Refresh();
			}

			if (selectMode == SelectionMode.NetSelection)
			{
				drawArea.Refresh();
				return;
			}
		}

		//Right mouse button is released
		public override void OnMouseUp(DrawArea drawArea, MouseEventArgs e)
		{
			int al = drawArea.TheLayers.ActiveLayerIndex;
			if (selectMode == SelectionMode.NetSelection)
			{
				drawArea.TheLayers[al].Graphics.SelectInRectangle(drawArea.NetRectangle);

				selectMode = SelectionMode.None;
				drawArea.DrawNetRectangle = false;
			}

			if (resizedObject != null)
			{
				resizedObject.Normalize();
				resizedObject = null;
			}

			drawArea.Capture = false;
			drawArea.Refresh();

			if (commandChangeState != null && wasMove)
			{
				// Keep state after moving/resizing and add command to history
				commandChangeState.NewState(drawArea.TheLayers);
				drawArea.AddCommandToHistory(commandChangeState);
				commandChangeState = null;
			}
			lastPoint = drawArea.BackTrackMouse(e.Location);
		}

		#region Destruction
		private bool _disposed = false;

		protected override void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				if (disposing)
				{

					if (this.resizedObject != null)
					{
						this.resizedObject.Dispose();
					}
					if (this.commandChangeState != null)
					{
						this.commandChangeState.Dispose();
					}
					if (this.toolTip != null)
					{
						this.toolTip.Dispose();
					}
				}
				
				this._disposed = true;
			}
			base.Dispose(disposing);
		}
		#endregion
	}
}