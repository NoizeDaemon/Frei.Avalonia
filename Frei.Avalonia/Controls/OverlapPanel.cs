using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Frei.Avalonia.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frei.Avalonia.Controls;

public class OverlapPanel : StackPanel
{
    /// <summary>
    /// https://github.com/AvaloniaUI/Avalonia/discussions/18733#discussioncomment-12974548
    /// 
    /// Initially I didn't realise binding to the parent width may be required,
    /// but you are right and imho it makes sense and works well.
    ///
    /// An alternative solution that I thought of now is:
    /// get rid of MaxFitAllWidth property, and instead bind ScrollViewer HorizontalScrollBarVisibility
    /// depending on the display mode property
    /// 
    /// In NoOverlap use ScrollBarVisibility.Auto and in
    /// FitAll use ScrollBarVisibility.Disabled (an IValueConverter can be used for it).
    /// 
    /// Then in the panel you can use the availablePanelSize parameter in the MeasureOverride.
    /// This generally should work, but it doesn't due to a bug
    /// - changing the scrollbar visibility property doesn't invalidate measure.
    /// I've sent a PR which fixes it #18753.
    /// But in the meantime, binding to the scrollViewer width sounds good :)
    /// </summary>
    public static readonly StyledProperty<double> MaxFitAllWidthProperty;
    public double MaxFitAllWidth
    {
        get => GetValue(MaxFitAllWidthProperty);
        set => SetValue(MaxFitAllWidthProperty, value);
    }
    public static readonly StyledProperty<double> MaxFitAllHeightProperty;
    public double MaxFitAllHeight
    {
        get => GetValue(MaxFitAllHeightProperty);
        set => SetValue(MaxFitAllHeightProperty, value);
    }
    public static readonly StyledProperty<DisplayMode> DisplayModeProperty;
    public DisplayMode DisplayMode
    {
        get => GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    static OverlapPanel()
    {
        DisplayModeProperty = AvaloniaProperty.Register<OverlapPanel, DisplayMode>("DisplayMode", DisplayMode.FitAll);
        MaxFitAllWidthProperty = AvaloniaProperty.Register<OverlapPanel, double>("MaxFitAllWidth");
        MaxFitAllHeightProperty = AvaloniaProperty.Register<OverlapPanel, double>("MaxFitAllHeight");

        AffectsMeasure<OverlapPanel>(DisplayModeProperty, MaxFitAllWidthProperty, MaxFitAllHeightProperty);
        AffectsArrange<OverlapPanel>(DisplayModeProperty, MaxFitAllWidthProperty, MaxFitAllHeightProperty);
    }

    private Size _lastAvailableSize;

    protected override Size MeasureOverride(Size availablePanelSize)
    {
        double targetHeight = availablePanelSize.Height;
        double targetWidth = availablePanelSize.Width;

        int childrenCount = Children.Count;

        Size finalPanelSize;
        Size availableChildSize;

        switch (Orientation)
        {
            default:
            case Orientation.Horizontal:
                double height = double.IsInfinity(availablePanelSize.Height) ? Bounds.Height : availablePanelSize.Height;
                availableChildSize = new Size(double.PositiveInfinity, height);

                double childrenWidthSum = 0.0;
                foreach (var child in Children)
                {
                    child.Measure(availableChildSize);
                    childrenWidthSum += child.DesiredSize.Width;
                }

                if (childrenWidthSum <= 0)
                {
                    finalPanelSize = base.MeasureOverride(availablePanelSize);
                    return finalPanelSize;
                }

                if (DisplayMode == DisplayMode.NoOverlap)
                {
                    Spacing = 0;

                    availablePanelSize = new Size(childrenWidthSum, targetHeight);
                    finalPanelSize = base.MeasureOverride(availablePanelSize);
                    return finalPanelSize;
                }

                if (double.IsPositiveInfinity(targetWidth))
                {
                    if (double.IsPositive(MaxFitAllWidth) && double.IsFinite(MaxFitAllWidth))
                    {
                        targetWidth = MaxFitAllWidth;
                    }
                }

                targetWidth = double.Min(targetWidth, childrenWidthSum);
                Spacing = -double.Max(0, (childrenWidthSum - targetWidth) / (childrenCount - 1));

                finalPanelSize = new Size(targetWidth, targetHeight);

                return finalPanelSize;

            case Orientation.Vertical:
                double width = double.IsInfinity(availablePanelSize.Width) ? Bounds.Width : availablePanelSize.Width;
                availableChildSize = new Size(width, double.PositiveInfinity);

                double childrenHeightSum = 0.0;
                foreach (var child in Children)
                {
                    child.Measure(availableChildSize);
                    childrenHeightSum += child.DesiredSize.Height;
                }

                if (childrenHeightSum <= 0)
                {
                    finalPanelSize = base.MeasureOverride(availablePanelSize);
                    return finalPanelSize;
                }

                if (DisplayMode == DisplayMode.NoOverlap)
                {
                    Spacing = 0;

                    availablePanelSize = new Size(targetWidth, childrenHeightSum);
                    finalPanelSize = base.MeasureOverride(availablePanelSize);
                    return finalPanelSize;
                }

                if (double.IsPositiveInfinity(targetHeight))
                {
                    if (double.IsPositive(MaxFitAllHeight) && double.IsFinite(MaxFitAllHeight))
                    {
                        targetHeight = MaxFitAllHeight;
                    }
                }

                targetHeight = double.Min(targetHeight, childrenHeightSum);
                Spacing = -double.Max(0, (childrenHeightSum - targetHeight) / (childrenCount - 1));

                finalPanelSize = new Size(targetWidth, targetHeight);

                return finalPanelSize;
        }

        //if (childrenHaveActualSize == false)
        //{
        //    availablePanelSize = base.MeasureOverride(availablePanelSize);
        //    return availablePanelSize;
        //}


        //if (DisplayMode == DisplayMode.NoOverlap || !childrenHaveActualSize)
        //{
        //    Spacing = 0;

        //    availablePanelSize = new Size(childWidthSum, targetHeight);
        //    availablePanelSize = base.MeasureOverride(availablePanelSize);
        //    //_lastAvailableSize = availablePanelSize;

        //    return availablePanelSize;
        //}





        //double childWidthSum = Children.Sum(c => c.Bounds.Width);
        //double childHeightMax = Children.Max(c => c.Bounds.Height);


        //var actualAvailableSize = Orientation switch
        //{
        //    Orientation.Vertical => new Size(
        //       double.IsInfinity(availablePanelSize.Height) ? Bounds.Width : availablePanelSize.Width,
        //       double.PositiveInfinity)
        //};

        //double childrenLengthInOrientation = 0;

        //foreach (var child in Children)
        //{
        //    child.Measure(actualAvailableSize);
        //    childrenLengthInOrientation += Orientation switch
        //    {
        //        Orientation.Horizontal => child.DesiredSize.Width,
        //        Orientation.Vertical => child.DesiredSize.Height,
        //    };
        //}

        //if (childrenLengthInOrientation == 0)
        //{
        //    availablePanelSize = base.MeasureOverride(availablePanelSize);
        //    //_lastAvailableSize = availablePanelSize;

        //    return availablePanelSize;
        //}


        //if (DisplayMode == DisplayMode.NoOverlap)
        //{
        //    Spacing = 0;

        //    availablePanelSize = new Size(childWidthSum, targetHeight);
        //    availablePanelSize = base.MeasureOverride(availablePanelSize);
        //    //_lastAvailableSize = availablePanelSize;

        //    return availablePanelSize;
        //}

        //// If inside a ScrollView, bind MaxFitAllWidth to ScrollView.Bounds.Width:
        //if (double.IsPositiveInfinity(targetWidth))
        //{
        //    if (double.IsPositive(MaxFitAllWidth) && double.IsFinite(MaxFitAllWidth))
        //    {
        //        targetWidth = MaxFitAllWidth;
        //    }
        //}



        //targetWidth = double.Min(targetWidth, childWidthSum);
        //Spacing = -double.Max(0, (childWidthSum - targetWidth) / (childrenCount - 1));

        //availablePanelSize = new Size(targetWidth, targetHeight);


        //_lastAvailableSize = availablePanelSize;
        //return availablePanelSize;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="orientation"></param>
    /// <param name="availablePanelSize"></param>
    /// <returns></returns>
    //private double MeasureDesiredChildrenLengthInOrientation(Orientation orientation, Size availablePanelSize)
    //{
    //    var actualAvailableSize = orientation switch
    //    {
    //        Orientation.Horizontal => new Size(
    //            double.PositiveInfinity,
    //            double.IsInfinity(availablePanelSize.Height) ? Bounds.Height : availablePanelSize.Height),
    //        Orientation.Vertical => new Size(
    //           double.IsInfinity(availablePanelSize.Height) ? Bounds.Width : availablePanelSize.Width,
    //           double.PositiveInfinity)
    //    };

    //    double childrenLengthInOrientation = 0;

    //    foreach (var child in Children)
    //    {
    //        child.Measure(actualAvailableSize);
    //        childrenLengthInOrientation += orientation switch
    //        {
    //            Orientation.Horizontal => child.DesiredSize.Width,
    //            Orientation.Vertical => child.DesiredSize.Height,
    //        };
    //    }

    //    return childrenLengthInOrientation;
    //}


    //protected override Size ArrangeOverride(Size finalSize)
    //{
    //    var children = Children;
    //    bool fHorizontal = Orientation == Orientation.Horizontal;
    //    var rcChild = new Rect(finalSize);
    //    double previousChildSize = 0.0;
    //    double spacing = Spacing;

    //    //
    //    // Arrange and Position Children.
    //    //
    //    for (int i = 0, count = children.Count; i < count; ++i)
    //    {
    //        var child = children[i];

    //        if (!child.IsVisible)
    //        {
    //            continue;
    //        }

    //        if (fHorizontal)
    //        {
    //            rcChild = rcChild.WithX(rcChild.X + previousChildSize);
    //            previousChildSize = child.DesiredSize.Width;
    //            rcChild = rcChild.WithWidth(previousChildSize);
    //            rcChild = rcChild.WithHeight(Math.Max(finalSize.Height, child.DesiredSize.Height));
    //            previousChildSize += spacing;
    //        }
    //        else
    //        {
    //            rcChild = rcChild.WithY(rcChild.Y + previousChildSize);
    //            previousChildSize = child.DesiredSize.Height;
    //            rcChild = rcChild.WithHeight(previousChildSize);
    //            rcChild = rcChild.WithWidth(Math.Max(finalSize.Width, child.DesiredSize.Width));
    //            previousChildSize += spacing;
    //        }

    //        child.Arrange(rcChild);
    //    }

    //    RaiseEvent(new RoutedEventArgs(Orientation == Orientation.Horizontal ? HorizontalSnapPointsChangedEvent : VerticalSnapPointsChangedEvent));

    //    return finalSize;
    //}

}