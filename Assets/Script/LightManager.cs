using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    public GameObject PlayerLight;

    private readonly List<GameObject> _lights = new();
    private readonly List<Action<bool>> _playerLightWatchers = new();

    private bool _playerLightEnabled;

    public bool PlayerLightEnabled
    {
        get => _playerLightEnabled;
        set
        {
            _playerLightEnabled = value;
            
            PlayerLight.GetComponent<Light>().enabled = value;
            
            foreach (var action in _playerLightWatchers)
                action(value);
        }
    }
    
    public void AddPlayerLightWatcher(Action<bool> action) => _playerLightWatchers.Add(action);

    /// <summary>
    /// Clear all lights.
    /// </summary>
    public void Clear()
    {
        foreach(GameObject light in _lights)
            Destroy(light);
        
        _lights.Clear();
        PlayerLightEnabled = true;
    }

    /// <summary>
    /// Get the position of all of the lights.
    /// </summary>
    /// <returns></returns>
    public List<Vector3> GetPositions() => _lights.Select(x => x.transform.position).ToList();

    private float _intensity = 0.2f;

    /// <summary>
    /// The intensity of the lights.
    /// </summary>
    public float Intensity
    {
        get => _intensity;
        set
        {
            _applyActionToLights(light => { light.intensity = value; });
            _intensity = value;
        }
    }

    private float _shadowStrength = 0.2f;

    /// <summary>
    /// The strength of the shadows.
    /// </summary>
    public float ShadowStrength
    {
        get => _shadowStrength;
        set
        {
            _applyActionToLights(light => { light.shadowStrength = value; });
            _intensity = value;
        }
    }

    /// <summary>
    /// Apply a given action to all of the lights.
    /// </summary>
    private void _applyActionToLights(Action<Light> lightFunction)
    {
        lightFunction(PlayerLight.GetComponent<Light>());

        foreach (var light in _lights)
            lightFunction(light.GetComponent<Light>());
    }

    /// <summary>
    /// Add a new light at the player's location.
    /// </summary>
    public void AddLight() => AddLight(transform.position);

    /// <summary>
    /// Add a new light at the given location.
    /// </summary>
    public void AddLight(Vector3 position)
    {
        // copy the player light
        GameObject lightGameObject = Instantiate(PlayerLight);
        lightGameObject.GetComponent<Light>().enabled = true;
        lightGameObject.transform.position = position;
        
        _lights.Add(lightGameObject);
    }

    void Update()
    {
        // move to player
        PlayerLight.transform.position = transform.position;

        // don't move the camera when time doesn't run
        if (Time.timeScale == 0)
            return;

        // place light
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F))
            AddLight();

        // toggle player light
        else if (Input.GetKeyDown(KeyCode.F))
            PlayerLightEnabled = !PlayerLightEnabled;
    }
}