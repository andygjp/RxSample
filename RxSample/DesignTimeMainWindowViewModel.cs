namespace RxSample
{
    using System.Collections.ObjectModel;

    public class DesignTimeMainWindowViewModel : IMainWindowViewModel
    {
        public ObservableCollection<string> Names { get; } = new ObservableCollection<string> {"Hello", "World"};

        public void Initialize()
        { }
    }
}
