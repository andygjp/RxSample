namespace RxSample
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    // See http://rxwiki.wikidot.com/start and http://www.introtorx.com/ for helpful tips
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            var asyncMethod = AsyncMethod.OnScheduler;
            //asyncMethod = AsyncMethod.NoAwait;
            //asyncMethod = AsyncMethod.OnWorkerThread;
            //asyncMethod = AsyncMethod.Synchronous;

            DataContext = new MainWindowViewModel(asyncMethod);
        }

        public new MainWindowViewModel DataContext
        {
            get { return (MainWindowViewModel) base.DataContext; }
            set { base.DataContext = value; }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            DataContext.GetNames();
        }
    }

    public enum AsyncMethod
    {
        Synchronous,
        NoAwait,
        OnWorkerThread,
        OnScheduler
    }

    public interface IMainWindowViewModel
    {
        ObservableCollection<string> Names { get; }
    }

    public class DesignTimeMainWindowViewModel : IMainWindowViewModel
    {
        public ObservableCollection<string> Names { get; } = new ObservableCollection<string> {"Hello", "World"};
    }

    public class MainWindowViewModel : IMainWindowViewModel
    {
        private readonly INameService _service;
        private readonly IScheduler _scheduler;
        private readonly AsyncMethod _asyncMethod;

        public MainWindowViewModel(AsyncMethod asyncMethod)
            : this(new NameService(), DispatcherScheduler.Current, asyncMethod)
        { }

        public MainWindowViewModel(INameService service, IScheduler scheduler, AsyncMethod asyncMethod = AsyncMethod.OnScheduler)
        {
            _service = service;
            _scheduler = scheduler;
            _asyncMethod = asyncMethod;
        }

        public ObservableCollection<string> Names { get; } = new ObservableCollection<string>();

        public void GetNames()
        {
            switch (_asyncMethod)
            {
                case AsyncMethod.OnScheduler:
                    GetNamesAsynchronouslyAndObserveOnScheduler();
                    break;
                case AsyncMethod.OnWorkerThread:
                    GetNamesAsynchronouslyAndObserveOnWorkerScheduler();
                    break;
                case AsyncMethod.NoAwait:
                    GetNamesAsync();
                    break;
                case AsyncMethod.Synchronous:
                    GetNamesSynchronously();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region How not to do it

        private void GetNamesSynchronously()
        {
            var names = _service.GetNames().Result;
            Names.AddRange(names);
        }

        private async Task GetNamesAsync()
        {
            var nameService = _service as NameService;

            IEnumerable<string> names = new[] {$"Expected service to be {nameof(NameService)} - it was not."};
            if (nameService != null)
            {
                // Not awaiting the result updates the UI, but exceptions are not handled.
                names = await nameService.GetNamesResultInError();
            }
            Names.AddRange(names);
        }

        private void GetNamesAsynchronouslyAndObserveOnWorkerScheduler()
        {
            // This subscribes to the service, but when it processes the data it'll be on the wrong thread
            Observable.FromAsync(() => _service.GetNames()).Subscribe(Names.AddRange);
        }

        #endregion

        private void GetNamesAsynchronouslyAndObserveOnScheduler()
        {
            Observable.FromAsync(() => _service.GetNames()).ObserveOn(_scheduler).Subscribe(Names.AddRange);
        }
    }

    public interface INameService
    {
        Task<IEnumerable<string>> GetNames();
    }

    public class NameService : INameService
    {
        public async Task<IEnumerable<string>> GetNamesResultInError()
        {
            await ((INameService) this).GetNames();
            throw new Exception("BOOM!");
        }

        async Task<IEnumerable<string>> INameService.GetNames()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            return new[] {"Hello", "World"};
        }
    }

    public static class ObservableCollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }
    }
}
