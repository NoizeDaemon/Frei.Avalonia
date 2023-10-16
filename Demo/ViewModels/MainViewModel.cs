using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;

namespace Demo.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ViewModelBase CurrentPage { get; set; }
    public ReactiveCommand<string, Unit> ChangePageCommand { get; set; }

    private readonly Dictionary<string, ViewModelBase> Pages;

    public MainViewModel()
    {
        Pages = new Dictionary<string, ViewModelBase>()
        {
            { "TimerBarDemo", new TimerBarDemoViewModel() }
        };

        CurrentPage = Pages["TimerBarDemo"];
        ChangePageCommand = ReactiveCommand.Create<string>(ChangePage);
    }

    private void ChangePage(string pageName)
    {
        CurrentPage = Pages[pageName];
    }
}
