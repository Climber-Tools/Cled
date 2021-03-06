using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
///     A class for a collection of holds that form it.
/// </summary>
public class Route
{
    private readonly Dictionary<GameObject, HoldBlueprint> _holds = new();

    private readonly List<Action> _routesChangedCallbacks;

    private string _grade;

    private string _name;

    private string _setter;

    private string _zone;

    public Route(List<Action> routeChangedCallbacks)
    {
        _routesChangedCallbacks = routeChangedCallbacks;
    }

    public string Name
    {
        get => _name;
        set
        {
            RoutesChanged();
            _name = value;
        }
    }

    public string Grade
    {
        get => _grade;
        set
        {
            RoutesChanged();
            _grade = value;
        }
    }

    public string Zone
    {
        get => _zone;
        set
        {
            RoutesChanged();
            _zone = value;
        }
    }

    public string Setter
    {
        get => _setter;
        set
        {
            RoutesChanged();
            _setter = value;
        }
    }

    /// <summary>
    ///     Get the holds of the route.
    /// </summary>
    public GameObject[] Holds => _holds.Keys.ToArray();

    /// <summary>
    ///     Calls route changed callbacks; called when a route is changed.
    /// </summary>
    private void RoutesChanged()
    {
        foreach (var callback in _routesChangedCallbacks)
            callback();
    }

    /// <summary>
    ///     Add a hold to the route.
    /// </summary>
    public void AddHold(GameObject hold, HoldBlueprint blueprint)
    {
        _holds[hold] = blueprint;

        RoutesChanged();
    }

    /// <summary>
    ///     Remove a hold from the route.
    /// </summary>
    public void RemoveHold(GameObject hold)
    {
        _holds.Remove(hold);

        RoutesChanged();
    }

    /// <summary>
    ///     Toggle a hold being in this route.
    /// </summary>
    public void ToggleHold(GameObject hold, HoldBlueprint blueprint)
    {
        if (ContainsHold(hold))
            RemoveHold(hold);
        else
            AddHold(hold, blueprint);
    }

    /// <summary>
    ///     Return true if the route contains this hold, else return false.
    /// </summary>
    public bool ContainsHold(GameObject hold)
    {
        return _holds.ContainsKey(hold);
    }

    /// <summary>
    ///     Return True if the given route is empty.
    /// </summary>
    public bool IsEmpty()
    {
        return Holds.Length == 0;
    }
}

/// <summary>
///     A class for managing all currently active routes.
///     The routes themselves can contain any number of holds.
///     However, if a hold is right click selected, it will automatically create a route of one hold.
///     This route will, however, not be a real route, even if it is implemented as one, since lone holds don't substitute
///     routes.
/// </summary>
public class RouteManager : MonoBehaviour, IResetable
{
    public GameObject StartMarkerPrefab;
    public GameObject EndMarkerPrefab;
    private readonly HashSet<Route> _routes = new();

    private readonly List<Action> _routesChangedCallbacks = new();
    private readonly List<Action> _selectedRouteChangedCallbacks = new();

    public readonly HashSet<GameObject> EndingHolds = new();
    public readonly HashSet<GameObject> StartingHolds = new();

    private Route _selectedRoute;

    public Route SelectedRoute
    {
        get => _selectedRoute;
        set
        {
            _selectedRoute = value;

            foreach (var callback in _selectedRouteChangedCallbacks)
                callback();
        }
    }

    public void Reset()
    {
        _routes.Clear();

        SelectedRoute = null;

        StartingHolds.Clear();
        EndingHolds.Clear();
    }

    /// <summary>
    ///     Add a callback for when the routes change, either by one being added, removed, or edited.
    /// </summary>
    public void AddRoutesChangedCallback(Action action)
    {
        _routesChangedCallbacks.Add(action);
    }

    /// <summary>
    ///     Add a callback for when the selected route is changed.
    /// </summary>
    public void AddSelectedRouteChangedCallback(Action action)
    {
        _selectedRouteChangedCallbacks.Add(action);
    }

    /// <summary>
    ///     Get all routes.
    /// </summary>
    public IEnumerable<Route> GetRoutes()
    {
        return _routes;
    }

    /// <summary>
    ///     Get only routes that are "usable."
    ///     By this we mean routes that are interesting to the user.
    ///     They must either contain more than one hold, contain a top or bottom, or have some attribute be not null or empty.
    /// </summary>
    public IEnumerable<Route> GetUsableRoutes()
    {
        return GetRoutes().Where(route =>
            route.Holds.Length > 1 || GetRouteStartingHolds(route).Count() != 0 ||
            GetRouteEndingHolds(route).Count() != 0
            || !string.IsNullOrEmpty(route.Setter)
            || !string.IsNullOrEmpty(route.Name)
            || !string.IsNullOrEmpty(route.Zone)
            || !string.IsNullOrEmpty(route.Grade));
    }

    /// <summary>
    ///     Add the marker to the hold.
    /// </summary>
    private void AddMarker(GameObject hold, GameObject marker)
    {
        var markerInstance = Instantiate(marker, hold.transform);
        markerInstance.name = "Marker";
        markerInstance.SetActive(true);
        markerInstance.transform.localPosition = Vector3.zero;

        var customUpdate = markerInstance.AddComponent<MarkerUpdate>();

        void CustomUpdateFunction()
        {
            Physics.SyncTransforms();
            
            var holdCollider = hold.GetComponent<MeshCollider>();
            
            // only update when hold position or rotation changed
            if (customUpdate.LastPosition == hold.transform.position &&
                customUpdate.LastRotation == hold.transform.rotation &&
                customUpdate.LastScale == hold.transform.localScale) return;

            var holdPosition = hold.transform.position;

            customUpdate.LastPosition = holdPosition;
            customUpdate.LastRotation = hold.transform.rotation;
            customUpdate.LastScale = hold.transform.localScale;

            markerInstance.transform.position = holdPosition;
            markerInstance.transform.LookAt(hold.transform.forward + holdPosition, Vector3.up);

            // raycast from the center of the hold and move the marker accordingly
            // we have to temporarily allow hitting backfaces
            var temp = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            // the ray must start inside the hold so we move it slightly upward
            Ray ray = new Ray(holdPosition + hold.transform.forward.normalized / 200,
                -markerInstance.transform.up);

            // allow for hitting backfaces, since we're casting from inside the object
            if (holdCollider.Raycast(ray, out RaycastHit hit, 100f))
            {
                markerInstance.transform.position = hit.point;
                
                // correct for raycast offset
                markerInstance.transform.position -= hold.transform.forward.normalized / 200;
            }

            Physics.queriesHitBackfaces = temp;

            // move up slightly
            markerInstance.transform.position += markerInstance.transform.up * 0.01f;
        }

        customUpdate.OnUpdate += CustomUpdateFunction;
        CustomUpdateFunction();
    }

    /// <summary>
    ///     Remove the hold's marker.
    /// </summary>
    private void RemoveMarker(GameObject hold)
    {
        DestroyImmediate(hold.transform.GetChild(hold.transform.childCount - 1).gameObject);
    }

    /// <summary>
    ///     Return the starting holds of a route.
    /// </summary>
    public IEnumerable<GameObject> GetRouteStartingHolds(Route route)
    {
        return route.Holds.Where(x => StartingHolds.Contains(x));
    }

    /// <summary>
    ///     Return the ending holds of a route.
    /// </summary>
    public IEnumerable<GameObject> GetRouteEndingHolds(Route route)
    {
        return route.Holds.Where(x => EndingHolds.Contains(x));
    }

    /// <summary>
    ///     Select the given route, possibly triggering the callbacks that go along with it.
    /// </summary>
    public void SelectRoute(Route route, bool triggerCallbacks = true)
    {
        if (triggerCallbacks)
            SelectedRoute = route;
        else
            _selectedRoute = route;
    }

    /// <summary>
    ///     Deselect the currently selected route.
    /// </summary>
    public void DeselectRoute()
    {
        SelectedRoute = null;
    }

    /// <summary>
    ///     Toggle a starting hold of the route.
    /// </summary>
    public void ToggleStarting(GameObject hold, HoldBlueprint blueprint)
    {
        // if it's already ending, make it not so
        if (EndingHolds.Contains(hold))
            ToggleEnding(hold, blueprint);

        if (StartingHolds.Contains(hold))
        {
            RemoveMarker(hold);
            StartingHolds.Remove(hold);
        }
        else
        {
            AddMarker(hold, StartMarkerPrefab);
            StartingHolds.Add(hold);
        }

        RoutesChanged();
    }

    /// <summary>
    ///     Toggle a starting hold of the route.
    /// </summary>
    public void ToggleEnding(GameObject hold, HoldBlueprint blueprint)
    {
        // if it's already starting, make it not so
        if (StartingHolds.Contains(hold))
            ToggleStarting(hold, blueprint);

        if (EndingHolds.Contains(hold))
        {
            RemoveMarker(hold);
            EndingHolds.Remove(hold);
        }
        else
        {
            AddMarker(hold, EndMarkerPrefab);
            EndingHolds.Add(hold);
        }

        RoutesChanged();
    }

    /// <summary>
    ///     Toggle a hold being in a route, possibly removing it from other routes.
    /// </summary>
    public void ToggleHold(Route route, GameObject hold, HoldBlueprint blueprint)
    {
        // the route the hold was originally in
        var originalRoute = GetRouteWithHold(hold);

        // if we're removing it from a route, simply toggle it
        if (route == originalRoute)
        {
            route.ToggleHold(hold, blueprint);
        }

        // if we're adding it to a different one, remove it from the original (if it was in one) and add it to the new
        else
        {
            if (originalRoute != null)
            {
                RemoveHold(hold);

                if (originalRoute.IsEmpty())
                    RemoveRoute(originalRoute);
            }

            route.AddHold(hold, blueprint);
        }

        RoutesChanged();
    }

    /// <summary>
    ///     Remove the given hold from the route it is in (if it is in one).
    ///     Also remove it from starting and ending holds.
    /// </summary>
    public void RemoveHold(GameObject hold)
    {
        // remove from starting and ending!
        StartingHolds.Remove(hold);
        EndingHolds.Remove(hold);

        var route = GetRouteWithHold(hold);

        if (route == null)
            return;

        route.RemoveHold(hold);

        if (route.IsEmpty())
            RemoveRoute(route);
    }

    /// <summary>
    ///     Create a new route, adding it to the manager.
    /// </summary>
    public Route CreateRoute()
    {
        var newRoute = new Route(_routesChangedCallbacks);
        _routes.Add(newRoute);

        RoutesChanged();

        return newRoute;
    }

    /// <summary>
    ///     Calls route changed callbacks; called when a route is changed.
    /// </summary>
    private void RoutesChanged()
    {
        foreach (var callback in _routesChangedCallbacks)
            callback();
    }

    /// <summary>
    ///     Return the route with the given hold, returning null if no such route exists.
    /// </summary>
    public Route GetRouteWithHold(GameObject hold)
    {
        return _routes.FirstOrDefault(route => route.ContainsHold(hold));
    }

    /// <summary>
    ///     Return the route with the given hold, or create one if no such exists.
    /// </summary>
    public Route GetOrCreateRouteWithHold(GameObject hold, HoldBlueprint blueprint)
    {
        var route = GetRouteWithHold(hold);

        if (route != null)
            return route;

        route = CreateRoute();
        route.AddHold(hold, blueprint);

        RoutesChanged();

        return route;
    }

    /// <summary>
    ///     Remove a route from the manager.
    /// </summary>
    private void RemoveRoute(Route route)
    {
        _routes.Remove(route);

        RoutesChanged();
    }

    /// <summary>
    ///     A component for updating the marker.
    ///     It remembers the last hold position and rotation to only update the marker if necessary.
    /// </summary>
    public class MarkerUpdate : MonoBehaviour
    {
        public Vector3 LastPosition;
        public Quaternion LastRotation;
        public Vector3 LastScale;

        private void LateUpdate() => OnUpdate?.Invoke();   

        public event Action OnUpdate;
    }
}