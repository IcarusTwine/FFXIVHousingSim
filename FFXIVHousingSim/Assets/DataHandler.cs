﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using FFXIVHSLib;
using Newtonsoft.Json;
using Object = System.Object;
using Transform = UnityEngine.Transform;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

/// <summary>
/// Handles all extracted FFXIV data.
/// </summary>
public static class DataHandler
{
	private static bool DebugLoadMap = true;
	private static bool DebugCustomLoad = false;
	private static bool DebugLoadExteriors = false;
	
//	private static int[] debugCustomLoadList =
//	{
//		0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30
//	};
	
	private static int[] debugCustomLoadList =
	{
		126
	};
	
    //Serialized FFXIV data
    private static List<Plot> _wardInfo;
	private static HousingExteriorStructure[] _landSet;
    private static Dictionary<int, HousingExteriorFixture> _exteriorFixtures;
    private static HousingExteriorBlueprintSet _blueprints;
    private static Map _map;
    
    //Extracted model handling
    private static Dictionary<int, Mesh[]> _modelMeshes;
	private static Dictionary<int, Mesh[][][]> _exteriorFixtureMeshes;
	private static Dictionary<int, FFXIVHSLib.Transform[][]> _exteriorFixtureMeshTransforms;

    private static Territory _territory = (Territory) 999;
    private static string _teriStr;

    public static string teriStr
    {
        get { return _teriStr; }
        set
        {
            if (_teriStr == value)
                return;

            _teriStr = value;

            Debug.LogFormat("Territory changed to {0}", value);

            //TODO: When events implemented make this an event
            //CameraHandler[] c = Resources.FindObjectsOfTypeAll<CameraHandler>();
            //c[0]._territory = null;

            GameObject[] currentGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            //Destroy old objects
            if (currentGameObjects.Length > defaultGameObjects.Length)
                foreach (GameObject obj in currentGameObjects)
                    if (!defaultGameObjects.Contains(obj))
                        UnityEngine.Object.Destroy(obj);

            LoadTerritory();
        }
    }
    public static Territory territory
    {
        get { return _territory; }
        set
        {
	        if (_territory == value)
		        return;
	        
			_territory = value;
	        Debug.LogFormat("Territory changed to {0}", value.ToString());
	        
	        //TODO: When events implemented make this an event
	        CameraHandler[] c = Resources.FindObjectsOfTypeAll<CameraHandler>();
	        c[0]._territory = value;
			
			GameObject[] currentGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
	
			//Destroy old objects
			if (currentGameObjects.Length > defaultGameObjects.Length)
				foreach (GameObject obj in currentGameObjects)
					if (!defaultGameObjects.Contains(obj))
						UnityEngine.Object.Destroy(obj);

			LoadTerritory();
        }
    }
    
    public static GameObject[] defaultGameObjects { get; set; }
    
    private static void LoadWardInfo()
    {
        string jsonText = File.ReadAllText(FFXIVHSPaths.GetWardInfoJson());

        _wardInfo = JsonConvert.DeserializeObject<List<Plot>>(jsonText);
    }

    private static void LoadExteriorFixtures()
    {
        string jsonText = File.ReadAllText(FFXIVHSPaths.GetHousingExteriorJson());

        _exteriorFixtures = JsonConvert.DeserializeObject<Dictionary<int, HousingExteriorFixture>>(jsonText);
    }

    private static void LoadExteriorBlueprints()
    {
        string jsonText = File.ReadAllText(FFXIVHSPaths.GetHousingExteriorBlueprintSetJson());

        _blueprints = JsonConvert.DeserializeObject<HousingExteriorBlueprintSet>(jsonText);
    }

	private static void LoadLandset()
	{
		if (!DebugLoadExteriors)
			return;
			
		if (_exteriorFixtures == null)
			LoadExteriorFixtures();
		
		if (_blueprints == null)
			LoadExteriorBlueprints();

		string landSetPath = FFXIVHSPaths.GetWardLandsetJson(territory);

		if (!File.Exists(landSetPath))
		{
			//Main and subdivision, 60 plots
			_landSet = new HousingExteriorStructure[60];
			for (int i = 0; i < _landSet.Length; i++)
			{
				_landSet[i] = new HousingExteriorStructure();
				int numFixtureTypes = Enum.GetValues(typeof(FixtureType)).Length;
				_landSet[i].fixtures = new int[numFixtureTypes];
			}
				
			string jsonText = JsonConvert.SerializeObject(_landSet, Formatting.Indented);
			File.WriteAllText(landSetPath, jsonText);
		}
		else
		{
			string jsonText = File.ReadAllText(landSetPath);
			_landSet = JsonConvert.DeserializeObject<HousingExteriorStructure[]>(jsonText);
		}
		
		//TODO: move this, rewrite this ?
		for (int plotIndex = 0; plotIndex < _landSet.Length; plotIndex++)
		{
			Plot plotAt = GetPlot(_territory, plotIndex % 30 + 1, plotIndex > 29);
			if (_landSet[plotIndex].size == Size.s)
			{
				//Verify possibly unset size
				_landSet[plotIndex].size = plotAt.size;
			}

			if (_landSet[plotIndex].fixtures[(int) FixtureType.fnc - 1] == 0)
				_landSet[plotIndex].fixtures[(int) FixtureType.fnc - 1] = DefaultFences.fnc[(int) _territory];

			HousingExteriorBlueprint blueprint = _blueprints.set[(int) _landSet[plotIndex].size];
			
			//TODO: If you ever figure out how to tell which transforms are for which house size, fix this
			string groupName = "{0}_{1:D2}_{2}_house";
			string fixedWardName = _territory.ToString().ToLower().Substring(0, _territory.ToString().Length - 1) + '0';
			string strSize = _landSet[plotIndex].size.ToString();

			groupName = string.Format(groupName, fixedWardName, plotIndex + 1, strSize);
			Debug.Log(groupName);
			GameObject parentPlotObject = GameObject.Find(groupName);
			
			//For each fixture in our landset element
			for (int fixtureIndex = 0; fixtureIndex < _landSet[plotIndex].fixtures.Length; fixtureIndex++)
			{
				if (_landSet[plotIndex].fixtures[fixtureIndex] == 0)
					continue;
				
				FixtureType fixtureType = (FixtureType) fixtureIndex + 1;

				FFXIVHSLib.Transform[][] transformsForModels = null;
				Mesh[][][] meshes = GetMeshesForExteriorFixture(_landSet[plotIndex].fixtures[fixtureIndex], ref transformsForModels);

				//For each variant
				for (int variantIndex = 0; variantIndex < meshes.Length; variantIndex++)
				{
					if (blueprint.fixtureTransforms[fixtureType][variantIndex] == null ||
					    meshes[variantIndex] == null)
						continue;
					
					//The set of gameobjects for this variant, 1 gameobject per model
					GameObject[] objects = new GameObject[meshes[variantIndex].Length];
					
					for (int modelIndex = 0; modelIndex < objects.Length; modelIndex++)
					{
						objects[modelIndex] = AddMeshToNewGameObject(meshes[variantIndex][modelIndex]);
					}
					
					foreach (FFXIVHSLib.Transform t in blueprint.fixtureTransforms[fixtureType][variantIndex])
					{
						GameObject variantBaseObject = new GameObject();
						variantBaseObject.GetComponent<Transform>().SetParent(parentPlotObject.transform);
						variantBaseObject.GetComponent<Transform>().localPosition = t.translation;
						variantBaseObject.GetComponent<Transform>().localRotation = t.rotation;
						variantBaseObject.GetComponent<Transform>().localScale = t.scale;
						variantBaseObject.name = string.Format("bp{0}_ft{1}_v{2}", _landSet[plotIndex].size, fixtureType, variantIndex);
						
						for (int modelIndex = 0; modelIndex < objects.Length; modelIndex++)
						{
							if (objects[modelIndex] == null)
								continue;
							
							FFXIVHSLib.Transform modelTransform = transformsForModels[variantIndex][modelIndex];

							GameObject addedModel = UnityEngine.Object.Instantiate(objects[modelIndex]);
							addedModel.GetComponent<Transform>().SetParent(variantBaseObject.GetComponent<Transform>());
							addedModel.GetComponent<Transform>().localPosition = modelTransform.translation;
							addedModel.GetComponent<Transform>().localRotation = modelTransform.rotation;
							addedModel.GetComponent<Transform>().localScale = modelTransform.scale;
							addedModel.name = addedModel.name.Replace("(Clone)", "_") + string.Format("{0}_{1}_{2}", fixtureIndex, variantIndex, modelIndex);
							addedModel.SetActive(true);
							
							UnityEngine.Object.Destroy(objects[modelIndex]);
						}
					}
				}
			}
		}
	}

    private static void LoadMapTerrainInfo()
    {
        UnityEngine.Debug.Log(teriStr);//
        string jsonText = File.ReadAllText(FFXIVHSPaths.GetTerritoryJson(teriStr));
        //
        _map = JsonConvert.DeserializeObject<Map>(jsonText);
	    Debug.Log("_map loaded.");
    }

    private static void LoadMapMeshes()
    {
        LoadMapTerrainInfo();

        _modelMeshes = new Dictionary<int, Mesh[]>();

	    string objectsFolder = FFXIVHSPaths.GetTerritoryObjectsDirectory(_teriStr);
        
        foreach (MapModel model in _map.models.Values)
        {
	        if (DebugCustomLoad)
		        if (!debugCustomLoadList.Contains(model.id))
			        continue;
	        
            Mesh[] modelMeshes = new Mesh[model.numMeshes];

            for (int i = 0; i < model.numMeshes; i++)
            {
                string meshFileName = string.Format("{0}{1}_{2}.obj", objectsFolder, model.modelName, i);
	            modelMeshes[i] = FastObjImporter.Instance.ImportFile(meshFileName);
            }
            _modelMeshes.Add(model.id, modelMeshes);
        }
	    Debug.Log("_modelMeshes loaded.");
    }
    
    private static void LoadTerritory()
    {
	    if (DebugLoadMap)
	    {
		    LoadMapMeshes();
		
		    foreach (MapGroup group in _map.groups.Values)
			    LoadMapGroup(group);
	    }
        //LoadLights();
	    //LoadLandset();
    }

    private static void LoadLight(MapLightEntry light)
    {
        
    }

    private static void LoadMapGroup(MapGroup group, GameObject parent = null)
	{
		if (group.groupName.Contains("fnc0000"))
			return;
		
		GameObject groupRootObject = new GameObject(group.groupName);

		if (parent == null)
		{
			groupRootObject.GetComponent<Transform>().position = Vector3.Reflect(group.groupTransform.translation, Vector3.left);
			groupRootObject.GetComponent<Transform>().rotation = Quaternion.Euler(Vector3.Reflect(group.groupTransform.rotation.ToVector3(), Vector3.left));
			groupRootObject.GetComponent<Transform>().localScale = Vector3.Reflect(group.groupTransform.scale, Vector3.left);
		}
		else
		{
			groupRootObject.GetComponent<Transform>().SetParent(parent.GetComponent<Transform>());
			groupRootObject.GetComponent<Transform>().localPosition = group.groupTransform.translation;
			groupRootObject.GetComponent<Transform>().localRotation = group.groupTransform.rotation;
			groupRootObject.GetComponent<Transform>().localScale = group.groupTransform.scale;
		}
		
		groupRootObject.SetActive(true);
		
		if (group.entries != null && group.entries.Count > 0)
		{
			foreach (MapModelEntry entry in group.entries)
			{
				if (DebugCustomLoad)
					if (!debugCustomLoadList.Contains(entry.modelId))
						continue;
				
				Mesh[] meshes = _modelMeshes[entry.modelId];
				GameObject obj = AddMeshToNewGameObject(meshes, true);

				obj.GetComponent<Transform>().SetParent(groupRootObject.GetComponent<Transform>());
				obj.GetComponent<Transform>().localPosition = entry.transform.translation;
				obj.GetComponent<Transform>().localRotation = entry.transform.rotation;
				obj.GetComponent<Transform>().localScale = entry.transform.scale;
				obj.SetActive(true);
			}	
		}

        if (group.lights != null && group.lights.Count > 0)
        {
            foreach (MapLightEntry entry in group.lights)
            {
                UnityEngine.GameObject obj = new UnityEngine.GameObject("LIGHT_" + entry.id + "_" + entry.layerId);

                obj.GetComponent<Transform>().SetParent(groupRootObject.GetComponent<Transform>());
                obj.GetComponent<Transform>().localPosition = entry.transform.translation;
                obj.GetComponent<Transform>().localRotation = entry.transform.rotation;
                obj.GetComponent<Transform>().localScale = entry.transform.scale;

                UnityEngine.Light light = obj.AddComponent<UnityEngine.Light>();
                float x = entry.transform.scale.x;
                
                if (x != 1.0f && x != -1.0f)
                    light.areaSize = new UnityEngine.Vector2(entry.transform.scale.x, entry.transform.scale.y);

                // LightType
                {
                    if (entry.lightType == "Point")
                        light.type = UnityEngine.LightType.Point;
                    else if (entry.lightType == "Spot")
                        light.type = UnityEngine.LightType.Spot;
                    else if (entry.lightType == "Directional")
                        light.type = UnityEngine.LightType.Directional;
                    else if (entry.lightType == "Line" || entry.lightType == "Plane")
                        light.type = UnityEngine.LightType.Rectangle;
                    else
                        light.type = UnityEngine.LightType.Point;

                    /*  Directional, Point, Spot, Plane, Line, FakeSpecular, */
                }

                // Attenuation
                // RangeRate
                // PointLightType
                // AttenuationConeCoefficient
                // ConeDegree
                light.spotAngle = entry.coneDegree;

                // ColorHDRI
                float intensity = entry.colorIntensity;
                float a = entry.color.alpha / 255.0f;
                float b = entry.color.blue / 255.0f;
                float g = entry.color.green / 255.0f;
                float r = entry.color.red / 255.0f;

                light.color = new UnityEngine.Color(r, g, b, a);
                light.intensity = intensity * 1.25f;

                // FollowsDirectionalLight
                // SpecularEnabled
                // BGShadowEnabled

                // ShadowClipRange
                light.shadowNearPlane = entry.shadowClipRange;

                obj.SetActive(true);
            }
        }

        if (group.vfx != null && group.vfx.Count > 0)
        {
            foreach (MapVfxEntry entry in group.vfx)
            {
                Mesh[] meshes = _modelMeshes[entry.modelId];
                GameObject obj = AddMeshToNewGameObject(meshes, true);

                obj.name = ("VFX_" + entry.id + "_" + entry.layerId + "_" + System.IO.Path.GetFileNameWithoutExtension(entry.avfxPath));

                obj.GetComponent<Transform>().SetParent(groupRootObject.GetComponent<Transform>());
                obj.GetComponent<Transform>().localPosition = entry.transform.translation;
                obj.GetComponent<Transform>().localRotation = entry.transform.rotation;
                obj.GetComponent<Transform>().localScale = entry.transform.scale;

                UnityEngine.Light light = obj.AddComponent<UnityEngine.Light>();
                float x = entry.transform.scale.x;

                if (x != 1.0f && x != -1.0f)
                    light.areaSize = new UnityEngine.Vector2(entry.transform.scale.x, entry.transform.scale.y);

                //float intensity = entry.colorIntensity;
                float a = entry.color.alpha / 255.0f;
                float b = entry.color.blue / 255.0f;
                float g = entry.color.green / 255.0f;
                float r = entry.color.red / 255.0f;

                light.color = new UnityEngine.Color(r, g, b, a);
                light.intensity = 1.25f;
                light.type = UnityEngine.LightType.Point;

                obj.SetActive(true);
            }
        }

		if (group.groups != null && group.groups.Count > 0)
		{
			foreach (MapGroup subGroup in group.groups)
			{
				if (subGroup != null)
					LoadMapGroup(subGroup, groupRootObject);
			}	
		}
	}

	private static void AddMeshToGameObject(Mesh[] meshes, GameObject obj)
	{
		Renderer objRenderer = obj.GetComponent<Renderer>();
		Material[] mats = new Material[meshes.Length];
		MaterialHandler mtlHandler = MaterialHandler.GetInstance();
	
		for (int i = 0; i < meshes.Length; i++)
		{
			Material mat = mtlHandler.GetMaterialForMesh(meshes[i].name);
				
			if (mat == null)
			{
				Debug.LogFormat("Could not find material for mesh {0}", meshes[i].name);
				mats[i] = mtlHandler.GetMaterialForMesh("default");
			}
			else
				mats[i] = mat;
		}
		objRenderer.materials = mats;
			
		Mesh main = new Mesh();
		main.subMeshCount = meshes.Length;
			
		if (meshes.Length == 0)
		{
			obj.GetComponent<MeshFilter>().mesh = main;
		}
		else
		{
			CombineInstance[] combine = new CombineInstance[meshes.Length];
						
			for (int i = 0; i < meshes.Length; i++)
			{
				combine[i].mesh = meshes[i];
				combine[i].transform = Matrix4x4.identity;
			}
			main.CombineMeshes(combine);
		
			for (int i = 0; i < main.subMeshCount; i++)
			{
				int[] tri = new int[0];
		
				tri = meshes[i].triangles;
		
				int offset = 0;
				if (i > 0)
					for (int j = 0; j < i; j++)
						offset += meshes[j].vertexCount;
						
				main.SetTriangles(tri, i, false, offset);
					
				//Don't ask?
				if (main.subMeshCount != meshes.Length)
					main.subMeshCount = meshes.Length;
			}
		
			obj.GetComponent<MeshFilter>().mesh = main;
		}
	}
	
	private static GameObject AddMeshToNewGameObject(Mesh[] meshes, bool addMeshCollider = false, string name = null)
	{
		//Set up our gameobject and add a renderer and filter
		GameObject obj = new GameObject();
		obj.AddComponent<MeshRenderer>();
		obj.AddComponent<MeshFilter>();
		
		Renderer objRenderer = obj.GetComponent<Renderer>();
		Material[] mats = new Material[meshes.Length];
		MaterialHandler mtlHandler = MaterialHandler.GetInstance();
	
		for (int i = 0; i < meshes.Length; i++)
		{
			Material mat = mtlHandler.GetMaterialForMesh(meshes[i].name);
				
			if (mat == null)
			{
				Debug.LogFormat("Could not find material for mesh {0}", meshes[i].name);
				mats[i] = mtlHandler.GetMaterialForMesh("default");
			}
			else
				mats[i] = mat;
		}
		objRenderer.materials = mats;
			
		Mesh main = new Mesh();
		main.subMeshCount = meshes.Length;
			
		if (meshes.Length == 0)
		{
			obj.GetComponent<MeshFilter>().mesh = main;
		}
		else
		{
			CombineInstance[] combine = new CombineInstance[meshes.Length];
						
			for (int i = 0; i < meshes.Length; i++)
			{
				combine[i].mesh = meshes[i];
				combine[i].transform = Matrix4x4.identity;
			}
			main.CombineMeshes(combine);
		
			for (int i = 0; i < main.subMeshCount; i++)
			{
				int[] tri = meshes[i].triangles;
		
				int offset = 0;
				if (i > 0)
					for (int j = 0; j < i; j++)
						offset += meshes[j].vertexCount;
						
				main.SetTriangles(tri, i, false, offset);
					
				//Don't ask?
				if (main.subMeshCount != meshes.Length)
					main.subMeshCount = meshes.Length;
			}
		
			obj.GetComponent<MeshFilter>().mesh = main;
		}
		if (addMeshCollider)
			obj.AddComponent<MeshCollider>();

		string newName = "";
		//Redo this
		if (name != null)
			newName = name;
		else
			newName = meshes.Length > 0 ? meshes[0].name.Substring(0, meshes[0].name.Length - 2) : "Unknown";

		obj.name = newName;
		obj.SetActive(false);
		return obj;
	}
	
	private static Mesh[][][] GetMeshesForExteriorFixture(int fixtureId, ref FFXIVHSLib.Transform[][] transformsPerModel)
	{
		//A little different this time!
		if (_exteriorFixtureMeshes == null)
			_exteriorFixtureMeshes = new Dictionary<int, Mesh[][][]>();

		if (_exteriorFixtureMeshTransforms == null)
			_exteriorFixtureMeshTransforms = new Dictionary<int, FFXIVHSLib.Transform[][]>();

		Mesh[][][] modelMeshes;
		if (!_exteriorFixtureMeshes.TryGetValue(fixtureId, out modelMeshes))
		{
			string exteriorHousingObjectsFolder = FFXIVHSPaths.GetHousingExteriorObjectsDirectory();
			
			//Load the meshes if not found
			HousingExteriorFixture fixture = _exteriorFixtures[fixtureId];

			//Initialize variants dimensions
			int numVariants = HousingExteriorFixture.GetVariants(fixture.fixtureType);
			modelMeshes = new Mesh[numVariants][][];
			transformsPerModel = new FFXIVHSLib.Transform[numVariants][];

			int i = 0;
			foreach (HousingExteriorFixtureVariant variant in fixture.variants)
			{
				//Initialize model dimensions for this variant
				int numModels = variant.models.Length;
				modelMeshes[i] = new Mesh[numModels][];
				transformsPerModel[i] = new FFXIVHSLib.Transform[numModels];
				
				int j = 0;
				foreach (HousingExteriorFixtureModel model in variant.models)
				{
					modelMeshes[i][j] = new Mesh[model.numMeshes];
					transformsPerModel[i][j] = model.transform;
					
					for (int k = 0; k < model.numMeshes; k++)
					{
						string meshFileName = string.Format("{0}{1}_{2}.obj", exteriorHousingObjectsFolder, model.modelName, k);
						modelMeshes[i][j][k] = FastObjImporter.Instance.ImportFile(meshFileName);
					}
					j++;
				}
				i++;
			}
			_exteriorFixtureMeshes.Add(fixtureId, modelMeshes);
			_exteriorFixtureMeshTransforms.Add(fixtureId, transformsPerModel);
		}
		else
		{
			//If the meshes are in the dictionary, so are the transforms :p
			transformsPerModel = _exteriorFixtureMeshTransforms[fixtureId];
		}
		return modelMeshes;
	}
    
    public static Plot GetPlot(Territory territory, int plotNum, bool subdiv)
    {
        if (_wardInfo == null)
            LoadWardInfo();

	    Debug.LogFormat("GetPlot {0} {1} {2}", territory, plotNum, subdiv);
        Plot p = _wardInfo.Where(_ => _.ward == territory &&
                                      _.index == plotNum &&
                                      _.subdiv == subdiv)
                                        .Select(_ => _).Single();

        return p;
    }
}