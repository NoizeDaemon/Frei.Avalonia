using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Frei.Avalonia.Controls;

namespace Demo.ViewModels;

public class TimerBarDemoViewModel : ViewModelBase
{
    [Reactive] public string Message { get; set; }

    public TimerBarDemoViewModel()
    {
        Message = "Has not completed yet.";
    }

    public void UpdateMessage(object? sender, TimeBar.TimeBarCompletedEventArgs e)
    {
        var direction = e.IsCountingUp ? "max" : "min";
        Message = $"TimeBar reached {direction}, {e.RemainingIterations.Value} iterations remain.";
    }
}
