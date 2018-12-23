using System;
using System.Collections.Generic;

namespace ProgramLogic.Edit
{

	internal class UndoManager:IDisposable
	{
		private Layers layers;

		private List<Command> historyList;
		private int nextUndo;

		public UndoManager(Layers layerList)
		{
			layers = layerList;

			ClearHistory();
		}

		#region Destruction
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);       
		}

		private bool _disposed = false;

		protected void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				if (disposing)
				{
					if (this.historyList != null)
					{
						foreach (Command command in this.historyList)
						{
							if (command != null)
							{
								command.Dispose();
							}
						}
					}
					if (this.layers != null)
					{
						for (int i = 0; i < this.layers.Count; i++)
						{
							if (this.layers[i] != null)
							{
								this.layers[i].Dispose();
							}
						}
					}
				}		
				this._disposed = true;
			}
		}
		#endregion


		public bool CanUndo
		{
			get
			{
				if (nextUndo < 0 ||
					nextUndo > historyList.Count - 1) 
				{
					return false;
				}

				return true;
			}
		}

		/// Return true if Redo operation is available
		public bool CanRedo
		{
			get
			{
				// If the NextUndo pointer points to the last item, no commands to redo
				if (nextUndo == historyList.Count - 1)
				{
					return false;
				}

				return true;
			}
		}

		public void ClearHistory()
		{
			if (this.historyList != null)
			{
				foreach (Command command in historyList)
				{
					if (command != null)
					{
						command.Dispose();
					}
				}
			}
			historyList = new List<Command>();
			nextUndo = -1;
		}

		public void AddCommandToHistory(Command command)
		{
			// Purge history list
			TrimHistoryList();

			// Add command and increment undo counter
			historyList.Add(command);

			nextUndo++;
		}

		/// Undo
		public void Undo()
		{
			if (!CanUndo)
			{
				return;
			}

			// Get the Command object to be undone
			Command command = historyList[nextUndo];

			// Execute the Command object's undo method
			command.Undo(layers);

			// Move the pointer up one item
			nextUndo--;
		}

		/// Redo
		public void Redo()
		{
			if (!CanRedo)
			{
				return;
			}

			// Get the Command object to redo
			int itemToRedo = nextUndo + 1;
			Command command = historyList[itemToRedo];

			// Execute the Command object
			command.Redo(layers);

			// Move the undo pointer down one item
			nextUndo++;
		}

		private void TrimHistoryList()
		{
            // ћы можем повторить любую отмененную команду, пока не выполним новую 
            // команда. Ќова€ команда уводит нас в новом направлении,
            // что означает, что мы больше не можем переделывать ранее отмененные действи€. 
            // »так, мы удал€ем все отмененные команды из списка истории.*/

            // Exit if no items in History list
            if (historyList.Count == 0)
			{
				return;
			}

			// Exit if NextUndo points to last item on the list
			if (nextUndo == historyList.Count - 1)
			{
				return;
			}

			// Purge all items below the NextUndo pointer
			for (int i = historyList.Count - 1; i > nextUndo; i--)
			{
				historyList.RemoveAt(i);
			}
		}
	}
}