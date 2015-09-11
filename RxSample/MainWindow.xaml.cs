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

            var asyncMethod = AsyncMethod.OnSchedulerHandleErrors;
            asyncMethod = AsyncMethod.OnScheduler;
            //asyncMethod = AsyncMethod.OnSchedulerNoErrorHandling;
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
        OnScheduler,
        OnSchedulerHandleErrors,
        OnSchedulerNoErrorHandling
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
                case AsyncMethod.OnSchedulerHandleErrors:
                    GetNamesAsynchronouslyAndObserveOnSchedulerWhilstHandlingErrors();
                    break;
                case AsyncMethod.OnSchedulerNoErrorHandling:
                    GetNamesAsynchronouslyAndObserveOnSchedulerWithNoErrorHandling();
                    break;
                case AsyncMethod.OnWorkerThread:
                    GetNamesAsynchronouslyAndObserveOnWorkerScheduler();
                    break;
                case AsyncMethod.NoAwait:
                    // Not awaiting the result updates the UI, but exceptions are not handled.
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
            var names = await GetNamesFromBrokenNameService();
            Names.AddRange(names);
        }

        private void GetNamesAsynchronouslyAndObserveOnWorkerScheduler()
        {
            // This subscribes to the service, but when it processes the data it'll be on the wrong thread
            Observable.FromAsync(() => _service.GetNames()).Subscribe(Names.AddRange);
        }

        private void GetNamesAsynchronouslyAndObserveOnSchedulerWithNoErrorHandling()
        {
            Observable.FromAsync(GetNamesFromBrokenNameService).ObserveOn(_scheduler).Subscribe(Names.AddRange);
        }

        #endregion

        private void GetNamesAsynchronouslyAndObserveOnScheduler()
        {
            Observable.FromAsync(() => _service.GetNames()).ObserveOn(_scheduler).Subscribe(Names.AddRange);
        }

        private void GetNamesAsynchronouslyAndObserveOnSchedulerWhilstHandlingErrors()
        {
            Observable.FromAsync(GetNamesFromBrokenNameService).ObserveOn(_scheduler).Subscribe(Names.AddRange, ex =>
            {
                Names.Add(ex.ToString());
            });
        }

        private async Task<IEnumerable<string>> GetNamesFromBrokenNameService()
        {
            var nameService = GetBrokenNameService();
            var names = await nameService.GetNames();
            return names;
        }

        private INameService GetBrokenNameService()
        {
            INameService nameService = _service as BrokenNameService ?? new BrokenNameService();
            return nameService;
        }
    }

    public interface INameService
    {
        Task<IEnumerable<string>> GetNames();
    }

    public class NameService : INameService
    {
        public async Task<IEnumerable<string>> GetNames()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            return new[] {"Hello", "World"};
        }
    }

    public class BrokenNameService : NameService, INameService
    {
        async Task<IEnumerable<string>> INameService.GetNames()
        {
            await base.GetNames();
            throw new Exception("BOOM!");
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
