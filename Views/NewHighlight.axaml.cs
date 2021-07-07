using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System;
using VCS.ViewModels;

namespace VCS.Views
{
    public partial class NewHighlight : ReactiveWindow<NewHighlightViewModel>
    {
        public NewHighlight()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.WhenActivated(d => d(ViewModel.CloseCommand.Subscribe((val) => Close(val)))) ;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
