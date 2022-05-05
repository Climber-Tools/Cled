using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BottomBar : MonoBehaviour
{
    public HoldPickerMenu holdPickerMenu;

    public StyleSheet globalStyleSheets;

    private VisualElement _root;
    private VisualElement _bottomBar;

    void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _bottomBar = _root.Q<VisualElement>("bottom-bar");
    }

    void Start()
    {
        holdPickerMenu.AddChangedCallback(UpdateBar);

        UpdateBar();
    }

    /// <summary>
    /// Update the bottom bar according to the currently selected holds.
    /// </summary>
    void UpdateBar()
    {
        _bottomBar.Clear();

        var previous = holdPickerMenu.GetPreviousHold();
        var current = holdPickerMenu.CurrentlySelectedHold;
        var next = holdPickerMenu.GetNextHold();

        var holds = previous == current ? new[] { current } : new[] { previous, current, next };
        
        foreach (var hold in holds)
        {
            if (hold == null)
                continue;
            
            var item = new VisualElement();

            item.styleSheets.Add(globalStyleSheets);

            // make it either primary or secondary, depending on whether it's the selected one or the other ones
            item.AddToClassList(hold == holdPickerMenu.CurrentlySelectedHold
                ? "bottom-bar-primary-hold"
                : "bottom-bar-secondary-hold");

            item.style.backgroundImage =
                new StyleBackground(
                    Background.FromTexture2D(
                        holdPickerMenu.GridTextureDictionary[holdPickerMenu.HoldToGridDictionary[hold]]));

            _bottomBar.Add(item);
        }
    }
}