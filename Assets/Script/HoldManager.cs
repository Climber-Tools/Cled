using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dummiesman;
using UnityEngine;
using YamlDotNet.Serialization;
using Color = System.Drawing.Color;

/// <summary>
/// A class for storing the information about the given hold.
/// </summary>
public class HoldInformation
{
    public Color color;
    public string type;
    public string manufacturer;
    public string[] labels;
    public DateTime date;
}

/// <summary>
/// A class for storing both the hold information and the hold model.
/// </summary>
public class HoldBlueprint
{
    public readonly GameObject Model;
    public readonly HoldInformation HoldInformation;

    public HoldBlueprint(GameObject model, HoldInformation holdInformation)
    {
        Model = model;
        HoldInformation = holdInformation;
    }
}

/// <summary>
/// A class that handles various hold-related things, such as loading them, filtering them,
/// managing the ones that are selected and creating hold objects out of them.
/// </summary>
public class HoldManager : MonoBehaviour
{
    private readonly IDictionary<string, HoldBlueprint> _holds = new Dictionary<string, HoldBlueprint>();

    public readonly string ModelsFolder = Path.Combine("Models", "Holds");
    public readonly string HoldsYamlName = "holds.yaml";

    void Start()
    {
        string yml = File.ReadAllText(Path.Combine(ModelsFolder, HoldsYamlName));

        var deserializer = new DeserializerBuilder().Build();

        var holdInformation = deserializer.Deserialize<Dictionary<string, HoldInformation>>(yml);

        foreach (var pair in holdInformation)
        {
            var hold = new HoldBlueprint(ToGameObject(pair.Key), pair.Value);
            hold.Model.SetActive(false);

            _holds[pair.Key] = hold;
        }
    }

    /// <summary>
    /// Return a set of holds, given a filter delegate.
    /// </summary>
    /// <returns></returns>
    public HoldBlueprint[] Filter(Func<HoldInformation, bool> filter) => (from holdId in _holds.Keys
            where filter(_holds[holdId].HoldInformation)
            select _holds[holdId])
        .ToArray();

    /// <summary>
    /// Generate a 3D object from the given hold.
    /// </summary>
    private GameObject ToGameObject(string id)
    {
        FileStream modelStream = File.OpenRead(GetHoldModelPath(id));
        FileStream textureStream = File.OpenRead(GetHoldMaterialPath(id));

        GameObject hold = new OBJLoader().Load(modelStream, textureStream);

        MeshFilter mf = hold.transform.GetChild(0).GetComponent<MeshFilter>();
        MeshCollider meshCollider = hold.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mf.mesh;

        return hold;
    }

    /// <summary>
    /// Return the path of the .obj file of the hold.
    /// </summary>
    private string GetHoldModelPath(string id) => Path.Combine(ModelsFolder, id + ".obj");

    /// <summary>
    /// Return the path of the .mtl file of the hold.
    /// </summary>
    private string GetHoldMaterialPath(string id) => Path.Combine(ModelsFolder, id + ".mtl");
}