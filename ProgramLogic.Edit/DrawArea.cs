using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using DocManagerHelper;

namespace ProgramLogic.Edit
{
    /// <summary>
    /// рабочая зона.
    /// обрабатывает ввод мышью и рисует графические объекты.
    /// </summary>
    internal partial class DrawArea : UserControl, IDisposable
  {
    public DrawArea()
    {
      //создать лист слоев, где 1 по умл. явлю видимым
      _layers = new Layers();
      _layers.CreateNewLayer("Default");
      _panning = false;
      _panX = 0;
      _panY = 0;
      this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DrawArea_MouseWheel);

      InitializeComponent();
    }

	public new void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);       
	}
	bool _disposed = false;


        protected override void Dispose(bool disposing)
	{
		if (!this._disposed)
		{
			

			if (disposing)
			{
                    if (this._currentBrush != null)
                    {
                        this._currentBrush.Dispose();
                    }
                    if (this.CurrentPen != null)
                    {
                        this._currentPen.Dispose();
                    }
                    if (this._layers != null)
				{
					this._layers.Dispose();
				}
				if (this.tools != null)
				{
					foreach (Tool tool in tools)
					{
						if (tool != null)
						{
							tool.Dispose();
						}
					}
				}
				if (this.undoManager != null)
				{
					this.undoManager.Dispose();
				}

				if (components != null)
				{
					components.Dispose();
				}
			}

			this._disposed = true;

		}
		base.Dispose(disposing);
	}

    public enum DrawToolType
    {
      Pointer,
      Rectangle,
      Line,
      PolyLine,
      Text,
      Image,
      Connector,
      NumberOfDrawTools
    } ;

    private float _zoom = 1.0f;
    private float _rotation = 0f;
    private int _panX = 0;
    private int _panY;
    private int _originalPanY;
    private bool _panning = false;
    private Point lastPoint;
    private Color _lineColor = Color.Red;
    private Color _fillColor = Color.White;
    private bool _drawFilled = false;
    private int _lineWidth = 5;
	private LineCap _endCap = LineCap.Round;
    private Pen _currentPen;
    private Brush _currentBrush;


    private Layers _layers;

    private DrawToolType activeTool; // active drawing tool
    private Tool[] tools; // array of tools

    // Information about owner form
    private MainForm owner;
    private DocManagerHelper.DocManager docManager;

    // group selection rectangle
    private Rectangle netRectangle;
    private bool drawNetRectangle = false;

    private MainForm myparent;

    public MainForm MyParent
    {
      get { return myparent; }
      set { myparent = value; }
    }

    private UndoManager undoManager;


        public LineCap EndCap
        {
            get { return _endCap; }
            set { _endCap = value; }
        }

        /// Current Drawing Pen
        public Pen CurrentPen
    {
      get { return _currentPen; }
      set { _currentPen = value; }
    }


    /// Current Line Width
    public int LineWidth
    {
      get { return _lineWidth; }
      set { _lineWidth = value; }
    }

        /// Флаг определяет, будут ли объекты отрисовываться заполненными или нет
        public bool DrawFilled
    {
      get { return _drawFilled; }
      set { _drawFilled = value; }
    }

    /// цвет заполненных объектов
    public Color FillColor
    {
      get { return _fillColor; }
      set { _fillColor = value; }
    }

    /// цвет нарисованных линий
    public Color LineColor
    {
      get { return _lineColor; }
      set { _lineColor = value; }
    }

    /// Original Y position - испю при прокрутке
    public int OriginalPanY
    {
      get { return _originalPanY; }
      set { _originalPanY = value; }
    }
        
    /// Flag is true прокрутка активна
    public bool Panning
    {
      get { return _panning; }
      set { _panning = value; }
    }

    /// <summary>
    /// Current pan offset along X-axis
    /// </summary>
    public int PanX
    {
      get { return _panX; }
      set { _panX = value; }
    }

    /// <summary>
    /// Current pan offset along Y-axis
    /// </summary>
    public int PanY
    {
      get { return _panY; }
      set { _panY = value; }
    }

    //degrees of rotation of the drawing
    public float Rotation
    {
      get { return _rotation; }
      set { _rotation = value; }
    }

    /// <summary>
    /// Current Zoom factor
    /// </summary>
    public float Zoom
    {
      get { return _zoom; }
      set { _zoom = value; }
    }

    //group selection rectangle. Used for drawing.
    public Rectangle NetRectangle
    {
      get { return netRectangle; }
      set { netRectangle = value; }
    }

    //flag is set to true if group selection rectangle should be drawn.
    public bool DrawNetRectangle
    {
      get { return drawNetRectangle; }
      set { drawNetRectangle = value; }
    }

    //reference to the owner form
    public MainForm Owner
    {
      get { return owner; }
      set { owner = value; }
    }


    //reference to DocManager
    public DocManager DocManager
    {
      get { return docManager; }
      set { docManager = value; }
    }

    public DrawToolType ActiveTool
    {
      get { return activeTool; }
      set { activeTool = value; }
    }


    public Layers TheLayers
    {
      get { return _layers; }
      set { _layers = value; }
    }

    //true if undo is possible
    public bool CanUndo
    {
      get
      {
        if (undoManager != null)
        {
          return undoManager.CanUndo;
        }

        return false;
      }
    }

    //true if redo is possible
    public bool CanRedo
    {
      get
      {
        if (undoManager != null)
        {
          return undoManager.CanRedo;
        }

        return false;
      }
    }

	  public Rectangle GetBounds()
	  {
		  int furthestLeft = int.MaxValue;
		  int furthestTop = int.MaxValue;
		  int furthestRight = int.MinValue;
		  int furthestBottom = int.MinValue;
		  Rectangle rect;
		  Graphics g = this.CreateGraphics();
		  if (_layers != null)
		  {
			int lc = _layers.Count;
			for (int i = 0; i < lc; i++)
                { 
			  if (_layers[i].IsVisible)
			  {
				  if (_layers[i].Graphics != null)
				  {
					  for (int ig = 0; ig < _layers[i].Graphics.Count; ig++)
					  {
						  rect = _layers[i].Graphics[ig].GetBounds(g);
						  furthestLeft = Math.Min(furthestLeft, rect.Left);
						  furthestTop = Math.Min(furthestTop, rect.Left);
						  furthestRight = Math.Max(furthestRight, rect.Right);
						  furthestBottom = Math.Max(furthestBottom, rect.Bottom);
					  }
				  }
			  }
			}
		  }
		  rect = new Rectangle(furthestLeft, furthestTop, Math.Abs(furthestRight-furthestLeft), Math.Abs(furthestBottom-furthestTop));
		  return rect;
	  }

	  internal void ReplaceInitialImage(Image image)
	  {
		  ((ProgramLogic.Edit.DrawImage) (this._layers[0].Graphics[this._layers[0].Graphics.Count - 1])).TheImage = (Bitmap)image;
		  this.Invalidate();
	  }

	  internal KeyValuePair<int,DrawImage>? GetInitialImageGraphic()
	  {
		  for (int i = this._layers[0].Graphics.Count - 1; i >= 0; i--)
		  {
			  if (this._layers[0].Graphics[i] is DrawImage && (this._layers[0].Graphics[i] as DrawImage).IsInitialImage)
			  {
				  return new KeyValuePair<int, DrawImage>(i, this._layers[0].Graphics[i] as DrawImage);
			  }
		  }
		  return null;
	  }

	  internal void DeselectAll()
	  {
		  int al = this.TheLayers.ActiveLayerIndex;
		  this.TheLayers[al].Graphics.UnselectAll();
		  this.Invalidate();
	  }


    /// Draw graphic objects and group selection rectangle (optionally)
    private void DrawArea_Paint(object sender, PaintEventArgs e)
    {
      Matrix mx = new Matrix();
      mx.Translate(-ClientSize.Width / 2f, -ClientSize.Height / 2f, MatrixOrder.Append);
      mx.Rotate(_rotation, MatrixOrder.Append);
      mx.Translate(ClientSize.Width / 2f + _panX, ClientSize.Height / 2f + _panY, MatrixOrder.Append);
      mx.Scale(_zoom, _zoom, MatrixOrder.Append);
      e.Graphics.Transform = mx;
      Point centerRectangle = new Point();
      centerRectangle.X = ClientRectangle.Left + ClientRectangle.Width / 2;
      centerRectangle.Y = ClientRectangle.Top + ClientRectangle.Height / 2;
      centerRectangle = BackTrackMouse(centerRectangle);

      SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255));
      e.Graphics.FillRectangle(brush,
                   ClientRectangle);
     
      if (_layers != null)
      {
        int lc = _layers.Count;
        for (int i = 0; i < lc; i++)
        {
          Console.WriteLine(String.Format("Layer {0} is Visible: {1}", i.ToString(), _layers[i].IsVisible.ToString()));
          if (_layers[i].IsVisible)
          {
            if (_layers[i].Graphics != null)
              _layers[i].Graphics.Draw(e.Graphics);
          }
        }
      }

      DrawNetSelection(e.Graphics);

      brush.Dispose();
    }

    //Back Track the Mouse to return accurate coordinates regardless of zoom or pan effects.
    public Point BackTrackMouse(Point p)
    {
      // Backtrack the mouse...
      Point[] pts = new Point[] { p };
      Matrix mx = new Matrix();
      mx.Translate(-ClientSize.Width / 2f, -ClientSize.Height / 2f, MatrixOrder.Append);
      mx.Rotate(_rotation, MatrixOrder.Append);
      mx.Translate(ClientSize.Width / 2f + _panX, ClientSize.Height / 2f + _panY, MatrixOrder.Append);
      mx.Scale(_zoom, _zoom, MatrixOrder.Append);
      mx.Invert();
      mx.TransformPoints(pts);
      return pts[0];
    }

    /// Mouse down.
    /// Left button down event is passed to active tool.
    /// Right button down event is handled in this class.
    private void DrawArea_MouseDown(object sender, MouseEventArgs e)
    {
      lastPoint = BackTrackMouse(e.Location);
      if (e.Button ==
        MouseButtons.Left)
        tools[(int)activeTool].OnMouseDown(this, e);
      else if (e.Button ==
           MouseButtons.Right)
      {
        if (_panning)
          _panning = false;
        if (activeTool == DrawToolType.PolyLine || activeTool == DrawToolType.Connector)
          tools[(int)activeTool].OnMouseDown(this, e);
        ActiveTool = DrawToolType.Pointer;
        OnContextMenu(e);
      }
    }

    /// Mouse move.
    /// Moving without button pressed or with left button pressed
    /// is passed to active tool.
    private void DrawArea_MouseMove(object sender, MouseEventArgs e)
    {
      Point curLoc = BackTrackMouse(e.Location);
      if (e.Button == MouseButtons.Left ||
        e.Button == MouseButtons.None)
        if (e.Button == MouseButtons.Left && _panning)
        {
          if (curLoc.X !=
            lastPoint.X)
            _panX += curLoc.X - lastPoint.X;
			
          if (curLoc.Y !=
            lastPoint.Y)
            _panY += curLoc.Y - lastPoint.Y;
			
          Invalidate();
        }
        else
          tools[(int)activeTool].OnMouseMove(this, e);
      else
        Cursor = Cursors.Default;
      lastPoint = BackTrackMouse(e.Location);
    }

    /// Mouse up event.
    /// Left button up event is passed to active tool.
    private void DrawArea_MouseUp(object sender, MouseEventArgs e)
    {
      
      if (e.Button == MouseButtons.Left)
      {
	     
	      tools[(int)activeTool].OnMouseUp(this, e); 
		  if (activeTool != DrawToolType.Pointer && activeTool!= DrawToolType.Text && activeTool!= DrawToolType.Image)
	      {
			  int al = this.TheLayers.ActiveLayerIndex;
		      this.AddCommandToHistory(new CommandAdd(this.TheLayers[al].Graphics[0]));
	      }

		  if (this.PanX != 0 || this.PanY != 0)
									{
										this.myparent.ManualScroll(true, -(int)Math.Round(this.PanX*this._zoom));
										this.myparent.ManualScroll(false, -(int)Math.Round(this.PanY*this._zoom));
										this.PanX = 0;
										this.PanY = 0;
										this.Invalidate();
										this.myparent.pnlDrawArea.Invalidate();
									}

		  this.ActiveTool = DrawArea.DrawToolType.Pointer;//Selected tool is automatically dropped after drawing item
      }
    }

	public void CutObject()
    {
      MessageBox.Show("Cut (from drawarea)");
    }

    private void DrawArea_MouseWheel(object sender, MouseEventArgs e)
    {
    }


    #region Other Functions
    /// <summary>
    /// Initialization
    /// </summary>
    /// <param name="owner">Reference to the owner form</param>
    /// <param name="docManager">Reference to Document manager</param>
    public void Initialize(MainForm owner, DocManagerHelper.DocManager docManager, Image initialImage, string initialImageAsFilePath, byte[] initialImageAsPngBytes)
    {
      SetStyle(ControlStyles.AllPaintingInWmPaint |
           ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
      Invalidate();

      // Keep reference to owner form
      Owner = owner;
      DocManager = docManager;

      // set default tool
      activeTool = DrawToolType.Pointer;

      // Create undo manager
      undoManager = new UndoManager(_layers);

      // create array of drawing tools
      tools = new Tool[(int)DrawToolType.NumberOfDrawTools];
      tools[(int)DrawToolType.Pointer] = new ToolPointer();
      tools[(int)DrawToolType.Text] = new ToolText();
      tools[(int)DrawToolType.Image] = new ToolImage();

      LineColor = Color.Red;
      FillColor = Color.White;
      LineWidth = 5;
  
		LoadInitialImage(initialImage, initialImageAsFilePath, initialImageAsPngBytes, null);
    }

	  public void LoadInitialImage(Image initialImage, string initialImageAsFilePath, byte[] initialImageAsPngBytes, DrawImage paradigm)
	  {
		  if (initialImage != null)
		  {
			  ((ToolImage) tools[(int) DrawToolType.Image]).InsertImage(this, initialImage, true, true, paradigm);
		  }

		  if (initialImageAsFilePath != null)
		  {
			  ((ToolImage) tools[(int) DrawToolType.Image]).InsertImage(this, initialImageAsFilePath, true, true, paradigm);
		  }

		  if (initialImageAsPngBytes != null)
		  {
			  ((ToolImage) tools[(int) DrawToolType.Image]).InsertImage(this, initialImageAsPngBytes, true, true, paradigm);
		  }
		 
		  this.Invalidate();
	  }

	  /// <summary>
    /// Add command to history.
    /// </summary>
    public void AddCommandToHistory(Command command)
    {
      undoManager.AddCommandToHistory(command);
    }

    /// <summary>
    /// Clear Undo history.
    /// </summary>
    public void ClearHistory()
    {
      undoManager.ClearHistory();
    }

    /// <summary>
    /// Undo
    /// </summary>
    public void Undo()
    {
      undoManager.Undo();
      Refresh();
    }

    /// <summary>
    /// Redo
    /// </summary>
    public void Redo()
    {
      undoManager.Redo();
      Refresh();
    }

    /// <summary>
    ///  Draw group selection rectangle
    /// </summary>
    /// <param name="g"></param>
    public void DrawNetSelection(Graphics g)
    {
      if (!DrawNetRectangle)
        return;

      ControlPaint.DrawFocusRectangle(g, NetRectangle, Color.Black, Color.Transparent);
    }

    /// <summary>
    /// Right-click handler
    /// </summary>
    /// <param name="e"></param>
    private void OnContextMenu(MouseEventArgs e)
    {
      // Change current selection if necessary

      Point point = BackTrackMouse(new Point(e.X, e.Y));
      Point menuPoint = new Point(e.X, e.Y);
      int al = _layers.ActiveLayerIndex;
      int n = _layers[al].Graphics.Count;
      DrawObject o = null;

      for (int i = 0; i < n; i++)
      {
        if (_layers[al].Graphics[i].HitTest(point) == 0)
        {
          o = _layers[al].Graphics[i];
          break;
        }
      }

      if (o != null)
      {
        if (!o.Selected)
          _layers[al].Graphics.UnselectAll();

        // Select clicked object
        o.Selected = true;
      }
      else
      {
        _layers[al].Graphics.UnselectAll();
      }

      Refresh();
      Owner.ctxtMenu.Show(this, menuPoint);
    }
    #endregion

   
  }
}