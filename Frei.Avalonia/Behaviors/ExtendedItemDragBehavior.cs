using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Transformation;
using Avalonia.Xaml.Interactivity;
using Frei.Avalonia.Controls;
using System;
using System.Collections;
using System.Diagnostics;

namespace Frei.Avalonia.Behaviors;

/// <summary>
/// 
/// </summary>
public class ExtendedItemDragBehavior : Behavior<Control>
{
    private bool _enableDrag;
    private bool _dragStarted;
    private Point _start;
    private int _draggedIndex;
    private int _targetIndex;
    private ItemsControl? _itemsControl;
    private double _spacing;
    private Control? _draggedContainer;
    private bool _captured;
    private double _lastDelta;
    private Matrix _zeroMatrix = new();


    /// <summary>
    /// 
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ExtendedItemDragBehavior, Orientation>(nameof(Orientation));

    /// <summary>
    /// 
    /// </summary>
    public static readonly StyledProperty<double> HorizontalDragThresholdProperty =
        AvaloniaProperty.Register<ExtendedItemDragBehavior, double>(nameof(HorizontalDragThreshold), 3);

    /// <summary>
    /// 
    /// </summary>
    public static readonly StyledProperty<double> VerticalDragThresholdProperty =
        AvaloniaProperty.Register<ExtendedItemDragBehavior, double>(nameof(VerticalDragThreshold), 3);

    /// <summary>
    /// 
    /// </summary>
    public static readonly StyledProperty<bool> OverrideZIndexProperty =
        AvaloniaProperty.Register<ExtendedItemDragBehavior, bool>(nameof(OverrideZIndexProperty), false);

    /// <summary>
    /// 
    /// </summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>
    /// 
    /// </summary>
    public double HorizontalDragThreshold
    {
        get => GetValue(HorizontalDragThresholdProperty);
        set => SetValue(HorizontalDragThresholdProperty, value);
    }

    /// <summary>
    /// 
    /// </summary>
    public double VerticalDragThreshold
    {
        get => GetValue(VerticalDragThresholdProperty);
        set => SetValue(VerticalDragThresholdProperty, value);
    }

    /// <summary>
    /// 
    /// </summary>
    public bool OverrideZIndex
    {
        get => GetValue(OverrideZIndexProperty);
        set => SetValue(OverrideZIndexProperty, value);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, PointerReleased, RoutingStrategies.Tunnel);
            AssociatedObject.AddHandler(InputElement.PointerPressedEvent, PointerPressed, RoutingStrategies.Tunnel);
            AssociatedObject.AddHandler(InputElement.PointerMovedEvent, PointerMoved, RoutingStrategies.Tunnel);
            AssociatedObject.AddHandler(InputElement.PointerCaptureLostEvent, PointerCaptureLost, RoutingStrategies.Tunnel);
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, PointerReleased);
            AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, PointerPressed);
            AssociatedObject.RemoveHandler(InputElement.PointerMovedEvent, PointerMoved);
            AssociatedObject.RemoveHandler(InputElement.PointerCaptureLostEvent, PointerCaptureLost);
        }
    }

    private void PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(AssociatedObject).Properties;
        if (properties.IsLeftButtonPressed
            && AssociatedObject?.Parent is ItemsControl itemsControl)
        {
            _enableDrag = true;
            _dragStarted = false;
            _start = e.GetPosition(itemsControl);
            _draggedIndex = -1;
            _targetIndex = -1;
            _itemsControl = itemsControl;
            _draggedContainer = AssociatedObject;

            if (_itemsControl.ItemsPanelRoot is StackPanel stackPanel && stackPanel.Orientation == Orientation)
            {
                _spacing = stackPanel.Spacing;
            }

            if (_draggedContainer is not null)
            {
                SetDraggingPseudoClasses(_draggedContainer, true);
            }

            AddTransforms(_itemsControl);

            if (OverrideZIndex)
            {
                SetZIndexesBasedOnItemsIndex(_itemsControl);
            }


            _captured = true;
        }
    }

    private void PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_captured)
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                Released();
            }

            _captured = false;
        }
    }

    private void PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        Released();
        _captured = false;
    }

    private void Released()
    {
        if (!_enableDrag)
        {
            return;
        }

        RemoveTransforms(_itemsControl);

        if (_itemsControl is not null)
        {
            foreach (var control in _itemsControl.GetRealizedContainers())
            {
                SetDraggingPseudoClasses(control, true);
            }
        }

        if (_dragStarted)
        {
            if (_draggedIndex >= 0 && _targetIndex >= 0 && _draggedIndex != _targetIndex)
            {
                MoveDraggedItem(_itemsControl, _draggedIndex, _targetIndex);
            }
        }

        if (_itemsControl is not null)
        {
            foreach (var control in _itemsControl.GetRealizedContainers())
            {
                SetDraggingPseudoClasses(control, false);

                if (OverrideZIndex)
                {
                    //Debug.WriteLine("Resetting ZIndex for " + control.DataContext);
                    control.ZIndex = 0;
                }
            }

        }

        if (_draggedContainer is not null)
        {
            SetDraggingPseudoClasses(_draggedContainer, false);
            _draggedContainer.ZIndex = 0;
        }

        _draggedIndex = -1;
        _targetIndex = -1;
        _enableDrag = false;
        _dragStarted = false;
        _spacing = 0;
        _itemsControl = null;

        _draggedContainer = null;
    }

    private void SetZIndexesBasedOnItemsIndex(ItemsControl? itemsControl)
    {
        if (itemsControl?.Items is null)
        {
            return;
        }

        int i = 0;

        foreach (object? _ in itemsControl.Items)
        {
            var container = itemsControl.ContainerFromIndex(i);
            if (container is not null)
            {
                container.ZIndex = i * 10;
            }

            i++;
        }
    }


    private void AddTransforms(ItemsControl? itemsControl)
    {
        if (itemsControl?.Items is null)
        {
            return;
        }

        int i = 0;

        foreach (object? _ in itemsControl.Items)
        {
            var container = itemsControl.ContainerFromIndex(i);
            if (container is not null)
            {
                SetTranslateTransform(container, 0, 0);
            }

            i++;
        }
    }

    private void RemoveTransforms(ItemsControl? itemsControl)
    {
        if (itemsControl?.Items is null)
        {
            return;
        }

        int i = 0;

        foreach (object? _ in itemsControl.Items)
        {
            var container = itemsControl.ContainerFromIndex(i);
            if (container is not null)
            {
                SetTranslateTransform(container, 0, 0);
            }

            i++;
        }
    }

    private void MoveDraggedItem(ItemsControl? itemsControl, int draggedIndex, int targetIndex)
    {
        if (itemsControl?.ItemsSource is IList itemsSource)
        {
            object? draggedItem = itemsSource[draggedIndex];
            itemsSource.RemoveAt(draggedIndex);
            itemsSource.Insert(targetIndex, draggedItem);

            if (itemsControl is SelectingItemsControl selectingItemsControl)
            {
                selectingItemsControl.SelectedIndex = targetIndex;
            }
        }
        else
        {
            if (itemsControl?.Items is { IsReadOnly: false } itemCollection)
            {
                object? draggedItem = itemCollection[draggedIndex];
                itemCollection.RemoveAt(draggedIndex);
                itemCollection.Insert(targetIndex, draggedItem);

                if (itemsControl is SelectingItemsControl selectingItemsControl)
                {
                    selectingItemsControl.SelectedIndex = targetIndex;
                }
            }
        }
    }

    private void PointerMoved(object? sender, PointerEventArgs e)
    {
        var properties = e.GetCurrentPoint(AssociatedObject).Properties;
        if (_captured
            && properties.IsLeftButtonPressed)
        {
            if (_itemsControl?.Items is null || _draggedContainer?.RenderTransform is null || !_enableDrag)
            {
                return;
            }

            var orientation = Orientation;
            var position = e.GetPosition(_itemsControl);
            double delta = orientation == Orientation.Horizontal ? position.X - _start.X : position.Y - _start.Y;
            bool changedDirection = Math.Sign(delta) != Math.Sign(_lastDelta);

            if (!_dragStarted)
            {
                var diff = _start - position;
                double horizontalDragThreshold = HorizontalDragThreshold;
                double verticalDragThreshold = VerticalDragThreshold;

                if (orientation == Orientation.Horizontal)
                {
                    if (Math.Abs(diff.X) > horizontalDragThreshold)
                    {
                        _dragStarted = true;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (Math.Abs(diff.Y) > verticalDragThreshold)
                    {
                        _dragStarted = true;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (orientation == Orientation.Horizontal)
            {
                SetTranslateTransform(_draggedContainer, delta, 0);
            }
            else
            {
                SetTranslateTransform(_draggedContainer, 0, delta);
            }

            _draggedIndex = _itemsControl.IndexFromContainer(_draggedContainer);
            _targetIndex = -1;

            var draggedBounds = _draggedContainer.Bounds;

            double draggedStart = orientation == Orientation.Horizontal ? draggedBounds.X : draggedBounds.Y;

            double draggedDeltaStart = orientation == Orientation.Horizontal
                ? draggedBounds.X + delta
                : draggedBounds.Y + delta;

            double draggedDeltaEnd = orientation == Orientation.Horizontal
                ? draggedBounds.X + delta + draggedBounds.Width + double.Min(_spacing, 0)
                : draggedBounds.Y + delta + draggedBounds.Height + double.Min(_spacing, 0);

            int i = 0;

            foreach (object? _ in _itemsControl.Items)
            {
                var targetContainer = _itemsControl.ContainerFromIndex(i);
                if (targetContainer?.RenderTransform is null || ReferenceEquals(targetContainer, _draggedContainer))
                {
                    i++;
                    continue;
                }

                object? item = _itemsControl.ItemFromContainer(targetContainer);

                var targetBounds = targetContainer.Bounds;

                double targetStart = orientation == Orientation.Horizontal ? targetBounds.X : targetBounds.Y;

                double targetMid = orientation == Orientation.Horizontal
                    ? targetBounds.X + (targetBounds.Width / 2)
                    : targetBounds.Y + (targetBounds.Height / 2);

                if (_spacing < 0)
                {
                    targetMid += _spacing / 2;
                }

                int targetIndex = _itemsControl.IndexFromContainer(targetContainer);


                if (draggedDeltaEnd >= targetMid && targetStart > draggedStart)
                {
                    if (OverrideZIndex && _draggedContainer.ZIndex < targetContainer.ZIndex)
                    {
                        _draggedContainer.ZIndex = targetContainer.ZIndex + 5;
                    }

                    if (orientation == Orientation.Horizontal)
                    {
                        SetTranslateTransform(targetContainer, -(draggedBounds.Width + _spacing), 0);
                    }
                    else
                    {
                        SetTranslateTransform(targetContainer, 0, -(draggedBounds.Height + _spacing));
                    }

                    _targetIndex = _targetIndex == -1
                        ? targetIndex
                        : targetIndex > _targetIndex
                            ? targetIndex
                            : _targetIndex;

                    _lastDelta = delta;
                }
                else if (draggedDeltaStart <= targetMid && targetStart < draggedStart)
                {
                    if (OverrideZIndex && _draggedContainer.ZIndex > targetContainer.ZIndex)
                    {
                        _draggedContainer.ZIndex = targetContainer.ZIndex - 5;
                    }

                    if (orientation == Orientation.Horizontal)
                    {
                        SetTranslateTransform(targetContainer, draggedBounds.Width + _spacing, 0);
                    }
                    else
                    {
                        SetTranslateTransform(targetContainer, 0, draggedBounds.Height + _spacing);
                    }

                    _targetIndex = _targetIndex == -1
                        ? targetIndex
                        : targetIndex < _targetIndex
                            ? targetIndex
                            : _targetIndex;


                    _lastDelta = delta;
                }
                else
                {
                    var targetRenderTransformMatrix = targetContainer.RenderTransform.Value;

                    if (!targetRenderTransformMatrix.IsIdentity)
                    {
                        _draggedContainer.ZIndex = delta < 0
                            ? targetContainer.ZIndex + 5
                            : targetContainer.ZIndex - 5;

                    }


                    if (orientation == Orientation.Horizontal)
                    {
                        SetTranslateTransform(targetContainer, 0, 0);
                    }
                    else
                    {
                        SetTranslateTransform(targetContainer, 0, 0);
                    }
                }

                i++;
            }
        }
    }

    private void SetDraggingPseudoClasses(Control control, bool isDragging)
    {
        if (isDragging)
        {
            ((IPseudoClasses)control.Classes).Add(":dragging");
        }
        else
        {
            ((IPseudoClasses)control.Classes).Remove(":dragging");
        }
    }

    private void SetTranslateTransform(Control control, double x, double y)
    {
        var transformBuilder = new TransformOperations.Builder(1);
        transformBuilder.AppendTranslate(x, y);
        control.RenderTransform = transformBuilder.Build();
    }
}

