using System;

namespace ProgramLogic.Edit
{
    /// <summary>
    /// команды: Добавить новый объект
    /// </summary>
    internal class CommandAdd : Command, IDisposable
    {
        private DrawObject drawObject;

        // Создать команду с ДроуОбжктвывести экземпляр объекта, добавленный в список
        public CommandAdd(DrawObject drawObject) : base()
        {
            // хранить копию объекта
            this.drawObject = drawObject.Clone();
        }

        /// <summary>
        /// отменить последнюю команду добавлния
        /// </summary>
        /// <param name="list">Layers collection</param>
        public override void Undo(Layers list)
        {
            list[list.ActiveLayerIndex].Graphics.DeleteLastAddedObject();
        }

        /// <summary>
        /// повторить последнюю команду добавления
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
        // флаг: вызывали ли dispose?
        bool _disposed = false;

        // защищенная реализация Диспоуз. 
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