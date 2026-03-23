using System.Windows.Input;

namespace HCApp.Controls;

public enum ButtonVariant { Primary, Secondary, Danger, Icon }

public partial class SteampunkButton : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(SteampunkButton), string.Empty,
            propertyChanged: (b, _, n) => ((SteampunkButton)b).ButtonLabel.Text = (string)n);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SteampunkButton), null);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(SteampunkButton), null);

    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(nameof(Variant), typeof(ButtonVariant), typeof(SteampunkButton), ButtonVariant.Secondary,
            propertyChanged: (b, o, n) => { if (o != n) ((SteampunkButton)b).ApplyVariant(); });

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public ButtonVariant Variant
    {
        get => (ButtonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    private Shadow _normalShadow = new();
    private Shadow _pressedShadow = new();

    public SteampunkButton()
    {
        InitializeComponent();

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            if (!IsEnabled) return;
            await AnimatePress();
            if (Command?.CanExecute(CommandParameter) == true)
                Command.Execute(CommandParameter);
        };
        GestureRecognizers.Add(tap);

        ApplyVariant();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsEnabledProperty.PropertyName)
            Face.Opacity = IsEnabled ? 1.0 : 0.45;
    }

    private void ApplyVariant()
    {
        var (grad, normalShadow, pressedShadow) = Variant switch
        {
            ButtonVariant.Primary => (
                BuildGradient("#3A1606", "#7A3010", "#C85A1C"),
                new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#D06020")), Radius = 12, Opacity = 0.65f, Offset = new Point(0, 0) },
                new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#D06020")), Radius = 8, Opacity = 0.3f, Offset = new Point(0, 0) }
            ),
            ButtonVariant.Danger => (
                BuildGradient("#3A0606", "#800E0E", "#C01414"),
                new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#E02020")), Radius = 12, Opacity = 0.65f, Offset = new Point(0, 0) },
                new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#E02020")), Radius = 8, Opacity = 0.3f, Offset = new Point(0, 0) }
            ),
            _ => (
                BuildGradient("#2A1A0A", "#3E2810", "#5A3A1A"),
                new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#8A6030")), Radius = 10, Opacity = 0.35f, Offset = new Point(0, 0) },
                new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#8A6030")), Radius = 6, Opacity = 0.15f, Offset = new Point(0, 0) }
            )
        };

        Face.Background = grad;
        Face.Shadow = normalShadow;
        _normalShadow = normalShadow;
        _pressedShadow = pressedShadow;

        if (Variant == ButtonVariant.Icon)
        {
            Face.Padding = new Thickness(4);
            Bezel.Padding = new Thickness(2);
        }
    }

    private static LinearGradientBrush BuildGradient(string darkMid, string brightMid, string bottom)
    {
        return new LinearGradientBrush(
        [
            new GradientStop(Color.FromArgb("#0D0804"), 0.00f),
            new GradientStop(Color.FromArgb(darkMid),  0.35f),
            new GradientStop(Color.FromArgb(brightMid), 0.70f),
            new GradientStop(Color.FromArgb(bottom),    1.00f)
        ],
        new Point(0, 0),
        new Point(0, 1));
    }

    private async Task AnimatePress()
    {
        Face.Shadow = _pressedShadow;
        await Face.ScaleTo(0.93, 60, Easing.CubicIn);
        await Task.Delay(80);
        Face.Shadow = _normalShadow;
        await Face.ScaleTo(1.0, 80, Easing.CubicOut);
    }
}
