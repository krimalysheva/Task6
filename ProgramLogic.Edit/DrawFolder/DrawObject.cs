using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace ProgramLogic.Edit
{
	[Serializable]
	public abstract class DrawObject : IComparable, IDisposable
	{
		private bool selected;
		private Color color;
		private int penWidth;
		private LineCap _endCap;
		private string tipText;

		//для сериализации
		private const string entryColor = "Color";
		private const string entryPenWidth = "PenWidth";
		private const string entryPen = "DrawPen";
		private const string entryBrush = "DrawBrush";
		private const string entryFillColor = "FillColor";
		private const string entryFilled = "Filled";
		private const string entryZOrder = "ZOrder";
		private const string entryRotation = "Rotation";
		private const string entryTipText = "TipText";

		private bool dirty;
		private int _id;
		private int _zOrder;
		private int _rotation = 0;
		private Point _center;

		private bool _disposed;


        //центр изображения
		public Point Center
		{
			get { return _center; }
			set { _center = value; }
		}

		//поворот объекта. - is Left, + is Right.

		public int Rotation
		{
			get { return _rotation; }
			set
			{
				if (value > 360)
					_rotation = value - 360;
				else if (value < -360)
					_rotation = value + 360;
				else
					_rotation = value;
			}
		}

        //ZOrder-порядок отображения объектов - чем ниже ZOrder порядок, тем ближе к вершине находится объект.
        public int ZOrder
		{
			get { return _zOrder; }
			set { _zOrder = value; }
		}

		//Object ID used for Undo\Redo functions
		public int ID
		{
			get { return _id; }
			set { _id = value; }
		}

		//Set to true whenever the object changes!!!
		public bool Dirty
		{
			get { return dirty; }
			set { dirty = value; }
		}

		//Selection flag
		public bool Selected
		{
			get { return selected; }
			set { selected = value; }
		}

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        public LineCap EndCap
		{
			get { return _endCap; }
			set { _endCap = value; }
		}

		//число вращений
		public virtual int HandleCount
		{
			get { return 0; }
		}

		//Number of Connection Points
		public virtual int ConnectionCount
		{
			get { return 0; }
		}

		//клонировать состояние
		public abstract DrawObject Clone();

		//Draw object
		public virtual void Draw(Graphics g)
		{
		}

		/// <summary>
		/// Get handle point by 1-based number
		/// </summary>
		/// <param name="handleNumber">1-based handle number to return</param>
		/// <returns>Point where handle is located, if found</returns>
		public virtual Point GetHandle(int handleNumber)
		{
			return new Point(0, 0);
		}

        /// <summary>
        /// Get handle rectangle by 1-based number
        /// </summary>
        /// <param name="handleNumber"></param>
        /// <returns>Rectangle structure to draw the handle</returns>
        public virtual Rectangle GetHandleRectangle(int handleNumber)
        {
            Point point = GetHandle(handleNumber);
            // Take into account width of pen
            return new Rectangle(point.X - (penWidth + 3), point.Y - (penWidth + 3), 7 + penWidth, 7 + penWidth);
        }

        //Draw tracker for selected object
        public virtual void DrawTracker(Graphics g)
        {
            if (!Selected)
                return;
            SolidBrush brush = new SolidBrush(Color.Black);

            for (int i = 1; i <= HandleCount; i++)
            {
                g.FillRectangle(brush, GetHandleRectangle(i));
            }
            brush.Dispose();
        }

        /// <summary>
        /// Get connection point by 0-based number
        /// </summary>
        /// <param name="connectionNumber">0-based connection number to return</param>
        /// <returns>Point where connection is located, if found</returns>
        public virtual Point GetConnection(int connectionNumber)
		{
			return new Point(0, 0);
		}
    
        /// <summary>
        /// Hit test to determine if object is hit.
        /// </summary>
        /// <param name="point">Point to test</param>
        /// <returns>			(-1)		no hit
        ///						(0)		hit anywhere
        ///						(1 to n)	handle number</returns>
        public virtual int HitTest(Point point)
		{
			return -1;
		}

		protected virtual bool PointInObject(Point point)
		{
			return false;
		}


		public abstract Rectangle GetBounds(Graphics g);

		public virtual Cursor GetHandleCursor(int handleNumber)
		{
			return Cursors.Default;
		}

		//
		public virtual bool IntersectsWith(Rectangle rectangle)
		{
			return false;
		}

        /// <summary>
        /// устроим движ объектов туц-туц
        /// </summary>
        /// <param name="deltaX">расстояние вдоль оси X: (+)=правый (-)=левый</param>
        /// <param name="deltaY">расстояние по оси Y: (+) =вниз, (- ) =вверх</param>
        public virtual void Move(int deltaX, int deltaY)
		{
		}

		//напривать управление курсов на объект 
		public virtual void MoveHandleTo(Point point, int handleNumber)
		{
		}

        //Normalize object.
        //вызов после оберзки
        public virtual void Normalize()
        {
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

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

		public virtual void SaveToStream(SerializationInfo info, int orderNumber, int objectIndex)
		{
            info.AddValue(
                String.Format(CultureInfo.InvariantCulture,
                              "{0}{1}-{2}",
                              entryColor, orderNumber, objectIndex),
                Color.ToArgb());

			info.AddValue(
				String.Format(CultureInfo.InvariantCulture,
							  "{0}{1}-{2}",
							  entryZOrder, orderNumber, objectIndex),
				ZOrder);

			info.AddValue(
				String.Format(CultureInfo.InvariantCulture,
							  "{0}{1}-{2}",
							  entryRotation, orderNumber, objectIndex),
				Rotation);

			info.AddValue(
				String.Format(CultureInfo.InvariantCulture, 
							  "{0}{1}-{2}",
							  entryTipText, orderNumber, objectIndex),
				tipText);
		}

		public virtual void LoadFromStream(SerializationInfo info, int orderNumber, int objectData)
		{
			int n = info.GetInt32(
				String.Format(CultureInfo.InvariantCulture,
							  "{0}{1}-{2}",
							  entryColor, orderNumber, objectData));

			Color = Color.FromArgb(n);

			n = info.GetInt32(
				String.Format(CultureInfo.InvariantCulture,
							  "{0}{1}-{2}",
							  entryFillColor, orderNumber, objectData));

			ZOrder = info.GetInt32(
				String.Format(CultureInfo.InvariantCulture,
							  "{0}{1}-{2}",
							  entryZOrder, orderNumber, objectData));

			Rotation = info.GetInt32(
				String.Format(CultureInfo.InvariantCulture,
							  "{0}{1}-{2}",
							  entryRotation, orderNumber, objectData));

			tipText = info.GetString(String.Format(CultureInfo.InvariantCulture,
							  "{0}{1}-{2}",
							  entryTipText, orderNumber, objectData));
		}

		protected void Initialize()
		{
		}

        /// <summary>
        /// возврат (-1), (0), (+1) для представления относительного Z-порядка сравниваемого объекта
        /// </summary>
        /// <param name="obj">DrawObject that is compared to this object</param>
        /// <returns>	(-1), если объект меньше (дальше), чем этот объект.
        ///				(0) если объект равен этому объекту (тот же уровень графически).
        ///             (1) если объект больше (ближе к фронту), чем этот объект.</returns>
        public int CompareTo(object obj)
		{
			DrawObject d = obj as DrawObject;
			int x = 0;
			if (d != null)
				if (d.ZOrder == ZOrder)
					x = 0;
				else if (d.ZOrder > ZOrder)
					x = -1;
				else
					x = 1;

			return x;
		}
	}
}