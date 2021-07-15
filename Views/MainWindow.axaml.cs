using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;
using VCS.ViewModels;

namespace VCS.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public static MainWindow? Instance;
        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.WhenActivated(async d => {
                d(ViewModel.ShowNewHighlightDialog.RegisterHandler(DoShowDialogAsync));
                await ViewModel.Load();
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task DoShowDialogAsync(InteractionContext<Unit, float?> interaction)
        {
            var vm = new NewHighlightViewModel();
            var window = new NewHighlight();
            window.DataContext = vm;
            var result = await window.ShowDialog<float?>(this);
            interaction.SetOutput(result);
        }

        public void OnFocusLost(object sender, RoutedEventArgs e)
        {
            ViewModel?.OnFocusLost(sender, e);
        }
    }
}
