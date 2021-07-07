using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using VCS.Utils;

namespace VCS.ViewModels
{
    public class NewHighlightViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

        public ReactiveCommand<Unit, float?> CloseCommand { get; }

        string timestamp = "";
        public string Timestamp
        {
            get => timestamp;
            set
            {
                this.RaiseAndSetIfChanged(ref timestamp, value);
                try
                {
                    TimestampUtils.TimestampToSeconds(value);
                    ValidTimestamp = true;
                }
                catch
                {
                    ValidTimestamp = false;
                }
            }
        }

        bool validTimestamp = false;
        public bool ValidTimestamp
        {
            get => validTimestamp;
            set
            {
                this.RaiseAndSetIfChanged(ref validTimestamp, value);
            }
        }

        public bool Canceled = true;

        public NewHighlightViewModel()
        {
            CloseCommand = ReactiveCommand.Create<Unit, float?>((a) =>
            {
                if (Canceled || !ValidTimestamp)
                {
                    return null;
                }

                return TimestampUtils.TimestampToSeconds(Timestamp);
            });

            CancelCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Canceled = true;
                await CloseCommand.Execute();
            });

            ConfirmCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Canceled = false;
                await CloseCommand.Execute();
            });
        }
    }
}
