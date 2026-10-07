using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

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

        SetZoom(Store.LoadZoom(), save: false);
        PreviewMouseWheel += (_, e) =>
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            SetZoom(Zoom.ScaleX + (e.Delta > 0 ? 0.1 : -0.1));
            e.Handled = true;
        };
        PreviewKeyDown += (_, e) =>
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            double? z = e.Key switch
            {
                Key.OemPlus or Key.Add => Zoom.ScaleX + 0.1,
                Key.OemMinus or Key.Subtract => Zoom.ScaleX - 0.1,
                Key.D0 or Key.NumPad0 => 1,
                _ => null
            };
            if (z is not double v) return;
            SetZoom(v);
            e.Handled = true;
        };

        MouseDown += (_, _) => Keyboard.ClearFocus(); // click empty space = done editing
        Loaded += (_, _) => NewTask.Focus();
    }

    void SetZoom(double z, bool save = true)
    {
        z = Math.Round(Math.Clamp(z, 0.7, 2.5), 1);
        Zoom.ScaleX = Zoom.ScaleY = z;
        if (save) Store.SaveZoom(z);
    }

    void Item_Changed(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TodoItem.Name) or nameof(TodoItem.Percent) or nameof(TodoItem.Note)) Changed();
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

    // ✎ opens the note line (if hidden) and puts the cursor at the end of the note.
    void Note_Click(object sender, RoutedEventArgs e)
    {
        var button = (FrameworkElement)sender;
        var row = (ContentPresenter)button.TemplatedParent;
        var box = (TextBox)row.ContentTemplate.FindName("NoteBox", row);
        ((TodoItem)button.DataContext).NoteOpen = true;
        Dispatcher.InvokeAsync(() => { box.Focus(); box.CaretIndex = box.Text.Length; }, DispatcherPriority.Loaded);
    }

    // Leaving an empty note hides the line again.
    void Note_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        var item = (TodoItem)((FrameworkElement)sender).DataContext;
        item.Note = item.Note.Trim();
        item.NoteOpen = false;
    }

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
