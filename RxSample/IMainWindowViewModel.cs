namespace RxSample
{
    using System.Collections.ObjectModel;

    public interface IMainWindowViewModel
    {
        ObservableCollection<string> Names { get; }
        void Initialize();
    }
}
