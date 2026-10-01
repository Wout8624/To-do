using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace FastTodo;

public partial class MainWindow : Window
{
    readonly ObservableCollection<TodoItem> _items;

    public MainWindow()
    {
        InitializeComponent();

        _items = new(Store.Load());
        foreach (var t in _items) t.PropertyChanged += Item_Changed;
        _items.CollectionChanged += (_, _) => Changed();
        TaskList.ItemsSource = _items;
        UpdateTotal();

        MouseDown += (_, _) => Keyboard.ClearFocus(); // click empty space = done editing
        Loaded += (_, _) => NewTask.Focus();
    }

    void Item_Changed(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TodoItem.Name) or nameof(TodoItem.Percent)) Changed();
    }

    void Changed()
    {
        UpdateTotal();
        Store.Save(_items);
    }

    void UpdateTotal()
    {
        double avg = _items.Count == 0 ? 0 : _items.Average(t => t.Percent);
        Total.Text = $"TOTAL COMPLETION: [{avg.ToString("0.###", CultureInfo.InvariantCulture)}% complete]";
    }

    void NewTask_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || string.IsNullOrWhiteSpace(NewTask.Text)) return;
        var item = new TodoItem { Name = NewTask.Text.Trim() };
        item.PropertyChanged += Item_Changed;
        _items.Add(item);
        NewTask.Clear();
    }

    void Delete_Click(object sender, RoutedEventArgs e) =>
        _items.Remove((TodoItem)((FrameworkElement)sender).DataContext);

    void Box_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape) Keyboard.ClearFocus();
    }

    // First click on a percentage selects it, so you can just type the new number.
    void Percent_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var box = (TextBox)sender;
        if (box.IsKeyboardFocusWithin) return;
        e.Handled = true;
        box.Focus();
        box.SelectAll();
    }

    void Percent_LostFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        ((TodoItem)((FrameworkElement)sender).DataContext).NormalizeText();

    // Dark title bar to match the window (Windows 10 20H1+ / Windows 11).
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        int on = 1;
        DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref on, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
