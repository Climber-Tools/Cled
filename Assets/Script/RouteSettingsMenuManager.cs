using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RouteSettingsMenuManager : MonoBehaviour
{
    public WallManager wallManager;
    public PauseManager pauseManager;

    private VisualElement _root;
    private Dictionary<string, TextField> _textfields;
    private Dictionary<string, DropdownField> _dropdowns;

    private Dictionary<string, Func<List<string>>> _attributeMapping;

    private Route _selectedRoute;

    private bool _initialized;

    void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.visible = false;

        // they're functions because the values haven't been initialized by this point
        _attributeMapping = new Dictionary<string, Func<List<string>>>
        {
            { "name", () => null },
            { "grade", () => wallManager.Metadata.Grades },
            { "zone", () => wallManager.Metadata.Zones },
            { "setter", () => wallManager.Metadata.Setters }
        };

        _textfields = new Dictionary<string, TextField>();
        _dropdowns = new Dictionary<string, DropdownField>();

        foreach (var attribute in _attributeMapping.Keys)
        {
            _textfields[attribute] = _root.Q<TextField>($"{attribute}-textfield");
            _dropdowns[attribute] = _root.Q<DropdownField>($"{attribute}-dropdown");
        }

        var applyButton = _root.Q<Button>("apply-button");
        applyButton.clicked += Apply;

        var discardButton = _root.Q<Button>("discard-button");
        discardButton.clicked += Discard;
    }

    /// <summary>
    /// Apply the settings to the given route, hiding the settings.
    /// </summary>
    private void Apply()
    {
        foreach (var attribute in _attributeMapping.Keys)
        {
            var textfield = _textfields[attribute];

            switch (attribute)
            {
                case "name":
                    _selectedRoute.Name = textfield.value;
                    break;
                case "zone":
                    _selectedRoute.Zone = textfield.value;
                    break;
                case "grade":
                    _selectedRoute.Grade = textfield.value;
                    break;
                case "setter":
                    _selectedRoute.Setter = textfield.value;
                    break;
            }
        }
        
        Close();
    }

    /// <summary>
    /// Discard the settings of the given route, hiding the settings.
    /// </summary>
    private void Discard() => Close();

    public void Close()
    {
        _root.visible = false;
        pauseManager.Unpause(PauseType.RouteSettings);
    }

    private void _initialize()
    {
        foreach (var (attribute, metadataFunction) in _attributeMapping)
        {
            var metadata = metadataFunction();

            var dropdown = _dropdowns[attribute];

            if (dropdown != null)
            {
                if (metadata != null && metadata.Count != 0)
                {
                    var textfield = _textfields[attribute];

                    var allValues = new List<string>(metadata);
                    allValues.Insert(0, attribute[..1].ToUpper() + attribute[1..]);
                    allValues.Insert(1, "");

                    dropdown.choices = allValues;
                    dropdown.RegisterValueChangedCallback(val =>
                    {
                        if (val.newValue != attribute[..1].ToUpper() + attribute[1..])
                            textfield.SetValueWithoutNotify(val.newValue);
                    });
                    
                    dropdown.index = 0;

                    textfield.RegisterValueChangedCallback(val => { dropdown.index = 0; });
                }
                else
                {
                    dropdown.SetEnabled(false);
                }
            }
        }
        
        _initialized = true;
    }

    /// <summary>
    /// Set menu values from route.
    /// </summary>
    private void _setFromRoute(Route route)
    {
        foreach (var attribute in _attributeMapping.Keys)
        {
            var textfield = _textfields[attribute];

            // TODO: this is ugly
            switch (attribute)
            {
                case "name":
                    textfield.SetValueWithoutNotify(route.Name);
                    break;
                case "zone":
                    textfield.SetValueWithoutNotify(route.Zone);
                    break;
                case "grade":
                    textfield.SetValueWithoutNotify(route.Grade);
                    break;
                case "setter":
                    textfield.SetValueWithoutNotify(route.Setter);
                    break;
            }
        }
    }

    /// <summary>
    /// Show the route settings for this route.
    /// </summary>
    public void Show(Route route)
    {
        _selectedRoute = route;
        _root.visible = true;

        if (!_initialized)
            _initialize();
        
        _setFromRoute(route);

        pauseManager.Pause(PauseType.RouteSettings);
    }
}