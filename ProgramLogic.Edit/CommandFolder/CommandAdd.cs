using System;

namespace ProgramLogic.Edit
{
    /// <summary>
    /// �������: �������� ����� ������
    /// </summary>
    internal class CommandAdd : Command, IDisposable
    {
        private DrawObject drawObject;

        // ������� ������� � ���������������� ��������� �������, ����������� � ������
        public CommandAdd(DrawObject drawObject) : base()
        {
            // ������� ����� �������
            this.drawObject = drawObject.Clone();
        }

        /// <summary>
        /// �������� ��������� ������� ���������
        /// </summary>
        /// <param name="list">Layers collection</param>
        public override void Undo(Layers list)
        {
            list[list.ActiveLayerIndex].Graphics.DeleteLastAddedObject();
        }

        /// <summary>
        /// ��������� ��������� ������� ����������
        /// </summary>
        /// <param name="list">Layers collection</param>
        public override void Redo(Layers list)
        {
            list[list.ActiveLayerIndex].Graphics.UnselectAll();
            list[list.ActiveLayerIndex].Graphics.Add(drawObject);

            if (drawObject is DrawImage && (drawObject as DrawImage).IsInitialImage)
            {
                drawObject.Selected = true;
                list[list.ActiveLayerIndex].Graphics.MoveSelectionToBack();
                list[list.ActiveLayerIndex].Graphics.UnselectAll();
            }
        }

        #region Destruction
        // ����: �������� �� dispose?
        bool _disposed = false;

        // ���������� ���������� �������. 
        public override void Dispose(bool disposing)
        {
            if (!this._disposed)
            {

                if (disposing)
                {
                    // Free any managed objects here. 
                    if (this.drawObject != null)
                    {
                        this.drawObject.Dispose();
                    }
                }

                // Free any unmanaged objects here. 
                this._disposed = true;
            }
        }
        #endregion
    }
}