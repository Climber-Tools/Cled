using System;
using System.Globalization;
using SFB;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsMenu : MonoBehaviour, IClosable, IAcceptable
{
    public PauseMenu pauseMenu;
    public PopupMenu popupMenu;
    public LightManager lightManager;

    private VisualElement _root;

    private Button imagePathButton;
    private string imagePathButtonValue;
    private TextField imageSupersize;

    private TextField lightIntensityTextfield;
    private TextField shadowStrengthTextfield;


    private void Awake()
    {
        var document = GetComponent<UIDocument>();

        _root = document.rootVisualElement;
        _root.visible = false;

        lightIntensityTextfield = _root.Q<TextField>("light-intensity-textfield");
        shadowStrengthTextfield = _root.Q<TextField>("shadow-strength-textfield");
        imageSupersize = _root.Q<TextField>("image-superscale-textfield");

        imagePathButton = _root.Q<Button>("image-path-button");
        imagePathButton.clicked += () =>
        {
            var paths = StandaloneFileBrowser.OpenFolderPanel("Image Path", "", false);

            if (paths.Length != 1)
                return;

            var path = paths[0];
            if (string.IsNullOrWhiteSpace(path))
                return;

            imagePathButton.text = path;
            imagePathButtonValue = path;

            if (imagePathButton.text.Length > 20)
                imagePathButton.text = path[..10] + "..." + path[^10..];
        };

        var applyButton = _root.Q<Button>("apply-button");
        applyButton.clicked += Accept;

        var discardButton = _root.Q<Button>("discard-button");
        discardButton.clicked += Close;
    }

    public void Accept()
    {
        float lightIntensity;
        try
        {
            lightIntensity = float.Parse(lightIntensityTextfield.value, CultureInfo.InvariantCulture);

            if (lightIntensity < 0)
            {
                popupMenu.CreateInfoPopup("The light intensity value must be non-negative.");
                return;
            }
        }
        catch (FormatException)
        {
            popupMenu.CreateInfoPopup("The light intensity must be a float.");
            return;
        }

        float shadowStrength;
        try
        {
            shadowStrength = float.Parse(shadowStrengthTextfield.value, CultureInfo.InvariantCulture);

            if (shadowStrength is < 0 or > 1)
            {
                popupMenu.CreateInfoPopup("The shadow strength value must be between 0 and 1.");
                return;
            }
        }
        catch (FormatException)
        {
            popupMenu.CreateInfoPopup("The shadow strength must be a float.");
            return;
        }

        int imageSupersize;
        try
        {
            imageSupersize = int.Parse(this.imageSupersize.value, CultureInfo.InvariantCulture);

            if (imageSupersize < 0)
            {
                popupMenu.CreateInfoPopup("The image supersize value must be greater than 0.");
                return;
            }
        }
        catch (FormatException)
        {
            popupMenu.CreateInfoPopup("The image supersize must be an integer.");
            return;
        }

        Preferences.ShadowStrength = shadowStrength;
        Preferences.ImageSupersize = imageSupersize;
        Preferences.LightIntensity = lightIntensity;

        lightManager.UpdateLightIntensity();
        lightManager.UpdateShadowStrength();

        Preferences.CaptureImagePath = imagePathButtonValue;

        Close();
    }

    /// <summary>
    ///     Close the settings, clearing them.
    /// </summary>
    public void Close()
    {
        _root.visible = false;
        pauseMenu.UnpauseType(PauseType.Settings);

        SetToDefault();
    }

    /// <summary>
    ///     Clear the settings.
    /// </summary>
    private void SetToDefault()
    {
        lightIntensityTextfield.SetValueWithoutNotify(Preferences.LightIntensity.ToString());
        shadowStrengthTextfield.SetValueWithoutNotify(Preferences.ShadowStrength.ToString());
        imageSupersize.SetValueWithoutNotify(Preferences.ImageSupersize.ToString());

        imagePathButton.text = Preferences.CaptureImagePath;

        if (imagePathButton.text.Length > 20)
            imagePathButton.text = imagePathButton.text[..10] + "..." + imagePathButton.text[^10..];
    }

    /// <summary>
    ///     Show the settings.
    /// </summary>
    public void Show()
    {
        _root.visible = true;
        SetToDefault();

        pauseMenu.PauseType(PauseType.Settings);
    }
}