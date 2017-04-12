using System;
using System.IO;
using System.Threading.Tasks;

namespace WebCamPhotoService
{
    public interface IPhotoService : IDisposable
    {
        bool IsDisposed { get; }
        bool IsFailed { get; }
        bool IsInitialized { get; }
        bool IsPreviewing { get; }
        int PhotoWidth { get; }
        int PhotoHeight { get; }
        event EventHandler<PhotoServiceStateChangedEventArg> StateChanged;

        void BeginCleanup();
        void BeginInitialize();
        Task CleanupAsync();
        void EndCleanup();
        void EndInitialize();
        Task InitializeAsync();
        Task<MemoryStream> GetPhotoStreamAsync();
    }
}