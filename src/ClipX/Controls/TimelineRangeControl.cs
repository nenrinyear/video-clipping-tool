using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ClipX;

public enum TimelineHandle { Position, Start, End }

public sealed class TimelineInteractionEventArgs(TimelineHandle handle, double value) : EventArgs
{
    public TimelineHandle Handle { get; } = handle;
    public double Value { get; } = value;
}

/// <summary>A compact timeline combining playback position and trim range.</summary>
public sealed class TimelineRangeControl : FrameworkElement
{
    private const double EdgePadding = 10;
    private TimelineHandle? activeHandle;

    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration), typeof(double), typeof(TimelineRangeControl), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
        nameof(Position), typeof(double), typeof(TimelineRangeControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty RangeStartProperty = DependencyProperty.Register(
        nameof(RangeStart), typeof(double), typeof(TimelineRangeControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty RangeEndProperty = DependencyProperty.Register(
        nameof(RangeEnd), typeof(double), typeof(TimelineRangeControl), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Duration { get => (double)GetValue(DurationProperty); set => SetValue(DurationProperty, Math.Max(.1, value)); }
    public double Position { get => (double)GetValue(PositionProperty); set => SetValue(PositionProperty, Clamp(value)); }
    public double RangeStart { get => (double)GetValue(RangeStartProperty); set => SetValue(RangeStartProperty, Math.Clamp(value, 0, Math.Max(0, RangeEnd - .1))); }
    public double RangeEnd { get => (double)GetValue(RangeEndProperty); set => SetValue(RangeEndProperty, Math.Clamp(value, Math.Min(Duration, RangeStart + .1), Duration)); }

    public event EventHandler<TimelineInteractionEventArgs>? InteractionStarted;
    public event EventHandler<TimelineInteractionEventArgs>? ValueChangedByUser;
    public event EventHandler<TimelineInteractionEventArgs>? InteractionCompleted;

    public TimelineRangeControl()
    {
        Height = 36;
        Cursor = Cursors.Hand;
        Focusable = true;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var y = ActualHeight / 2;
        var left = X(RangeStart); var right = X(RangeEnd); var play = X(Position);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(55, 58, 68)), null, new Rect(EdgePadding, y - 3, Math.Max(0, ActualWidth - EdgePadding * 2), 6), 3, 3);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(108, 99, 255)), null, new Rect(left, y - 4, Math.Max(0, right - left), 8), 4, 4);

        var accent = new SolidColorBrush(Color.FromRgb(139, 132, 255));
        dc.DrawRoundedRectangle(accent, null, new Rect(left - 5, y - 13, 10, 26), 4, 4);
        dc.DrawRoundedRectangle(accent, null, new Rect(right - 5, y - 13, 10, 26), 4, 4);

        var white = Brushes.White;
        dc.DrawLine(new Pen(white, 2), new Point(play, y - 12), new Point(play, y + 12));
        dc.DrawEllipse(white, null, new Point(play, y - 12), 4, 4);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus(); CaptureMouse();
        var x = e.GetPosition(this).X;
        var startDistance = Math.Abs(x - X(RangeStart));
        var endDistance = Math.Abs(x - X(RangeEnd));
        activeHandle = Math.Min(startDistance, endDistance) <= 12
            ? (startDistance <= endDistance ? TimelineHandle.Start : TimelineHandle.End)
            : TimelineHandle.Position;
        var value = ValueFromX(x);
        InteractionStarted?.Invoke(this, new(activeHandle.Value, value));
        SetFromPointer(value);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (activeHandle is not null && e.LeftButton == MouseButtonState.Pressed)
            SetFromPointer(ValueFromX(e.GetPosition(this).X));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (activeHandle is { } handle)
        {
            var value = handle switch { TimelineHandle.Start => RangeStart, TimelineHandle.End => RangeEnd, _ => Position };
            InteractionCompleted?.Invoke(this, new(handle, value));
        }
        activeHandle = null; ReleaseMouseCapture(); e.Handled = true;
    }

    private void SetFromPointer(double value)
    {
        if (activeHandle is null) return;
        switch (activeHandle)
        {
            case TimelineHandle.Start: RangeStart = Math.Min(value, RangeEnd - .1); break;
            case TimelineHandle.End: RangeEnd = Math.Max(value, RangeStart + .1); break;
            default: Position = value; break;
        }
        var actual = activeHandle switch { TimelineHandle.Start => RangeStart, TimelineHandle.End => RangeEnd, _ => Position };
        ValueChangedByUser?.Invoke(this, new(activeHandle.Value, actual));
    }

    private double X(double value) => EdgePadding + Clamp(value) / Math.Max(.1, Duration) * Math.Max(0, ActualWidth - EdgePadding * 2);
    private double ValueFromX(double x) => Math.Clamp((x - EdgePadding) / Math.Max(1, ActualWidth - EdgePadding * 2), 0, 1) * Duration;
    private double Clamp(double value) => Math.Clamp(value, 0, Math.Max(.1, Duration));
}
