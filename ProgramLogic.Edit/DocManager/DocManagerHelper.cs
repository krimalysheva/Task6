using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;
using System.Security;

namespace DocManagerHelper
{

    /// <summary>
    ///Работа с файлами: сохранение, открытие, создание нового, обновление
    /// </summary>
    public class DocManager
    {

        public event SaveEventHandler SaveEvent;
        public event LoadEventHandler LoadEvent;
        public event OpenFileEventHandler OpenEvent;
        public event EventHandler ClearEvent;
        public event EventHandler DocChangedEvent;

        private string fileName = "";
        private bool dirty = false; //флаг для документа,говорящий о его повреждении

        private Form frmOwner;
        private string newDocName;
        private string fileDlgFilter;
        private string registryPath;
        private bool updateTitle;

        private const string registryValue = "Path";
        private string fileDlgInitDir = "";//инициализация файловой дирректории

        /// <summary>
        /// как сохранять будем? 
        /// </summary>
        public enum SaveType
        {
            Save,
            SaveAs
        }

        /// <summary>
        /// свойтство "Грязно", тру,если имеет несохраненные изменения
        /// </summary>
        public bool Dirty
        {
            get
            {
                return dirty;
            }
            set
            {
                dirty = value;
            }
        }

        /// <summary>
        /// новый документ
        /// </summary>
        /// <returns></returns>
        public bool NewDocument()
        {
            if (!CloseDocument())
                return false;

            SetFileName("");

            if (ClearEvent != null)
            {
                // вызваем событие для очистки содержимого документа в памяти
                ClearEvent(this, new EventArgs());
            }

            Dirty = false;

            return true;
        }

        /// <summary>
        /// закрыть док
        /// </summary>
        /// <returns></returns>
        public bool CloseDocument()
        {
            if (!this.dirty)
                return true;

            DialogResult res = MessageBox.Show(frmOwner, "Save changes?", Application.ProductName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

            switch (res)
            {
                case DialogResult.Yes: return SaveDocument(SaveType.Save);
                case DialogResult.No: return true;
                case DialogResult.Cancel: return false;
                default: Debug.Assert(false); return false;
            }
        }

        /// <summary>
        /// открыть док
        /// </summary>
        /// <param name="newFileName">
        /// имя документа. пусто-функция показывает диалог открытия файла.
        /// </param>
        /// <returns></returns>
        public bool OpenDocument(string newFileName)
        {
            // проверка: можно закрыть текущий файл?
            if (!CloseDocument())
                return false;

            // берем файл чтоб открыть
            if (Empty(newFileName))
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = fileDlgFilter;
                openFileDialog1.InitialDirectory = fileDlgInitDir;

                DialogResult res = openFileDialog1.ShowDialog(frmOwner);

                if (res != DialogResult.OK)
                    return false;

                newFileName = openFileDialog1.FileName;
                fileDlgInitDir = new FileInfo(newFileName).DirectoryName;
            }

            // считываем данные
            try
            {
                using (Stream stream = new FileStream(
                           newFileName, FileMode.Open, FileAccess.Read))
                {
                    // Deserialize object from text format
                    IFormatter formatter = new BinaryFormatter();

                    if (LoadEvent != null)        // if caller subscribed to this event
                    {
                        SerializationEventArgs args = new SerializationEventArgs(
                            formatter, stream, newFileName);

                        // raise event to load document from file
                        LoadEvent(this, args);

                        if (args.Error)
                        {
                            // report failure
                            if (OpenEvent != null)
                            {
                                OpenEvent(this,
                                    new OpenFileEventArgs(newFileName, false));
                            }

                            return false;
                        }

                        // raise event to show document in the window
                        if (DocChangedEvent != null)
                        {
                            DocChangedEvent(this, new EventArgs());
                        }
                    }
                }
            }
            // ловим все возможные исключения.
            // invoked by LoadEvent and DocChangedEvent.
            catch (ArgumentNullException ex) { return HandleOpenException(ex, newFileName); }
            catch (ArgumentOutOfRangeException ex) { return HandleOpenException(ex, newFileName); }
            catch (ArgumentException ex) { return HandleOpenException(ex, newFileName); }
            catch (SecurityException ex) { return HandleOpenException(ex, newFileName); }
            catch (FileNotFoundException ex) { return HandleOpenException(ex, newFileName); }
            catch (DirectoryNotFoundException ex) { return HandleOpenException(ex, newFileName); }
            catch (PathTooLongException ex) { return HandleOpenException(ex, newFileName); }
            catch (IOException ex) { return HandleOpenException(ex, newFileName); }

            // чистим все грязное 
            Dirty = false;
            SetFileName(newFileName);

            if (OpenEvent != null)
            {
                // успех?
                OpenEvent(this, new OpenFileEventArgs(newFileName, true));
            }

            // успех.
            return true;
        }

        /// <summary>
        /// сохранение файла.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool SaveDocument(SaveType type)
        {
            string newFileName = this.fileName;

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = fileDlgFilter;

            if ((type == SaveType.SaveAs) ||
                Empty(newFileName))
            {

                if (!Empty(newFileName))
                {
                    saveFileDialog1.InitialDirectory = Path.GetDirectoryName(newFileName);
                    saveFileDialog1.FileName = Path.GetFileName(newFileName);
                }
                else
                {
                    saveFileDialog1.InitialDirectory = fileDlgInitDir;
                    saveFileDialog1.FileName = newDocName;
                }

                DialogResult res = saveFileDialog1.ShowDialog(frmOwner);

                if (res != DialogResult.OK)
                    return false;

                newFileName = saveFileDialog1.FileName;
                fileDlgInitDir = new FileInfo(newFileName).DirectoryName;
            }

            // записываем данные 
            try
            {
                using (Stream stream = new FileStream(
                           newFileName, FileMode.Create, FileAccess.Write))
                {
                    // Serialize object to text format
                    IFormatter formatter = new BinaryFormatter();

                    if (SaveEvent != null)
                    {
                        SerializationEventArgs args = new SerializationEventArgs(
                            formatter, stream, newFileName);

                        //вызов события
                        SaveEvent(this, args);

                        if (args.Error)
                            return false;
                    }

                }
            }
            catch (ArgumentNullException ex) { return HandleSaveException(ex, newFileName); }
            catch (ArgumentOutOfRangeException ex) { return HandleSaveException(ex, newFileName); }
            catch (ArgumentException ex) { return HandleSaveException(ex, newFileName); }
            catch (SecurityException ex) { return HandleSaveException(ex, newFileName); }
            catch (FileNotFoundException ex) { return HandleSaveException(ex, newFileName); }
            catch (DirectoryNotFoundException ex) { return HandleSaveException(ex, newFileName); }
            catch (PathTooLongException ex) { return HandleSaveException(ex, newFileName); }
            catch (IOException ex) { return HandleSaveException(ex, newFileName); }

            Dirty = false;
            SetFileName(newFileName);

            return true;
        }

        /// <summary>
        /// соответсвие типа файла с теми данными что в реестре /*too hard*/
        /// </summary>
        /// <param name="data"></param>
        /// <returns>true - OK, false - failed</returns>
        public bool RegisterFileType(
            string fileExtension,
            string progId,
            string typeDisplayName)
        {
            try
            {
                string s = String.Format(CultureInfo.InvariantCulture, ".{0}", fileExtension);

                // Register custom extension with the shell
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(s))
                {
                    // Map custom  extension to a ProgID
                    key.SetValue(null, progId);
                }

                // create ProgID key with display name
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(progId))
                {
                    key.SetValue(null, typeDisplayName);
                }

                // register icon
                using (RegistryKey key =
                           Registry.ClassesRoot.CreateSubKey(progId + @"\DefaultIcon"))
                {
                    key.SetValue(null, Application.ExecutablePath + ",0");
                }

                // Register open command with the shell
                string cmdkey = progId + @"\shell\open\command";
                using (RegistryKey key =
                           Registry.ClassesRoot.CreateSubKey(cmdkey))
                {
                    // Map ProgID to an Open action for the shell
                    key.SetValue(null, Application.ExecutablePath + " \"%1\"");
                }

                // Register application for "Open With" dialog
                string appkey = "Applications\\" +
                    new FileInfo(Application.ExecutablePath).Name +
                    "\\shell";
                using (RegistryKey key =
                           Registry.ClassesRoot.CreateSubKey(appkey))
                {
                    key.SetValue("FriendlyCache", Application.ProductName);
                }
            }
            catch (ArgumentNullException ex)
            {
                return HandleRegistryException(ex);
            }
            catch (SecurityException ex)
            {
                return HandleRegistryException(ex);
            }
            catch (ArgumentException ex)
            {
                return HandleRegistryException(ex);
            }
            catch (ObjectDisposedException ex)
            {
                return HandleRegistryException(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                return HandleRegistryException(ex);
            }

            return true;
        }

        ///// <summary>
        ///// </summary>
        ///// <param name="ex"></param>
        private bool HandleRegistryException(Exception ex)
        {
            Trace.WriteLine("Registry operation failed: " + ex.Message);
            return false;
        }

        /// <summary>
        /// Set file name and change owner's caption
        /// </summary>
        /// <param name="fileName"></param>
        private void SetFileName(string fileName)
        {
            this.fileName = fileName;
        }

        /// <summary>
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="fileName"></param>
        private bool HandleOpenException(Exception ex, string fileName)
        {
            MessageBox.Show(frmOwner,
                "Open File operation failed. File name: " + fileName + "\n" +
                "Reason: " + ex.Message,
                Application.ProductName);

            if (OpenEvent != null)
            {
                // report failure
                OpenEvent(this, new OpenFileEventArgs(fileName, false));
            }

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="fileName"></param>
        private bool HandleSaveException(Exception ex, string fileName)
        {
            MessageBox.Show(frmOwner,
                "Save File operation failed. File name: " + fileName + "\n" +
                "Reason: " + ex.Message,
                Application.ProductName);

            return false;
        }


        /// <summary>
        /// пустая ли строка?
        /// </summary>
        /// <param name="s"></param>
        static bool Empty(string s)
        {
            return s == null || s.Length == 0;
        }
    }

    public delegate void SaveEventHandler(object sender, SerializationEventArgs e);
    public delegate void LoadEventHandler(object sender, SerializationEventArgs e);
    public delegate void OpenFileEventHandler(object sender, OpenFileEventArgs e);

    /// <summary>
    /// Serialization.
    /// Используется в событиях, вызванных из класса DocManager.
    /// Class содержит информацию, необходимую для загрузки / сохранения файла
    /// </summary>
    public class SerializationEventArgs : System.EventArgs
    {
        private IFormatter formatter;
        private Stream stream;
        private string fileName;
        private bool errorFlag;

        public SerializationEventArgs(IFormatter formatter, Stream stream,
            string fileName)
        {
            this.formatter = formatter;
            this.stream = stream;
            this.fileName = fileName;
            errorFlag = false;
        }

        public bool Error
        {
            get
            {
                return errorFlag;
            }
            set
            {
                errorFlag = value;
            }
        }

        public IFormatter Formatter
        {
            get
            {
                return formatter;
            }
        }

        public Stream SerializationStream
        {
            get
            {
                return stream;
            }
        }
        public string FileName
        {
            get
            {
                return fileName;
            }
        }
    }

    /// <summary>
    /// Аргументы события открытия файла.
    /// Используется в событиях, вызванных из класса DocManager.
    /// Class содержит имя файла и результат открытой операции.
    /// </summary>
    public class OpenFileEventArgs : System.EventArgs
    {
        private string fileName;
        private bool success;

        public OpenFileEventArgs(string fileName, bool success)
        {
            this.fileName = fileName;
            this.success = success;
        }

        public string FileName
        {
            get
            {
                return fileName;
            }
        }

        public bool Succeeded
        {
            get
            {
                return success;
            }
        }
    }

    /// <summary>
    ///класс, используемый для инициализации класса DocManager
    /// </summary>
    public class DocManagerData
    {
        public DocManagerData()
        {
            frmOwner = null;
            fileDlgFilter = "All Files (*.*)|*.*";
        }

        private Form frmOwner;
        private string fileDlgFilter;

        public Form FormOwner
        {
            get
            {
                return frmOwner;
            }
            set
            {
                frmOwner = value;
            }
        }

        public string FileDialogFilter
        {
            get
            {
                return fileDlgFilter;
            }
            set
            {
                fileDlgFilter = value;
            }
        }
    };
}
