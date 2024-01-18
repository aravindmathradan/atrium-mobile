using System.Collections.Generic;
using System.Linq;
using GLTFast;
using UnityEngine;
using static TimeLogger;

public class GLTFLoader : MonoBehaviour {
  private GltfImport gltf;
  private ImportSettings settings;
  private BuildingMap buildingMap;
  public GameObject terrainGameObject;
  private TerrainProcessor terrainProcessor;
  public static Bounds sceneBounds;

  private void Awake() {
    terrainProcessor = terrainGameObject.GetComponent<TerrainProcessor>();
    buildingMap = new BuildingMap();
  }

  private void Start() {
    Building currentBuilding = buildingMap.GetCurrentBuilding();
    if (currentBuilding.isValid) {
      ImportModel(currentBuilding.url);
    } else {
      // Test
      ImportModel("file:///C:/Users/Aravind/Desktop/gltf/interior/gltf/scene.gltf");
    }
  }

  private async void ImportModel(string url) {
    gltf = new GltfImport();
    settings = new ImportSettings {
      GenerateMipMaps = true,
      AnisotropicFilterLevel = 3,
      NodeNameMethod = NameImportMethod.OriginalUnique,
      AnimationMethod = AnimationMethod.None,
      DefaultMagFilterMode = GLTFast.Schema.Sampler.MagFilterMode.Linear,
      DefaultMinFilterMode = GLTFast.Schema.Sampler.MinFilterMode.Linear
    };

    var success = await gltf.Load(url, settings);
    if (success) {
      await gltf.InstantiateMainSceneAsync(gameObject.transform);
      TimeLogger.TimerStart();
      ProcessModelV2();
      TimeLogger.TimerEnd();
    } else {
      Debug.LogError("Loading glTF model failed");
    }
  }

  private void ProcessModel() {
    List<Bounds> bounds = new List<Bounds>();
    List<Transform> allChildrenObjects = GetComponentsInChildren<Transform>().ToList();

    foreach (var childObject in allChildrenObjects) {
      AttachColliders(childObject.gameObject);

      if (childObject.TryGetComponent<Renderer>(out Renderer renderer)) {
        bool skipBound = false;
        foreach (var bound in bounds) {
          if (bound.Contains(renderer.bounds.center)) {
            skipBound = true;
            break;
          }
        }
        if (!skipBound) {
          bounds.Add(renderer.bounds);
        }
      }
    }

    terrainProcessor.ClearTrees(bounds);
    terrainProcessor.FlattenTerrain(bounds);
    // terrainProcessor.PaintHoles(bounds);
  }

  private void ProcessModelV2() {
    List<Renderer> allChildrenRenderers = GetComponentsInChildren<Renderer>().ToList();

    Vector3 sceneCenter = Vector3.zero;
    foreach (Renderer childRenderer in allChildrenRenderers) {
      AttachColliders(childRenderer.gameObject);
      sceneCenter += childRenderer.bounds.center;
    }
    sceneCenter /= allChildrenRenderers.Count;
    sceneBounds = CalculateSceneBounds(allChildrenRenderers, sceneCenter);
    terrainProcessor.ClearTreesV2(sceneBounds);
    terrainProcessor.FlattenTerrainV2(sceneBounds);
    // terrainProcessor.PaintHoles(bounds);
  }

  private Bounds CalculateSceneBounds(List<Renderer> allChildrenRenderers, Vector3 center) {
    Bounds newBounds = new Bounds(center, Vector3.zero);
    foreach (Renderer childRenderer in allChildrenRenderers) {
      newBounds.Encapsulate(childRenderer.bounds);
    }
    return newBounds;
  }

  private void AttachColliders(GameObject childObject) {
    if (childObject.TryGetComponent<MeshFilter>(out MeshFilter meshFilter) && meshFilter.mesh.vertexCount > 3) {
      MeshCollider meshCollider = childObject.AddComponent<MeshCollider>();
      meshCollider.sharedMesh = meshFilter.mesh;
    }
  }

  private void OnDestroy() {
    // terrainProcessor.ResetTerrain();
  }
}
