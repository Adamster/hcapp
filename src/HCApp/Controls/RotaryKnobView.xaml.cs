using System.Collections;
using Microsoft.Maui.Graphics;

namespace HCApp.Controls;

public partial class RotaryKnobView : ContentView
{
    private readonly RotaryKnobDrawable _drawable = new();

#if MACCATALYST
    private float _scrollAccum = 0f;
#endif

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(RotaryKnobView),
            null,
            propertyChanged: (b, _, _) => ((RotaryKnobView)b).OnDataChanged());

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(
            nameof(SelectedItem),
            typeof(object),
            typeof(RotaryKnobView),
            null,
            BindingMode.TwoWay,
            propertyChanged: (b, _, _) => ((RotaryKnobView)b).OnDataChanged());

    // Plain CLR property — NOT a BindableProperty.
    // If declared as BindableProperty, MAUI XAML applies the {Binding Name} to the VM instead
    // of storing the Binding object, causing item.ToString() fallback.
    private BindingBase? _itemDisplayBinding;
    public BindingBase? ItemDisplayBinding
    {
        get => _itemDisplayBinding;
        set { _itemDisplayBinding = value; OnDataChanged(); }
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public RotaryKnobView()
    {
        InitializeComponent();
        KnobCanvas.Drawable = _drawable;

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        KnobCanvas.GestureRecognizers.Add(tap);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

#if WINDOWS
        if (Handler?.PlatformView is Microsoft.UI.Xaml.UIElement el)
            el.PointerWheelChanged += OnWindowsWheel;
#elif MACCATALYST
        if (Handler?.PlatformView is UIKit.UIView nativeView)
        {
            var pan = new UIKit.UIPanGestureRecognizer(OnMacScroll);
            pan.AllowedScrollTypesMask = UIKit.UIScrollTypeMask.Discrete | UIKit.UIScrollTypeMask.Continuous;
            pan.MaximumNumberOfTouches = 0;
            nativeView.AddGestureRecognizer(pan);
        }
#endif
    }

    private void CycleEnvironment(int direction)
    {
        var items = GetItems();
        if (items.Count <= 1) return;
        int current = SelectedItem is not null ? items.IndexOf(SelectedItem) : 0;
        if (current < 0) current = 0;
        int next = (current + direction + items.Count) % items.Count;
        SelectedItem = items[next];
    }

#if WINDOWS
    private void OnWindowsWheel(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint((Microsoft.UI.Xaml.UIElement)sender).Properties.MouseWheelDelta;
        CycleEnvironment(delta > 0 ? -1 : 1);
        e.Handled = true;
    }
#elif MACCATALYST
    private void OnMacScroll(UIKit.UIPanGestureRecognizer gesture)
    {
        if (gesture.State == UIKit.UIGestureRecognizerState.Began)
        {
            _scrollAccum = 0f;
            return;
        }

        var translation = gesture.TranslationInView(gesture.View);
        gesture.SetTranslation(CoreGraphics.CGPoint.Empty, gesture.View);

        _scrollAccum += (float)translation.Y;
        const float threshold = 20f;

        if (_scrollAccum >= threshold)
        {
            CycleEnvironment(1);
            _scrollAccum = 0f;
        }
        else if (_scrollAccum <= -threshold)
        {
            CycleEnvironment(-1);
            _scrollAccum = 0f;
        }
    }
#endif

    private List<object> GetItems()
    {
        if (ItemsSource is null) return [];
        var list = new List<object>();
        foreach (var item in ItemsSource)
            list.Add(item);
        return list;
    }

    private string ResolveDisplayName(object item)
    {
        if (ItemDisplayBinding is Binding { Path: { } path })
        {
            var prop = item.GetType().GetProperty(path);
            return prop?.GetValue(item)?.ToString() ?? item.ToString() ?? string.Empty;
        }
        return item.ToString() ?? string.Empty;
    }

    private void OnDataChanged()
    {
        var items = GetItems();
        int count = items.Count;
        int selectedIndex = SelectedItem is not null ? items.IndexOf(SelectedItem) : -1;
        if (selectedIndex < 0 && count > 0) selectedIndex = 0;

        bool isCircle = count > 5;

        _drawable.EnvironmentCount = count;
        _drawable.SelectedIndex = selectedIndex < 0 ? 0 : selectedIndex;
        _drawable.SelectedName = selectedIndex >= 0 ? ResolveDisplayName(items[selectedIndex]) : string.Empty;
        _drawable.Labels = items.Select(ResolveDisplayName).ToList();

        Nameplate.IsVisible = !isCircle && count > 0;
        if (!isCircle && count > 0)
            NameplateLabel.Text = _drawable.SelectedName.ToUpperInvariant();

        KnobCanvas.Invalidate();
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        var items = GetItems();
        if (items.Count == 0) return;

        var point = e.GetPosition(KnobCanvas);
        if (point is null) return;

        float tapX = (float)point.Value.X;
        float tapY = (float)point.Value.Y;
        float cx = 110f;
        float cy = 110f;
        const float hitRadius = 14f;

        for (int i = 0; i < items.Count; i++)
        {
            var tickPos = _drawable.GetTickPosition(i, cx, cy);
            float dx = tapX - tickPos.X;
            float dy = tapY - tickPos.Y;
            if (MathF.Sqrt(dx * dx + dy * dy) <= hitRadius)
            {
                SelectedItem = items[i];
                return;
            }
        }
    }
}
