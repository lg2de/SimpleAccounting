using System.Windows.Input;

namespace lg2de.SimpleAccounting
{
    public class MenuViewModel
    {
        private readonly IProjectLoader projectLoader;

        public MenuViewModel(IProjectLoader projectLoader, string fileName)
        {
            this.projectLoader = projectLoader;
            this.Header = fileName;
        }

        public string Header { get; }

        public ICommand Command => new RelayCommand(_ => this.projectLoader.LoadProject(this.Header));
    }
}
