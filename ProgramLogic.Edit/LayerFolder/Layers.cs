using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Windows.Forms;

namespace ProgramLogic.Edit
{
	/// <summary>
	/// Collection of <see cref="Layer"/>s исп. для организации рабочих поверхностей
	/// </summary>
	[Serializable]
	public class Layers : ISerializable, IDisposable
	{
		// Contains the list of Layers
		private ArrayList layerList;

		private bool _isDirty;

		/// <summary>
		/// Dirty is True if any graphic element in any Layer is dirty, else False
		/// </summary>
		public bool Dirty
		{
			get
			{
				if (_isDirty == false)
				{
					foreach (Layer l in layerList)
					{
						if (l.Dirty)
						{
							_isDirty = true;
							break;
						}
					}
				}
				return _isDirty;
			}
		}

		private const string entryCount = "LayerCount";
		private const string entryLayer = "LayerType";

		public Layers()
		{
			layerList = new ArrayList();
		}

		//только один слой мб активен в ед. времени
		public int ActiveLayerIndex
		{
			get
			{
				int i = 0;
				foreach (Layer l in layerList)
				{
					if (l.IsActive)
						break;
					i++;
				}
				return i;
			}
		}

		protected Layers(SerializationInfo info, StreamingContext context)
		{
			layerList = new ArrayList();

			int n = info.GetInt32(entryCount);

			for (int i = 0; i < n; i++)
			{
				string typeName;
				typeName = info.GetString(
					String.Format(CultureInfo.InvariantCulture,
					              "{0}{1}",
					              entryLayer, i));

				object _layer;
				_layer = Assembly.GetExecutingAssembly().CreateInstance(typeName);
				((Layer)_layer).LoadFromStream(info, i);
				layerList.Add(_layer);
			}
		}

		/// <summary>
		/// Save object to serialization stream
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(entryCount, layerList.Count);

			int i = 0;

			foreach (Layer l in layerList)
			{
				info.AddValue(
					String.Format(CultureInfo.InvariantCulture,
					              "{0}{1}",
					              entryLayer, i),
					l.GetType().FullName);

				l.SaveToStream(info, i);
				i++;
			}
		}

		//нарисовать все объекты на всех видимых слоях
		public void Draw(Graphics g)
		{
			foreach (Layer l in layerList)
			{
				if (l.IsVisible)
					l.Draw(g);
			}
		}

		/// Clear all objects in the list
		/// true if at least one object is deleted
		public bool Clear()
		{
			bool result = (layerList.Count > 0);
			foreach (Layer l in layerList)
				l.Graphics.Clear();

			if (layerList.Count > 0)
			{
				for (int i = layerList.Count - 1; i >= 0; i--)
				{
					if (layerList[i] != null)
					{
						((DrawObject) layerList[i]).Dispose();
					}
					layerList.RemoveAt(i);
				}
			}
			//всегда должен быть хоть один слой 
			CreateNewLayer("Default");

            // Установить грязный флаг на основе результата. Результат имеет значение true, 
            //только если хотя бы один элемент был очищен и так как список пуст, не может быть ничего грязного.
            if (result)
				_isDirty = false;
			return result;
		}

		public int Count
		{
			get { return layerList.Count; }
		}

		/// <summary>
		/// Allows iterating through the list of layers using a for loop
		/// </summary>
		/// <param name="index">the index of the layer to return</param>
		/// <returns>the specified layer object</returns>
		public Layer this[int index]
		{
			get
			{
				if (index < 0 ||
				    index >= layerList.Count)
					return null;
				return (Layer)layerList[index];
			}
		}

		public void Add(Layer obj)
		{
		  layerList.Add(obj);
		}

        //создать ноый слой активный и видимый
		public void CreateNewLayer(string theName)
		{
			//деактивировть уже существующий слой
			if (layerList.Count > 0)
				((Layer)layerList[ActiveLayerIndex]).IsActive = false;
			//создать новый слой, сделать его видимым и активным
			Layer l = new Layer();
			l.IsVisible = true;
			l.IsActive = true;
			l.LayerName = theName;
			//инициализируем слои для будущих проектов
			l.Graphics = new GraphicsList();
			//добавить слои
			Add(l);
		}

		public void InactivateAllLayers()
		{
			foreach (Layer l in layerList)
			{
				l.IsActive = false;
				if (l.Graphics != null)
					l.Graphics.UnselectAll();
			}
		}

		public void MakeLayerInvisible(int p)
		{
			if (p > -1 &&
			    p < layerList.Count)
				((Layer)layerList[p]).IsVisible = false;
		}

		public void MakeLayerVisible(int p)
		{
			if (p > -1 &&
			    p < layerList.Count)
				((Layer)layerList[p]).IsVisible = true;
		}


		public void SetActiveLayer(int p)
		{
			// Ensure the index is valid
			if (p > -1 &&
			    p < layerList.Count)
			{

				((Layer)layerList[p]).IsActive = true;
				((Layer)layerList[p]).IsVisible = true;
			}
		}

		public void RemoveLayer(int p)
		{
			if (ActiveLayerIndex == p)
			{
				MessageBox.Show("Cannot Remove the Active Layer");
				return;
			}
			if (layerList.Count == 1)
			{
				MessageBox.Show("There is only one Layer in this drawing! You Cannot Remove the Only Layer!");
				return;
			}
			if (p > -1 &&
			    p < layerList.Count)
			{
				((Layer)layerList[p]).Graphics.Clear();
				layerList.RemoveAt(p);
			}
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
					if (layerList != null)
					{
						foreach (Layer layer in layerList)
						{
							if (layer != null)
							{
								layer.Dispose();
							}
						}
					}
				}

				this._disposed = true;
			}
		}
	}
}