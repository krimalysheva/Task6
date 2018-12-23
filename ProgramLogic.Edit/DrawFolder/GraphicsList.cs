using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace ProgramLogic.Edit
{

	[Serializable]
	public class GraphicsList: IDisposable
	{
		private ArrayList graphicsList;

		private bool _isDirty;

		public bool Dirty
		{
			get
			{
				if (_isDirty == false)
				{
					foreach (DrawObject o in graphicsList)
					{
						if (o.Dirty)
						{
							_isDirty = true;
							break;
						}
					}
				}
				return _isDirty;
			}
			set
			{
				foreach (DrawObject o in graphicsList)
					o.Dirty = false;
				_isDirty = false;
			}
		}

		public IEnumerable<DrawObject> Selection
		{
			get
			{
				foreach (DrawObject o in graphicsList)
				{
					if (o.Selected)
					{
						yield return o;
					}
				}
			}
		}

		private const string entryCount = "ObjectCount";
		private const string entryType = "ObjectType";

		public GraphicsList()
		{
			graphicsList = new ArrayList();
		}
 
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);       
		}

		bool _disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				if (disposing)
				{
					if (this.graphicsList != null)
					{
						for (int i = 0; i < this.graphicsList.Count; i++)
						{
							if (this.graphicsList[i] != null)
							{
								((DrawObject) this.graphicsList[i]).Dispose();
							}
						}
					}
				}
				this._disposed = true;
			}
		}

		/// <summary>
		/// Load the GraphicsList from data pulled from disk
		/// </summary>
		/// <param name="info">Data from disk</param>
		/// <param name="orderNumber">Layer number to be loaded</param>
		public void LoadFromStream(SerializationInfo info, int orderNumber)
		{
			graphicsList = new ArrayList();

			// Get number of DrawObjects in this GraphicsList
			int numberObjects = info.GetInt32(
				String.Format(CultureInfo.InvariantCulture,
				              "{0}{1}",
				              entryCount, orderNumber));

			for (int i = 0; i < numberObjects; i++)
			{
				string typeName;
				typeName = info.GetString(
					String.Format(CultureInfo.InvariantCulture,
					              "{0}{1}",
					              entryType, i));

				object drawObject;
				drawObject = Assembly.GetExecutingAssembly().CreateInstance(
					typeName);

				// Let the Draw Object load itself
				((DrawObject)drawObject).LoadFromStream(info, orderNumber, i);

				graphicsList.Add(drawObject);
			}
		}

		/// <summary>
		/// Save GraphicsList to the stream
		/// </summary>
		/// <param name="info">Stream to place the GraphicsList into</param>
		/// <param name="orderNumber">Layer Number the List is on</param>
		public void SaveToStream(SerializationInfo info, int orderNumber)
		{
			// First store the number of DrawObjects in the list
			info.AddValue(
				String.Format(CultureInfo.InvariantCulture,
				              "{0}{1}",
				              entryCount, orderNumber),
				graphicsList.Count);
			// Next save each individual object
			int i = 0;
			foreach (DrawObject o in graphicsList)
			{
				info.AddValue(
					String.Format(CultureInfo.InvariantCulture,
					              "{0}{1}",
					              entryType, i),
					o.GetType().FullName);
				// Let each object save itself
				o.SaveToStream(info, orderNumber, i);
				i++;
			}
		}


		public void Draw(Graphics g)
		{
			int numberObjects = graphicsList.Count;
			for (int i = numberObjects - 1; i >= 0; i--)
			{
				DrawObject o;
				o = (DrawObject)graphicsList[i];
				// рисую только видимые объекты
				if (o.IntersectsWith(Rectangle.Round(g.ClipBounds)))
					o.Draw(g);

                if (o.Selected)
                    o.DrawTracker(g);
            }
		}


		public bool Clear()
		{
			bool result = (graphicsList.Count > 0);
			if (graphicsList.Count > 0)
			{
				for (int i = graphicsList.Count - 1; i >= 0; i--)
				{
					if (graphicsList[i] != null)
					{
						((DrawObject) graphicsList[i]).Dispose();
					}
					graphicsList.RemoveAt(i);
				}
			}
			// Set dirty flag based on result. Result is true only if at least one item was cleared and since the list is empty, there can be nothing dirty.
			if (result)
				_isDirty = false;
			return result;
		}

		public int Count
		{
			get { return graphicsList.Count; }
		}

		public DrawObject this[int index]
		{
			get
			{
				if (index < 0 ||
				    index >= graphicsList.Count)
					return null;

				return (DrawObject)graphicsList[index];
			}
		}

		public int SelectionCount
		{
			get
			{
				int n = 0;

				foreach (DrawObject o in graphicsList)
				{
					if (o.Selected)
						n++;
				}

				return n;
			}
		}

		public DrawObject GetSelectedObject(int index)
		{
			int n = -1;

			foreach (DrawObject o in graphicsList)
			{
				if (o.Selected)
				{
					n++;

					if (n == index)
						return o;
				}
			}

			return null;
		}

		public void Add(DrawObject obj)
		{
			graphicsList.Sort();
			foreach (DrawObject o in graphicsList)
				o.ZOrder++;

			graphicsList.Insert(0, obj);
		}
		public void AddAsInitialGraphic(DrawObject obj)
		{
			graphicsList.Add(obj);
		
		}

        public void Append(DrawObject obj)
        {
            graphicsList.Add(obj);
        }

		public void SelectInRectangle(Rectangle rectangle)
		{
			UnselectAll();

			foreach (DrawObject o in graphicsList)
			{
				if (o.IntersectsWith(rectangle))
					o.Selected = true;
			}
		}

		public void UnselectAll()
		{
			foreach (DrawObject o in graphicsList)
			{
				o.Selected = false;
			}
		}

		public void SelectAll()
		{
			foreach (DrawObject o in graphicsList)
			{
				o.Selected = true;
			}
		}

		//Delete selected items
		public bool DeleteSelection()
		{
			bool result = false;

			int n = graphicsList.Count;

			for (int i = n - 1; i >= 0; i--)
			{
				if (((DrawObject)graphicsList[i]).Selected)
				{
					graphicsList.RemoveAt(i);
					result = true;
				}
			}
			if (result)
				_isDirty = true;
			return result;
		}

		//Delete last added object from the list
		public void DeleteLastAddedObject()
		{
			if (graphicsList.Count > 0)
			{
				graphicsList.RemoveAt(0);
			}
		}

		public void Replace(int index, DrawObject obj)
		{
			if (index >= 0 &&
			    index < graphicsList.Count)
			{
				graphicsList.RemoveAt(index);
				graphicsList.Insert(index, obj);
			}
		}

		public void RemoveAt(int index)
		{
			graphicsList.RemoveAt(index);
		}


		public bool MoveSelectionToFront()
		{
			int n;
			int i;
			ArrayList tempList;

			tempList = new ArrayList();
			n = graphicsList.Count;

            // Читать список источников в обратном порядке, добавлять каждый выбранный элемент
            // во временный список и удалить его из списка источников
            for (i = n - 1; i >= 0; i--)
			{
				if (((DrawObject)graphicsList[i]).Selected)
				{
					tempList.Add(graphicsList[i]);
					graphicsList.RemoveAt(i);
				}
			}

            // Читать временный список в прямом порядке и вставлять каждый элемент
            // в начало списка источников
            n = tempList.Count;

			for (i = 0; i < n; i++)
			{
				graphicsList.Insert(0, tempList[i]);
			}
			if (n > 0)
				_isDirty = true;
			return (n > 0);
		}

        //Переместить выбранные элементы в конец списка
        public bool MoveSelectionToBack()
		{
			int n;
			int i;
			ArrayList tempList;

			tempList = new ArrayList();
			n = graphicsList.Count;

            // Читать список источников в обратном порядке, добавлять каждый выбранный элемент
            // во временный список и удалить его из списка источников
            for (i = n - 1; i >= 0; i--)
			{
				if (((DrawObject)graphicsList[i]).Selected)
				{
					tempList.Add(graphicsList[i]);
					graphicsList.RemoveAt(i);
				}
			}

            // Читать временный список в обратном порядке и добавлять каждый элемент
            // до конца списка источников
            n = tempList.Count;

			for (i = n - 1; i >= 0; i--)
			{
				graphicsList.Add(tempList[i]);
			}
			if (n > 0)
				_isDirty = true;
			return (n > 0);
		}
	}
}