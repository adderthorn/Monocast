using System;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using System.Threading;
using System.Diagnostics;
using Windows.Storage.Streams;

namespace Monocast
{
    public enum FolderLocation
    {
        None,
        Local,
        Roaming
    }

    public class AppData
    {
        #region Public Properties
        public string FileName { get; set; }
        public string FullPath { get => Path.Combine(GetStorageFolder().Path, FileName); }
        public FolderLocation FolderLocation { get; set; }
        public StorageFolder FileStorageFolder { get => GetStorageFolder(); }
        #endregion

        #region Private Variables
        private StorageFolder _FolderPath;
        private StorageFile _File;
        #endregion

        #region Class Initialization
        public AppData(string FileName, FolderLocation FolderLocation)
        {
            this.FileName = FileName;
            this.FolderLocation = FolderLocation;
            if (FolderLocation == FolderLocation.None)
            {
                GetFileWithFolderAsync(FileName);
            }
        }
        #endregion

        #region Public Methods
        public async Task SerializeToFileAsync<T>(T ObjectToWrite, CreationCollisionOption CollisionOption)
        {
            XmlWriterSettings writerSettings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "    ",
                NewLineHandling = NewLineHandling.Replace,
                NewLineChars = "\r\n",
                WriteEndDocumentOnClose = true
            };

            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            var getStreamResult = await CreateStreamFromFileAsync(CollisionOption);
            IRandomAccessStream stream = getStreamResult.Stream;
            using (var writer = XmlWriter.Create(stream.AsStream(), writerSettings))
            {
                serializer.WriteObject(writer, ObjectToWrite);
            }
            await stream.FlushAsync();
            stream.Dispose();
        }

        public async Task<T> DeserializeFromFileAsync<T>()
        {
            object obj;
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            var getStreamResult = await GetStreamFromFileAsync(CreationCollisionOption.OpenIfExists);
            using (IRandomAccessStream stream = getStreamResult.Stream)
            {
                using (var reader = XmlReader.Create(stream.AsStream()))
                {
                    obj = (T)serializer.ReadObject(reader);
                }
            }
            return (T)obj;
        }

        public async Task<string> SaveToFileAsync(byte[] bytes, CreationCollisionOption collisionOption,
            Action<uint> ProgressCallbackFunction)
        {
            bool isComplete = false;
            Progress<uint> progressCallback = new Progress<uint>(ProgressCallbackFunction);
            var getStreamResult = await CreateStreamFromFileAsync(collisionOption);
            var stream = getStreamResult.Stream;
            var tokenSource = new CancellationTokenSource();
            uint value = await stream.WriteAsync(bytes.AsBuffer()).AsTask(tokenSource.Token, progressCallback);

            isComplete = await stream.FlushAsync();

            return getStreamResult.File;
        }

        public async Task<string> SaveToFileAsync(byte[] bytes, CreationCollisionOption collisionOption)
        {
            bool isComplete = false;
            var getStreamResult = await CreateStreamFromFileAsync(collisionOption);
            var stream = getStreamResult.Stream;
            await stream.WriteAsync(bytes.AsBuffer());
            isComplete = await stream.FlushAsync();
            return getStreamResult.File;
        }

        public async Task<MemoryStream> LoadFromFileAsync()
        {
            var getStreamResult = await GetStreamFromFileAsync(CreationCollisionOption.OpenIfExists);
            var stream = getStreamResult.Stream;
            MemoryStream resultStream = new MemoryStream();
            stream.AsStreamForRead().CopyTo(resultStream);
            return resultStream;
        }

        public bool CheckFileExists()
        {
            StorageFolder folder = GetStorageFolder();
            if (folder == null || FileName == null) return false;
            return File.Exists(FullPath);            
        }

        public async Task<StorageFile> GetStorageFileAsync()
        {
            if (_File == null)
                _File = await StorageFile.GetFileFromPathAsync(FullPath);
            return _File;
        }

        public async Task DeleteStorageFileAsync()
        {
            if (!CheckFileExists())
                return;
            if (_File == null)
                _File = await StorageFile.GetFileFromPathAsync(FullPath);
            await _File.DeleteAsync();
        }
        #endregion

        #region Private Methods
        private async Task<StreamWithFileName> CreateStreamFromFileAsync(
            CreationCollisionOption collisionOption)
        {
            StorageFile subFile = await GetStorageFolder().CreateFileAsync(FileName, collisionOption);
            var stream = await subFile.OpenAsync(FileAccessMode.ReadWrite);
            var streamAndFile = new StreamWithFileName(stream, subFile.Path);
            return streamAndFile;
        }

        private async Task<StreamWithFileName> GetStreamFromFileAsync(
            CreationCollisionOption collisionOption)
        {
            StorageFile subFile = await GetStorageFolder().GetFileAsync(FileName);
            var stream = await subFile.OpenAsync(FileAccessMode.Read);
            var streamAndFile = new StreamWithFileName(stream, subFile.Path);
            return streamAndFile;
        }

        private StorageFolder GetStorageFolder()
        {
            switch (this.FolderLocation)
            {
                case FolderLocation.Local:
                    return ApplicationData.Current.LocalFolder;
                case FolderLocation.Roaming:
                    return ApplicationData.Current.RoamingFolder;
                default:
                    return _FolderPath;
            }
        }

        private async void GetFileWithFolderAsync(string fileName)
        {
            if (fileName == null) return;
            FileInfo info = new FileInfo(fileName);
            _FolderPath = await StorageFolder.GetFolderFromPathAsync(info.Directory.FullName);
            FileName = info.Name;
        }
        #endregion
    }
}
