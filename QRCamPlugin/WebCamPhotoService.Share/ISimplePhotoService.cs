using System;
using System.Threading.Tasks;

namespace WebCamPhotoService
{
    public interface ISimplePhotoService:IDisposable
    {
        bool IsDisposed { get; }
        bool IsFailed { get; }
        bool IsInitialized { get; }
        bool IsPreviewing { get; }

        event EventHandler<PhotoServiceStateChangedEventArg> StateChanged;

        void BeginCleanup();
        void BeginInitialize();
        Task CleanupAsync();
        void EndCleanup();
        void EndInitialize();
        Task InitializeAsync();
    }
}