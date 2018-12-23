using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;
using System.Windows.Forms;

using DocManagerHelper;

namespace ProgramLogic.Edit
{
	public partial class MainForm : UserControl
	{
		private DrawArea drawArea;
		private DocManagerHelper.DocManager docManager;

		private string argumentFile = ""; // file name from command line

		private bool _controlKey = false;
		private bool _panMode = false;


		public Form ParentForm { get; set; }
		public string ArgumentFile
		{
			get { return argumentFile; }
			set { argumentFile = value; }
		}

		public ToolStripMenuItem ContextParent
		{
			get { return editToolStripMenuItem; }
		}

		public Image InitialImage { get; set; }
		public string InitialImageAsFilePath{get; set;}
		public byte[] InitialImageAsPngBytes{get; set;}

		public Image Image
		{
			get
			{
				Bitmap b = new Bitmap(drawArea.Width, drawArea.Height);
				using (Graphics g = Graphics.FromImage(b))
				{
					g.Clear(Color.White);
					drawArea.TheLayers.Draw(g);
				}
				return b;
			}
		}

		private bool _zoomOnMouseWheel = false;
		public bool ZoomOnMouseWheel
		{
			get
			{
				return _zoomOnMouseWheel;
			}
			set
			{
				_zoomOnMouseWheel = value;
			}
		}

		public MainForm()
		{
			InitializeComponent();
			MouseWheel += new MouseEventHandler(MainForm_MouseWheel);
			if (this._zoomOnMouseWheel)
			{
				this.pnlDrawArea.MouseWheel += new MouseEventHandler(MainForm_MouseWheel);
			}
		}

		public void Initialize(Form parentForm)
		{
			this.ParentForm = parentForm;
		}

		#region Destructor
		private volatile bool _disposed = false;
		private volatile bool _disposingOrDisposed = false;

		protected override void Dispose(bool disposing)
		{
			this._disposingOrDisposed = true;
			if (!this._disposed)
			{
				if (disposing)
				{

					if (this.InitialImage != null)
					{
						this.InitialImage.Dispose();
					}
					if (this.drawArea != null)
					{
						this.drawArea.Dispose();
					}

					if(components != null)
					{
						components.Dispose();
					}
				}

				this._disposed = true;
			}
			base.Dispose(disposing);
		}

		#endregion


		private void toolStripButtonNew_Click(object sender, EventArgs e)
		{
			CommandNew();
		}

		private void toolStripButtonOpen_Click(object sender, EventArgs e)
		{
			CommandOpen();
		}

		private void toolStripButtonSave_Click(object sender, EventArgs e)
		{
			CommandSave();
		}

		private void toolStripButtonPointer_Click(object sender, EventArgs e)
		{
			CommandPointer();
		}

		private void toolStripButtonUndo_Click(object sender, EventArgs e)
		{
			CommandUndo();
		}

		private void toolStripButtonRedo_Click(object sender, EventArgs e)
		{
			CommandRedo();
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CommandNew();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CommandOpen();
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CommandSave();
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CommandSaveAs();
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			int x = drawArea.TheLayers.ActiveLayerIndex;
			drawArea.TheLayers[x].Graphics.SelectAll();
			drawArea.Refresh();
		}

		private void unselectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			int x = drawArea.TheLayers.ActiveLayerIndex;
			drawArea.TheLayers[x].Graphics.UnselectAll();
			drawArea.Refresh();
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			int x = drawArea.TheLayers.ActiveLayerIndex;
			CommandDelete command = new CommandDelete(drawArea.TheLayers);

			if (drawArea.TheLayers[x].Graphics.DeleteSelection())
			{
				drawArea.Refresh();
				drawArea.AddCommandToHistory(command);
			}
		}

		private void deleteAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			Clear(false);
		}

		private void Clear(bool clearHistory)
		{
			int x = drawArea.TheLayers.ActiveLayerIndex;
			CommandDeleteAll command = new CommandDeleteAll(drawArea.TheLayers);

			if (drawArea.TheLayers[x].Graphics.Clear())
			{
				drawArea.Refresh();
				drawArea.AddCommandToHistory(command);
			}

			if (clearHistory)
			{
				drawArea.ClearHistory();
			}
		}

		private void moveToFrontToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			int x = drawArea.TheLayers.ActiveLayerIndex;
			if (drawArea.TheLayers[x].Graphics.MoveSelectionToFront())
			{
				drawArea.Refresh();
			}
		}

		private void moveToBackToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			int x = drawArea.TheLayers.ActiveLayerIndex;
			if (drawArea.TheLayers[x].Graphics.MoveSelectionToBack())
			{
				drawArea.Refresh();
			}
		}

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void pointerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CommandPointer();
		}

		private void rectangleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CommandRectangle();
		}


		private void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CommandUndo();
		}

		private void redoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CommandRedo();
		}

		#region DocManager Event Handlers
		/// Load document from the stream supplied by DocManager
		private void docManager_LoadEvent(object sender, SerializationEventArgs e)
		{
			// DocManager asks to load document from supplied stream
			try
			{
				drawArea.TheLayers = (Layers)e.Formatter.Deserialize(e.SerializationStream);
			} catch (ArgumentNullException ex)
			{
				HandleLoadException(ex, e);
			} catch (SerializationException ex)
			{
				HandleLoadException(ex, e);
			} catch (SecurityException ex)
			{
				HandleLoadException(ex, e);
			}
		}

		/// Save document to stream supplied by DocManager
		private void docManager_SaveEvent(object sender, SerializationEventArgs e)
		{
			// DocManager asks to save document to supplied stream
			try
			{
				e.Formatter.Serialize(e.SerializationStream, drawArea.TheLayers);
			} catch (ArgumentNullException ex)
			{
				HandleSaveException(ex, e);
			} catch (SerializationException ex)
			{
				HandleSaveException(ex, e);
			} catch (SecurityException ex)
			{
				HandleSaveException(ex, e);
			}
		}
		#endregion

		#region Event Handlers
		private void MainForm_Load(object sender, EventArgs e)
		{
			//создадим область для рисования
			drawArea = new DrawArea();
			drawArea.MyParent = this;
			drawArea.Location = new Point(0, 0);
			drawArea.Size = new Size(10, 10);
			drawArea.Owner = this;
			drawArea.BorderStyle = BorderStyle.None;
			this.pnlDrawArea.Controls.Add(drawArea);

			drawArea.Initialize(this, docManager, InitialImage, InitialImageAsFilePath, InitialImageAsPngBytes);
			ResizeDrawArea();


			// Submit to Idle event to set controls state at idle time
			Application.Idle += delegate
			                    {
				                    if (!this._disposingOrDisposed)
				                    {
					                    this.ResizeDrawArea();					               
					                    SetStateOfControls();
				                    }
			                    };

			if (ArgumentFile.Length > 0)
				OpenDocument(ArgumentFile);
			SetStateOfControls();
		}

        /// /// Resize draw area when form is resized
			private void MainForm_Resize(object sender, EventArgs e)
		{
			if (drawArea != null)
			{
				ResizeDrawArea();
			}
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason ==
				CloseReason.UserClosing)
			{
				if (!docManager.CloseDocument())
					e.Cancel = true;
			}

		}

		/// Popup menu item (File, Edit ...) is opened.
		private void MainForm_DropDownOpened(object sender, EventArgs e)
		{

			drawArea.ActiveTool = DrawArea.DrawToolType.Pointer;
		}
		#endregion Event Handlers

        /// Set state of controls.
		/// Function is called at idle time.
		public void SetStateOfControls()
		{
			// Select active tool
			toolStripButtonPointer.Checked = !drawArea.Panning && (drawArea.ActiveTool == DrawArea.DrawToolType.Pointer);


			pointerToolStripMenuItem.Checked = (drawArea.ActiveTool == DrawArea.DrawToolType.Pointer);
			rectangleToolStripMenuItem.Checked = (drawArea.ActiveTool == DrawArea.DrawToolType.Rectangle);

			int x = drawArea.TheLayers.ActiveLayerIndex;
			bool objects = (drawArea.TheLayers[x].Graphics.Count > 0);
			bool selectedObjects = (drawArea.TheLayers[x].Graphics.SelectionCount > 0);
			// File operations
			saveToolStripMenuItem.Enabled = objects;
			//toolStripButtonSave.Enabled = objects;
			saveAsToolStripMenuItem.Enabled = objects;

			// Edit operations
			deleteToolStripMenuItem.Enabled = selectedObjects;
			deleteAllToolStripMenuItem.Enabled = objects;
			selectAllToolStripMenuItem.Enabled = objects;
			unselectAllToolStripMenuItem.Enabled = objects;
			moveToFrontToolStripMenuItem.Enabled = selectedObjects;
			moveToBackToolStripMenuItem.Enabled = selectedObjects;
			propertiesToolStripMenuItem.Enabled = selectedObjects;

			// Undo, Redo
			undoToolStripMenuItem.Enabled = drawArea.CanUndo;
			toolStripButtonUndo.Enabled = drawArea.CanUndo;

			redoToolStripMenuItem.Enabled = drawArea.CanRedo;
			toolStripButtonRedo.Enabled = drawArea.CanRedo;


			// Pan button
			tsbPanMode.Checked = drawArea.Panning;
		}

		/// Set draw area to all form client space except toolbar
		private void ResizeDrawArea()
		{
			var bounds = drawArea.GetBounds();

			drawArea.Width = Math.Max(this.pnlDrawArea.ClientRectangle.Width , (int)Math.Round((bounds.Left+ bounds.Width+10)*drawArea.Zoom));
			drawArea.Height = Math.Max(this.pnlDrawArea.ClientRectangle.Height , (int)Math.Round((bounds.Top+bounds.Height+10)*drawArea.Zoom));
			this.pnlDrawArea.Invalidate();
			;
		}

		private void HandleLoadException(Exception ex, SerializationEventArgs e)
		{
			MessageBox.Show(this,
							"Open File operation failed. File name: " + e.FileName + "\n" +
							"Reason: " + ex.Message,
							Application.ProductName);

			e.Error = true;
		}

		private void HandleSaveException(Exception ex, SerializationEventArgs e)
		{
			MessageBox.Show(this,
							"Save File operation failed. File name: " + e.FileName + "\n" +
							"Reason: " + ex.Message,
							Application.ProductName);

			e.Error = true;
		}


		public void OpenDocument(string file)
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			docManager.OpenDocument(file);
		}

		/// <summary>
		/// Set Pointer draw tool
		/// </summary>
		private void CommandPointer()
		{
			drawArea.ActiveTool = DrawArea.DrawToolType.Pointer;

			this._panMode = false;
			drawArea.Panning = this._panMode;
		}


		private void CommandRectangle()
		{
			drawArea.ActiveTool = DrawArea.DrawToolType.Rectangle;
			drawArea.DrawFilled = false;

			drawArea.EndCap = LineCap.Round;
			this._panMode = false;
			drawArea.Panning = this._panMode;
		}

		/// Open new file
		private void CommandNew()
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			docManager.NewDocument();
		}

		/// Open file
		private void CommandOpen()
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			docManager.OpenDocument("");
		}

		/// Save file
		private void CommandSave()
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

            docManager.SaveDocument(DocManagerHelper.DocManager.SaveType.Save);
		}

		/// Save As
		private void CommandSaveAs()
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

            docManager.SaveDocument(DocManagerHelper.DocManager.SaveType.SaveAs);
		}

		/// Undo
		private void CommandUndo()
		{
			drawArea.Undo();
		}

		/// Redo
		private void CommandRedo()
		{
			drawArea.Redo();
		}

        private void MainForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta != 0)
            {
                if (_zoomOnMouseWheel)
                {
                    ((HandledMouseEventArgs)e).Handled = true;
                }

                if (_controlKey)
                {
                    // We are panning up or down using the wheel
                    if (e.Delta > 0)
                        this.ManualScroll(false, 10);
                    else
                        this.ManualScroll(false, -10);
                    Invalidate();
                }
                else
                {
                    if (_zoomOnMouseWheel)
                    {
                        // We are zooming in or out using the wheel
                        if (e.Delta > 0)
                            AdjustZoom(.1f);
                        else
                            AdjustZoom(-.1f);
                    }
                }
                SetStateOfControls();
                return;
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			int al = drawArea.TheLayers.ActiveLayerIndex;
			switch (e.KeyCode)
			{
				case Keys.Delete:
					drawArea.TheLayers[al].Graphics.DeleteSelection();
					drawArea.Invalidate();
					break;
				case Keys.Right:
					this.ManualScroll(true,-10);
					drawArea.Invalidate();
					break;
				case Keys.Left:
					this.ManualScroll(true,+10);
					drawArea.Invalidate();
					break;
				case Keys.Up:
					if (e.KeyCode == Keys.Up &&
						e.Shift)
						AdjustZoom(.1f);
					else
						drawArea.PanY += 10;
					drawArea.Invalidate();
					break;
				case Keys.Down:
					if (e.KeyCode == Keys.Down &&
						e.Shift)
						AdjustZoom(-.1f);
					else
						drawArea.PanY -= 10;
					drawArea.Invalidate();
					break;
				case Keys.ControlKey:
					_controlKey = true;
					break;
				default:
					break;
			}
			drawArea.Invalidate();
			SetStateOfControls();
		}

		private void MainForm_KeyUp(object sender, KeyEventArgs e)
		{
			_controlKey = false;
		}

		private void AdjustZoom(float _amount)
		{
			drawArea.Zoom += _amount;
			if (drawArea.Zoom < .1f)
				drawArea.Zoom = .1f;
			if (drawArea.Zoom > 10)
				drawArea.Zoom = 10f;

			drawArea.Invalidate();
			SetStateOfControls();
		}


		private void tsbRotateRight_Click(object sender, EventArgs e)
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			int al = drawArea.TheLayers.ActiveLayerIndex;
			if (drawArea.TheLayers[al].Graphics.SelectionCount > 0)
				RotateObject(10);
			else
				RotateDrawing(10);
		}

		private void tsbRotateLeft_Click(object sender, EventArgs e)
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			int al = drawArea.TheLayers.ActiveLayerIndex;
			if (drawArea.TheLayers[al].Graphics.SelectionCount > 0)
				RotateObject(-10);
			else
				RotateDrawing(-10);
		}

		private void tsbRotateReset_Click(object sender, EventArgs e)
		{
			this._panMode = false;
			drawArea.Panning = this._panMode;

			int al = drawArea.TheLayers.ActiveLayerIndex;
			if (drawArea.TheLayers[al].Graphics.SelectionCount > 0)
				RotateObject(0);
			else
				RotateDrawing(0);
		}

		/// Rotate the selected Object(s)
		///Amount to rotate. Negative is Left, Positive is Right, Zero indicates Reset to zero</param>
		private void RotateObject(int p)
		{
			int al = drawArea.TheLayers.ActiveLayerIndex;
			for (int i = 0; i < drawArea.TheLayers[al].Graphics.Count; i++)
			{
				if (drawArea.TheLayers[al].Graphics[i].Selected)
					if (p == 0)
						drawArea.TheLayers[al].Graphics[i].Rotation = 0;
					else
						drawArea.TheLayers[al].Graphics[i].Rotation += p;
			}
		
			this._panMode = false;
			drawArea.Panning = this._panMode;
	
			drawArea.Invalidate();
			SetStateOfControls();
		}

		/// Rotate the entire drawing
		/// Negative is Left, Positive is Right, Zero indicates Reset to zero</param>
		private void RotateDrawing(int p)
		{
			if (p == 0)
				drawArea.Rotation = 0;
			else
			{
				drawArea.Rotation += p;
				if (p < 0) // Left Rotation
				{
					if (drawArea.Rotation <
						-360)
						drawArea.Rotation = 0;
				} else
				{
					if (drawArea.Rotation > 360)
						drawArea.Rotation = 0;
				}
			}
		
			this._panMode = false;
			drawArea.Panning = this._panMode;
		
			drawArea.Invalidate();
			SetStateOfControls();
		}

		private void tsbPanMode_Click(object sender, EventArgs e)
		{
			_panMode = true;//!_panMode;
			if (_panMode)
			{
				tsbPanMode.Checked = true;
			}
			else
			{
				tsbPanMode.Checked = false;
			}
			drawArea.ActiveTool = DrawArea.DrawToolType.Pointer;
			drawArea.Panning = _panMode;
		}

		private void tsbPanReset_Click(object sender, EventArgs e)
		{
			_panMode = false;
			if (tsbPanMode.Checked)
				tsbPanMode.Checked = false;
			drawArea.Panning = false;
			drawArea.PanX = 0;
			drawArea.PanY = drawArea.OriginalPanY;
			drawArea.Invalidate();
		}


		/// Draw PolyLine objects - a polyline is a series of straight lines of various lengths connected at their end points.

		private void tsbPolyLine_Click(object sender, EventArgs e)
		{
			drawArea.ActiveTool = DrawArea.DrawToolType.PolyLine;
			drawArea.DrawFilled = false;

			this._panMode = false;
			drawArea.Panning = this._panMode;
		}

		private void tsbConnector_Click(object sender, EventArgs e)
		{
			drawArea.ActiveTool = DrawArea.DrawToolType.Connector;
			drawArea.DrawFilled = false;
	
			this._panMode = false;
			drawArea.Panning = this._panMode;
		}

		/// Draw Text objects
		private void tsbDrawText_Click(object sender, EventArgs e)
		{
			drawArea.ActiveTool = DrawArea.DrawToolType.Text;
	
			this._panMode = false;
			drawArea.Panning = this._panMode;
		}

		

		private void tsbImage_Click(object sender, EventArgs e)
		{
			drawArea.ActiveTool = DrawArea.DrawToolType.Image;
	
			drawArea.EndCap = LineCap.Round;
			this._panMode = false;
			drawArea.Panning = this._panMode;
		}


		public void ExportToFile(string filePath, ImageFormat imageFormat)
		{
			using (Bitmap b = new Bitmap(drawArea.Width, drawArea.Height))
			{
				using (Graphics g = Graphics.FromImage(b))
				{
					g.Clear(Color.White);
					drawArea.DeselectAll();
					drawArea.TheLayers.Draw(g);
					b.Save(filePath, imageFormat);
				}
			}
		}
		
		public Image ExportToImage()
		{
			Bitmap b = new Bitmap(drawArea.Width, drawArea.Height);
			Graphics g = Graphics.FromImage(b);
			g.Clear(Color.White);
			drawArea.DeselectAll();
			drawArea.TheLayers.Draw(g);
			return b;
		}

		private void exportToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Bitmap b = new Bitmap(drawArea.Width, drawArea.Height);
			Graphics g = Graphics.FromImage(b);
			g.Clear(Color.White);
			drawArea.TheLayers.Draw(g);
			b.Save(@"c:\test.bmp", ImageFormat.Bmp);
			MessageBox.Show("save complete!");
			g.Dispose();
			b.Dispose();
		}

		public void CopyAllToClipboard()
		{
			using (Image image = this.ExportToImage())
			{
				Clipboard.SetDataObject(image, true);

				using (RichTextBox tempRtb = new RichTextBox())
				{
					tempRtb.WordWrap = false;

					tempRtb.Paste();
					Application.DoEvents();
					Thread.Sleep(500);

					tempRtb.SelectAll();

					tempRtb.Copy();
					Application.DoEvents();
					Thread.Sleep(500);
				}
			}
		}

    private void cutToolStripMenuItem_Click(object sender, EventArgs e)
    {
      drawArea.CutObject();
    }

	private void solidToolStripMenuItem_Click(object sender, EventArgs e)
	{
		this._panMode = false;
		drawArea.Panning = this._panMode;
	}

	private void dottedToolStripMenuItem_Click(object sender, EventArgs e)
	{//
		this._panMode = false;
		drawArea.Panning = this._panMode;
	}

	private void dashedToolStripMenuItem_Click(object sender, EventArgs e)
	{
		this._panMode = false;
		drawArea.Panning = this._panMode;
	}

	private void dotDashedtoolStripMenuItem7_Click(object sender, EventArgs e)
	{
		this._panMode = false;
		drawArea.Panning = this._panMode;
	}

	private void doubleLineToolStripMenuItem8_Click(object sender, EventArgs e)
	{
		this._panMode = false;
		drawArea.Panning = this._panMode;
	}

	private void ctxtMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
	{
		this.undoToolStripMenuItem2.Enabled = drawArea.CanUndo;
		this.toolStripMenuItem13.Enabled = drawArea.CanRedo;
	}

		/// Clears graphics and history and reloads initial image if present.
		public void Reset()
		{
			this.Clear(true);
			drawArea.LoadInitialImage(InitialImage, InitialImageAsFilePath, InitialImageAsPngBytes, null);
		}

        private void pnlDrawArea_Scroll(object sender, ScrollEventArgs e)
        {

        }

        internal void ManualScroll(bool isHorizontal, int delta)
		{
			this.ResizeDrawArea();
			int newValue;
			if (isHorizontal)
			{
				newValue = this.pnlDrawArea.HorizontalScroll.Value + delta;
				newValue = Math.Max(this.pnlDrawArea.HorizontalScroll.Minimum, newValue);
				newValue = Math.Min(this.pnlDrawArea.HorizontalScroll.Maximum, newValue);
				this.pnlDrawArea.HorizontalScroll.Value = newValue;
			}
			else
			{
				newValue = this.pnlDrawArea.VerticalScroll.Value + delta;
				newValue = Math.Max(this.pnlDrawArea.VerticalScroll.Minimum, newValue);
				newValue = Math.Min(this.pnlDrawArea.VerticalScroll.Maximum, newValue);
				this.pnlDrawArea.VerticalScroll.Value = newValue;
			}
			this.pnlDrawArea.Invalidate();
		}

		public void ReplaceInitialImage(Image image, bool preserveSize, bool addNewIfNotFound)
		{
			this.InitialImageAsFilePath = null;
			this.InitialImageAsPngBytes = null;
			this.InitialImage = image;
			
			var indexAndInitialImage = drawArea.GetInitialImageGraphic();
			if (indexAndInitialImage != null)
			{
				if (!preserveSize)
				{
					indexAndInitialImage.Value.Value.rectangle.Width = image.Width;
					indexAndInitialImage.Value.Value.rectangle.Height = image.Height;
				}
				indexAndInitialImage.Value.Value.TheImage = (Bitmap) image;
				
				drawArea.Invalidate();
			}
			else
			{
				if (addNewIfNotFound)
				{
					drawArea.LoadInitialImage(InitialImage, InitialImageAsFilePath, InitialImageAsPngBytes, null);
				}
			}
		}
    }
}