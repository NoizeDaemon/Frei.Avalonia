using Avalonia;
using Avalonia.Data;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using System.Diagnostics;

namespace Frei.Avalonia.Controls;

public class TimeBar : ProgressBar
{
    protected override Type StyleKeyOverride => typeof(ProgressBar);


    public static readonly StyledProperty<TimeSpan> DurationProperty =
    AvaloniaProperty.Register<TimeBar, TimeSpan>("Duration", TimeSpan.FromSeconds(5));

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }


    public static readonly StyledProperty<IterationCount> IterationCountProperty =
    AvaloniaProperty.Register<TimeBar, IterationCount>("IterationCount", new IterationCount(1));

    public IterationCount IterationCount
    {
        get => GetValue(IterationCountProperty);
        set => SetValue(IterationCountProperty, value);
    }


    public static readonly DirectProperty<TimeBar, IterationCount> RemainingIterationsProperty =
    AvaloniaProperty.RegisterDirect<TimeBar, IterationCount>(
        nameof(RemainingIterations),
        o => o.RemainingIterations);

    private IterationCount _remainingIterations;

    public IterationCount RemainingIterations
    {
        get => _remainingIterations;
        private set => SetAndRaise(RemainingIterationsProperty, ref _remainingIterations, value);
    }


    public static readonly DirectProperty<TimeBar, PlayState> CurrentPlayStateProperty =
    AvaloniaProperty.RegisterDirect<TimeBar, PlayState>(
    nameof(CurrentPlayState),
    o => o.CurrentPlayState);

    private PlayState _currentPlayState;

    public PlayState CurrentPlayState
    {
        get => _currentPlayState;
        private set => SetAndRaise(CurrentPlayStateProperty, ref _currentPlayState, value);
    }


    public static readonly StyledProperty<PlaybackDirection> PlaybackDirectionProperty =
    AvaloniaProperty.Register<TimeBar, PlaybackDirection>("PlaybackDirection", PlaybackDirection.Normal);

    public PlaybackDirection PlaybackDirection
    {
        get => GetValue(PlaybackDirectionProperty);
        set => SetValue(PlaybackDirectionProperty, value);
    }


    public static readonly StyledProperty<TimeSpan> DelayProperty =
    AvaloniaProperty.Register<TimeBar, TimeSpan>("Delay", TimeSpan.Zero);

    public TimeSpan Delay
    {
        get => GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }


    public static readonly StyledProperty<TimeSpan> DelayBetweenIterationsProperty =
    AvaloniaProperty.Register<TimeBar, TimeSpan>("DelayBetweenIterations", TimeSpan.Zero);

    public TimeSpan DelayBetweenIterations
    {
        get => GetValue(DelayBetweenIterationsProperty);
        set => SetValue(DelayBetweenIterationsProperty, value);
    }


    public static readonly RoutedEvent<TimeBarCompletedEventArgs> TimerCompletedEvent =
    RoutedEvent.Register<TimeBar, TimeBarCompletedEventArgs>(nameof(TimerCompleted), RoutingStrategies.Bubble);

    public event EventHandler<TimeBarCompletedEventArgs> TimerCompleted
    {
        add => AddHandler(TimerCompletedEvent, value);
        remove => RemoveHandler(TimerCompletedEvent, value);
    }

    public class TimeBarCompletedEventArgs : RoutedEventArgs
    {
        private bool _isCountingUp;
        private IterationCount _remainingIterations;

        public TimeBarCompletedEventArgs(bool isCountingUp, IterationCount remainingIterations)
        {
            _isCountingUp = isCountingUp;
            _remainingIterations = remainingIterations;
        }

        public bool IsCountingUp => _isCountingUp;
        public IterationCount RemainingIterations => _remainingIterations;

    }

    protected virtual void OnTimerCompleted()
    {
        TimeBarCompletedEventArgs args = new TimeBarCompletedEventArgs(_isCountingUp, _remainingIterations) { RoutedEvent = TimerCompletedEvent };
        RaiseEvent(args);
    }


    private void TimeBar_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        e.Handled = true;

        var isCountingUp = _isCountingUp;

        Debug.WriteLine("isCountingUp: " + isCountingUp +  ", old: " + e.OldValue + ", new: " + e.NewValue);

        if (isCountingUp && e.NewValue > e.OldValue && e.NewValue != 100.0d) return;
        if (!isCountingUp && e.NewValue < e.OldValue && e.NewValue != 0.0d) return;


        if (_skipNextChangeCheck)
        {
            _skipNextChangeCheck = false;
            return;
        }

        Debug.WriteLine(RemainingIterations + "-" + IterationCount);
        Debug.WriteLine("old: " + e.OldValue + ", new: " + e.NewValue);

        if (!RemainingIterations.IsInfinite)
        {
            RemainingIterations = new IterationCount(RemainingIterations.Value - 1);

            CurrentPlayState = (RemainingIterations.Value == 0) ? PlayState.Stop : PlayState.Run;
        }

        if (CurrentPlayState == PlayState.Run)
        {
            _isCountingUp = _isAlternating ? !_isCountingUp : _isCountingUp;

            if (_isContinueAnimation)
            {
                _skipNextChangeCheck = true;
                ContinueInitialRepeatingAnimation();
            }
        }

        OnTimerCompleted();
    }


    private bool _isRepeating;
    private bool _isContinueAnimation;
    private bool _skipNextChangeCheck;
    private bool _isAlternating;
    private bool _isCountingUp;
    private Animation? _initialAnimation;
    private Animation? _currentAnimation;
    private CancellationTokenSource _cancellationTokenSource;

    public TimeBar()
    {
        this.ValueChanged += TimeBar_ValueChanged;
        _cancellationTokenSource = new CancellationTokenSource();

        this.AttachedToVisualTree += (s, e) => StartInitialAnimation();
    }

    public void StartInitialAnimation()
    {
        CanTogglePlayState = false;
        _isAlternating = PlaybackDirection switch { PlaybackDirection.Alternate or PlaybackDirection.AlternateReverse => true, _ => false };
        _isCountingUp = PlaybackDirection switch { PlaybackDirection.Normal or PlaybackDirection.Alternate => true, _ => false };
        _isRepeating = IterationCount.IsInfinite || IterationCount.Value > 1;

        _initialAnimation = GetInitialAnimation();
        _currentAnimation = _initialAnimation;
        RemainingIterations = IterationCount.IsInfinite ? IterationCount.Infinite : new IterationCount(IterationCount.Value);
        _initialAnimation.RunAsync(this, _cancellationTokenSource.Token);
        CurrentPlayState = PlayState.Run;
        CanTogglePlayState = true;
    }

    private Animation GetInitialAnimation()
    {
        return new Animation()
        {
            Duration = this.Duration,
            PlaybackDirection = this.PlaybackDirection,
            IterationCount = this.IterationCount,
            Delay = this.Delay,
            DelayBetweenIterations = this.DelayBetweenIterations,
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame()
                {
                    KeyTime = TimeSpan.FromSeconds(0),
                    Setters =
                    {
                        new Setter(ProgressBar.ValueProperty, 0.0d)
                    }
                },
                new KeyFrame()
                {
                    KeyTime = this.Duration,
                    Setters =
                    {
                        new Setter(ProgressBar.ValueProperty, 100.0d)
                    }
                }
            }
        };
    }

    private Animation GetContinueAnimation(double currentValue)
    {
        var remainingValue = _isCountingUp ? 100.0d - currentValue : currentValue;
        var remainingTime = TimeSpan.FromMicroseconds(Duration.TotalMicroseconds * (double)(remainingValue / 100));

        return new Animation()
        {
            Duration = remainingTime,
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame()
                {
                    KeyTime = TimeSpan.FromSeconds(0),
                    Setters =
                    {
                        new Setter(ProgressBar.ValueProperty, currentValue)
                    }
                },
                new KeyFrame()
                {
                    KeyTime = remainingTime,
                    Setters =
                    {
                        new Setter(ProgressBar.ValueProperty, _isCountingUp ? 100.0d : 0.0d)
                    }
                }
            }
        };
    }


    public void PauseAnimation()
    {
        if (_currentPlayState is PlayState.Pause or PlayState.Stop) return;

        CanTogglePlayState = false;

        CurrentPlayState = PlayState.Pause;

        var currentValue = Value;
        _cancellationTokenSource.Cancel();
        Value = currentValue;

        if (!_cancellationTokenSource.TryReset()) _cancellationTokenSource = new CancellationTokenSource();

        CanTogglePlayState = true;
    }


    public void ContinueAnimation()
    {
        if (_currentPlayState is PlayState.Run or PlayState.Stop) return;

        CanTogglePlayState = false;

        //TODO: what happens when pausing/resuming on 100 or 0?
        _currentAnimation = GetContinueAnimation(Value);
        _currentAnimation.RunAsync(this, _cancellationTokenSource.Token);
        CurrentPlayState = PlayState.Run;

        _isContinueAnimation = true;
        CanTogglePlayState = true;
    }

    private void ContinueInitialRepeatingAnimation()
    {
        CanTogglePlayState = false;

        _isContinueAnimation = false;
        _currentAnimation = GetInitialAnimation();
        _currentAnimation.IterationCount = _remainingIterations.IsInfinite ? IterationCount.Infinite : new IterationCount(_remainingIterations.Value);
        _currentAnimation.PlaybackDirection = _isAlternating ? (_isCountingUp ? PlaybackDirection.Alternate : PlaybackDirection.AlternateReverse) : PlaybackDirection;
        _currentAnimation.Delay = DelayBetweenIterations;
        _currentAnimation.RunAsync(this, _cancellationTokenSource.Token);
        CurrentPlayState = PlayState.Run;

        CanTogglePlayState = true;
    }

    public void TogglePlayState()
    {
        switch (_currentPlayState)
        {
            case PlayState.Run:
                PauseAnimation();
                break;
            case PlayState.Pause:
                ContinueAnimation();
                break;
            case PlayState.Stop:
                _skipNextChangeCheck = true;
                StartInitialAnimation();
                break;
        }
    }

    public bool CanTogglePlayState { get; private set; }
}