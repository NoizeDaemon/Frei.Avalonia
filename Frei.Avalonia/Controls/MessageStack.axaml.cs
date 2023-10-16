using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using System.Collections.Generic;
using System;
using System.Linq;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using System.Diagnostics;
using Avalonia.Markup.Xaml;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using Avalonia.Animation;

namespace Frei.Avalonia.Controls;

public class MessageStack : ContentControl
{
    private StackPanel? _stack;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _stack = e.NameScope.Find<StackPanel>("PART_Stack");
        base.OnApplyTemplate(e);
    }


    public static readonly StyledProperty<IBrush> OverlayBackgroundProperty =
    AvaloniaProperty.Register<MessageStack, IBrush>("OverlayBackground", Brushes.Black);

    public IBrush OverlayBackground
    {
        get => GetValue(OverlayBackgroundProperty);
        set => SetValue(OverlayBackgroundProperty, value);
    }


    public static readonly StyledProperty<double> OverlayBackgroundOpacityProperty =
    AvaloniaProperty.Register<MessageStack, double>("OverlayBackgroundOpacity", 0.5d, defaultBindingMode: BindingMode.OneWay);

    public double OverlayBackgroundOpacity
    {
        get => GetValue(OverlayBackgroundOpacityProperty);
        set => SetValue(OverlayBackgroundOpacityProperty, value);
    }


    public static readonly StyledProperty<bool> IsHistoryOpenProperty =
    AvaloniaProperty.Register<MessageStack, bool>("IsHistoryOpen");

    public bool IsHistoryOpen
    {
        get => GetValue(IsHistoryOpenProperty);
        set => SetValue(IsHistoryOpenProperty, value);
    }


    public static readonly StyledProperty<bool> ShowProgressBarProperty = 
    AvaloniaProperty.Register<MessageStack, bool>("ShowProgressBar", true);

    public bool ShowProgressBar
    {
        get => GetValue(ShowProgressBarProperty);
        set => SetValue(ShowProgressBarProperty, value);
    }


    public static readonly StyledProperty<Dock> ProgressBarPositionProperty =
    AvaloniaProperty.Register<MessageStack, Dock>("ShowProgressBar", Dock.Right);

    public Dock ProgressBarPosition
    {
        get => GetValue(ProgressBarPositionProperty);
        set => SetValue(ProgressBarPositionProperty, value);
    }

    public Orientation ProgressBarOrientation
    {
        get => ProgressBarPosition switch { Dock.Left or Dock.Right => Orientation.Vertical, _ => Orientation.Horizontal };
    }

    public static readonly StyledProperty<TimeSpan> DefaultDisplayTimeProperty =
    AvaloniaProperty.Register<MessageStack, TimeSpan>("DefaultDisplayTime", TimeSpan.FromSeconds(5));

    public TimeSpan DefaultDisplayTime
    {
        get => GetValue(DefaultDisplayTimeProperty);
        set => SetValue(DefaultDisplayTimeProperty, value);
    }


    public static readonly StyledProperty<HorizontalAlignment> HorizontalStackAlignmentProperty =
    AvaloniaProperty.Register<MessageStack, HorizontalAlignment>("HorizontalStackAlignment", HorizontalAlignment.Right);

    public HorizontalAlignment HorizontalStackAlignment
    {
        get => GetValue(HorizontalStackAlignmentProperty);
        set => SetValue(HorizontalStackAlignmentProperty, value);
    }


    public static readonly StyledProperty<VerticalAlignment> VerticalStackAlignmentProperty =
    AvaloniaProperty.Register<MessageStack, VerticalAlignment>("VerticalStackAlignment", VerticalAlignment.Bottom);

    public VerticalAlignment VerticalStackAlignment
    {
        get => GetValue(VerticalStackAlignmentProperty);
        set => SetValue(VerticalStackAlignmentProperty, value);
    }

    public static readonly StyledProperty<double> StackSpacingProperty =
    AvaloniaProperty.Register<MessageStack, double>("StackSpacing", 10.0d, defaultBindingMode: BindingMode.OneWay);

    public double StackSpacing
    {
        get => GetValue(StackSpacingProperty);
        set => SetValue(StackSpacingProperty, value);
    }


    public static readonly StyledProperty<bool> IsMessageInsertedFirstProperty =
    AvaloniaProperty.Register<MessageStack, bool>("IsMessageInsertedFirst");

    public bool IsMessageInsertedFirst
    {
        get => GetValue(IsMessageInsertedFirstProperty);
        set => SetValue(IsMessageInsertedFirstProperty, value);
    }


    public static readonly StyledProperty<DataTemplates> MessageTemplatesProperty =
    AvaloniaProperty.Register<MessageStack, DataTemplates>("MessageTemplates");

    public DataTemplates MessageTemplates
    {
        get => GetValue(MessageTemplatesProperty);
        set => SetValue(MessageTemplatesProperty, value);
    }


    public static readonly StyledProperty<IControlTemplate> MessageWrapperTemplateProperty =
    AvaloniaProperty.Register<MessageStack, IControlTemplate>("MessageWrapperTemplate");

    public IControlTemplate MessageWrapperTemplate
    {
        get => GetValue(MessageWrapperTemplateProperty);
        set => SetValue(MessageWrapperTemplateProperty, value);
    }

    public static readonly StyledProperty<IControlTemplate> MessageLayoutTemplateProperty =
    AvaloniaProperty.Register<MessageStack, IControlTemplate>("MessageLayoutTemplate");

    public IControlTemplate MessageLayoutTemplate
    {
        get => GetValue(MessageLayoutTemplateProperty);
        set => SetValue(MessageLayoutTemplateProperty, value);
    }

    // *** INSTANCE HANDLING ***

    public static readonly StyledProperty<string?> IdentifierProperty =
    AvaloniaProperty.Register<MessageStack, string?>("Identifier");

    public string? Identifier
    {
        get => GetValue(IdentifierProperty);
        set => SetValue(IdentifierProperty, value);
    }

    private static readonly HashSet<MessageStack> LoadedInstances = new HashSet<MessageStack>();

    private static MessageStack GetInstance(string? MessageStackIdentifier)
    {
        if (LoadedInstances.Count == 0)
        {
            throw new InvalidOperationException("No loaded MessageStack instances.");
        }

        List<MessageStack> list = LoadedInstances.Where((MessageStack ps) => MessageStackIdentifier == null || object.Equals(ps.Identifier, MessageStackIdentifier)).ToList();
        if (list.Count == 0)
        {
            throw new InvalidOperationException($"No loaded MessageStack have an Identifier property matching MessageStackIdentifier {MessageStackIdentifier} argument.");
        }

        if (list.Count > 1)
        {
            throw new InvalidOperationException("Multiple viable MessageStacks. Specify a unique Identifier on each MessageStack, especially where multiple Windows are a concerned.");
        }

        return list[0];
    }


    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LoadedInstances.Add(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        LoadedInstances.Remove(this);
    }


    // *** ADDING MESSAGES ***

    public static void AddNew(object? content, string? messageStackIdentifier = null, bool useWrapper = true)
    {
        GetInstance(messageStackIdentifier).AddNewInternal(content, useWrapper);
    }

    private void AddNewInternal(object? content, bool useWrapper)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (content is Control view)
        {
            Attach(view, useWrapper);
            return;
        }

        //if (content is ViewModelBase viewModel)
        //{
        //    var contentPresenter = new ContentControl();
        //    contentPresenter.Content = viewModel;
        //    Attach(contentPresenter, useWrapper);
        //    return;
        //}

        var matchingDataTemplates = MessageTemplates.Where(dt => dt.Match(content)).ToList();
        if (matchingDataTemplates.Count == 1)
        {
            var modelView = matchingDataTemplates[0].Build(content);
            modelView.DataContext = content;
            Attach(modelView, useWrapper);
            return;
        }
        if (matchingDataTemplates.Count > 1)
        {
            throw new ArgumentException($"Found no distinct match for {nameof(content)} in DataTemplates.");
        }

        throw new ArgumentException($"{nameof(content)} is neither Control nor ViewModelBase.");
    }

    private void Attach(Control newMessage, bool useWrapper)
    {
        var layoutedMessage = new ContentControl()
        {
            Template = MessageLayoutTemplate,
            Name = "PART_MessageLayout",
            Content = newMessage,
            ClipToBounds = false
        };

        Control finalAttachment = new ContentControl()
        {
            Template = (useWrapper) ? MessageWrapperTemplate : null,
            Name = "Message",
            Content = layoutedMessage,
            ClipToBounds = false
        };

        finalAttachment.Loaded += OnMessageLoaded;

        if (IsMessageInsertedFirst) _stack.Children.Insert(0, finalAttachment);
        else _stack.Children.Add(finalAttachment);
    }

    private void OnMessageLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control message) return;

        e.Handled = true;
        message.FindDescendantOfType<ProgressBar>()!.ValueChanged += OnMessageTimeChanged;
    }

    private void OnMessageTimeChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not ProgressBar progressBar) return;

        if (e.NewValue <= 0)
        {
            e.Handled = true;
            progressBar.ValueChanged -= OnMessageTimeChanged;

            var message = _stack.Children.Single(c => c.IsVisualAncestorOf(progressBar));
            _stack.Children.Remove(message);
        }
    }
}