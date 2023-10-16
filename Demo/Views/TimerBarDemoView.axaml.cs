using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Demo.ViewModels;
using System;

namespace Demo.Views;

public partial class TimerBarDemoView : UserControl
{
    public TimerBarDemoView()
    {
        InitializeComponent();

        AttachedToVisualTree += TimerBarDemoView_AttachedToVisualTree;
    }

    private void TimerBarDemoView_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not TimerBarDemoViewModel viewModel) return; //throw new NullReferenceException();

        TimeBar1.TimerCompleted += viewModel.UpdateMessage;
    }
}