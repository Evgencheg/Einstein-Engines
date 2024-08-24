using System.Linq;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls;

/// <summary>
///     A simple yet good-looking tab container using normal UI elements with multiple styles
///     <br />
///     Because nobody else could do it better.
/// </summary>
[GenerateTypedNameReferences]
public sealed partial class NeoTabContainer : BoxContainer
{
    private readonly Dictionary<Control, BaseButton> _tabs = new();
    private readonly List<Control> _controls = new();
    private readonly ButtonGroup _tabGroup = new(false);

    /// <summary>
    ///     All children within the <see cref="ContentContainer"/>
    /// </summary>
    public OrderedChildCollection Tabs => ContentContainer.Children;

    public Control? CurrentControl { get; private set; }
    public int? CurrentTab => _controls.FirstOrDefault(control => control == CurrentControl) switch
    {
        { } control => _controls.IndexOf(control),
        _ => null,
    };

    /// <summary>
    ///     If true, the tabs will be displayed horizontally over the top of the contents
    ///     <br />
    ///     If false, the tabs will be displayed vertically to the left of the contents
    /// </summary>
    private bool _horizontal = true;
    /// <inheritdoc cref="_horizontal"/>
    public bool Horizontal
    {
        get => _horizontal;
        set => LayoutChanged(value);
    }

    //TODO private bool _swapSides = false;

    private bool _hScrollEnabled;
    public bool HScrollEnabled
    {
        get => _hScrollEnabled;
        set => ScrollingChanged(value, _vScrollEnabled);
    }

    private bool _vScrollEnabled;
    public bool VScrollEnabled
    {
        get => _vScrollEnabled;
        set => ScrollingChanged(_hScrollEnabled, value);
    }


    /// <inheritdoc cref="NeoTabContainer"/>
    public NeoTabContainer()
    {
        RobustXamlLoader.Load(this);

        LayoutChanged(Horizontal);
        ScrollingChanged(HScrollEnabled, VScrollEnabled);
    }

    //TODO This sucks, put this on some post-init if that exists
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        foreach (var child in Children.Where(child => child.Name is not nameof(Container)).ToList())
        {
            child.Orphan();
            AddTab(child, child.Name ?? "Untitled Tab");
        }
    }

    protected override void ChildRemoved(Control child)
    {
        if (_tabs.Remove(child, out var button))
            button.Dispose();

        // Set the current tab to a different control
        if (CurrentControl == child)
        {
            var previous = _controls.IndexOf(child) - 1;

            if (previous > -1)
                SelectTab(_controls[previous]);
            else
                CurrentControl = null;
        }

        _controls.Remove(child);
        base.ChildRemoved(child);
    }

    /// <summary>
    ///     Changes the layout of the tabs and contents based on the value
    /// </summary>
    /// <param name="value">See <see cref="Horizontal"/></param>
    private void LayoutChanged(bool value)
    {
        _horizontal = value;

        TabContainer.Orientation = Horizontal ? LayoutOrientation.Horizontal : LayoutOrientation.Vertical;
        Container.Orientation = Horizontal ? LayoutOrientation.Vertical : LayoutOrientation.Horizontal;
        TabScrollContainer.Margin = Horizontal ? new Thickness(5, 5, 5, 0) : new Thickness(5, 5, 0, 5);
        ContentScrollContainer.Margin = Horizontal ? new Thickness(5, 0, 5, 5) : new Thickness(0, 5, 5, 5);

        TabScrollContainer.HorizontalExpand = Horizontal;
        TabScrollContainer.VerticalExpand = !Horizontal;
        TabScrollContainer.HScrollEnabled = Horizontal;
        TabScrollContainer.VScrollEnabled = !Horizontal;
    }

    private void ScrollingChanged(bool hScroll, bool vScroll)
    {
        _hScrollEnabled = hScroll;
        _vScrollEnabled = vScroll;

        ContentScrollContainer.HScrollEnabled = hScroll;
        ContentScrollContainer.VScrollEnabled = vScroll;
    }


    /// <summary>
    ///     Adds a tab to this container
    /// </summary>
    /// <param name="control">The tab contents</param>
    /// <param name="title">The title of the tab</param>
    /// <returns>The index of the new tab</returns>
    public int AddTab(Control control, string title)
    {
        var button = new Button
        {
            Text = title,
            Group = _tabGroup,
            MinHeight = 32,
            MaxHeight = 32,
            HorizontalExpand = true,
        };
        button.OnPressed += _ => SelectTab(control);

        TabContainer.AddChild(button);
        ContentContainer.AddChild(control);
        _controls.Add(control);
        _tabs.Add(control, button);

        // Show it if it's the only tab
        if (ContentContainer.ChildCount > 1)
            control.Visible = false;
        else
            SelectTab(control);

        return ChildCount - 1;
    }

    /// <summary>
    ///     Sets the title of the tab associated with the given index
    /// </summary>
    public void SetTabTitle(int index, string title)
    {
        if (index < 0 || index >= _controls.Count)
            return;

        var control = _controls[index];
        SetTabTitle(control, title);
    }

    /// <summary>
    ///     Sets the title of the tab associated with the given control
    /// </summary>
    public void SetTabTitle(Control control, string title)
    {
        if (!_tabs.TryGetValue(control, out var button))
            return;

        if (button is Button b)
            b.Text = title;
    }

    /// <summary>
    ///     Shows or hides the tab associated with the given index
    /// </summary>
    public void SetTabVisible(int index, bool visible)
    {
        if (index < 0 || index >= _controls.Count)
            return;

        var control = _controls[index];
        SetTabVisible(control, visible);
    }

    /// <summary>
    ///     Shows or hides the tab associated with the given control
    /// </summary>
    public void SetTabVisible(Control control, bool visible)
    {
        if (!_tabs.TryGetValue(control, out var button))
            return;

        button.Visible = visible;
    }

    /// <summary>
    ///     Selects the tab associated with the control
    /// </summary>
    public void SelectTab(Control control)
    {
        if (CurrentControl != null)
            CurrentControl.Visible = false;

        var button = _tabs[control];
        button.Pressed = true;
        control.Visible = true;
        CurrentControl = control;
    }
}
