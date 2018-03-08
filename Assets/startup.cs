using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class startup : MonoBehaviour {

	public Terrain terrainMap;

	// these variables can be changed to experiment with generating different types of cities
	public float FrequencyOfHills = 5.0f;

	public int terrainWidth;
	public int terrainLength;
	public int lotsByLength;
	public int lotsByWidth;
	Texture buildingTexture;

	// Use this for initialization
	void Start () {
		CreateTerrain ();
		GenerateHeights (terrainMap, FrequencyOfHills);
		AddTerrainMaterial ();
		PlaceBuildings ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	/*
	 * Generates a terrain with a width and height and collider
	 */
	public void CreateTerrain() {
		GameObject TerrainObj = new GameObject("TerrainObj");

		TerrainData _TerrainData = new TerrainData();

		_TerrainData.size = new Vector3(10, 600, 10);
		_TerrainData.heightmapResolution = 512;
		_TerrainData.baseMapResolution = 1024;
		_TerrainData.SetDetailResolution(1024, 16);

		int _heightmapWidth = _TerrainData.heightmapWidth;
		int _heightmapHeight = _TerrainData.heightmapHeight;

		TerrainCollider _TerrainCollider = TerrainObj.AddComponent<TerrainCollider>();
		terrainMap = TerrainObj.AddComponent<Terrain>();

		_TerrainCollider.terrainData = _TerrainData;
		terrainMap.terrainData = _TerrainData;

		// Capture the width and length of the terrain
		terrainWidth = (int)terrainMap.terrainData.size.x;
		terrainLength = (int)terrainMap.terrainData.size.z;
	}

	/*
	 * Sets terrain material
	 */
	public void AddTerrainMaterial(){
		SplatPrototype[] terrainTexture = new SplatPrototype[1];
		terrainTexture [0] = new SplatPrototype ();
		terrainTexture [0].texture = Resources.Load ("GroundTexture") as Texture2D;
		terrainMap.terrainData.splatPrototypes = terrainTexture;
	}

	/*
	 * Creates a height map using Perlin noise to determine hill dimensions
	 */
	public void GenerateHeights(Terrain terrain, float tileSize) {
		float HighestHillHeight = 10.0f;
		float LowestHillHeight = 0.0f;
		float hillHeight = (float)((float)HighestHillHeight - (float)LowestHillHeight) / ((float)terrain.terrainData.heightmapHeight / 2);
		float baseHeight = (float)LowestHillHeight / ((float)terrain.terrainData.heightmapHeight / 2);
		float[,] heights = new float[terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight];
		for (int i = 0; i < terrain.terrainData.heightmapWidth; i++)
		{
			for (int k = 0; k < terrain.terrainData.heightmapHeight; k++)
			{
				heights [i, k] = baseHeight + (Mathf.PerlinNoise (((float)i / (float)terrain.terrainData.heightmapWidth) * tileSize, ((float)k / (float)terrain.terrainData.heightmapHeight) * tileSize) * (float)hillHeight);
			}
		}

		terrain.terrainData.SetHeights(0, 0, heights);
	}
		
	/*
	 *  Splits the terrain into building lots, then goes through each lot and
	 *  generates a building for that lot and places it on the terrain
	 */
	public void PlaceBuildings() {

		// get all the coordinates for the lots on the map
		Vector3[,] coordinates = CreateLots ();

		for (int i = 0; i < lotsByWidth; i++) {
			for (int j = 0; j < lotsByLength; j++) {
				CreateBuilding(coordinates[i, j]);
			}
		}
	}

	/*
	 * Generates a Perlin noise value for the coordinatees of the building, weighted using a seed value
	 * change the value of the seed to change the height and distribution of the buildings
	 */
	public float GenerateBuildingHeights(float xCoord, float yCoord) {
		int seed = 47;
		return 100 * Mathf.PerlinNoise(((float)xCoord / (float)terrainMap.terrainData.heightmapWidth) * seed, ((float)yCoord / (float)terrainMap.terrainData.heightmapHeight) * seed)/10.0f;
	}

	/* 
	 *  Takes the dimensions of the terrain and splits it into square lots
	 *  whose size is determined by the variable lotSize.
	 * 
	 *  A future improvement would be to allow for non-square lots for the 
	 *  placement of buildings such as brownstone apartments, or city parks
	 * 
	 */
	public Vector3[,] CreateLots() {

		// lotSize is an approximate preferred lot size (one size) in pixels.
		// try changing it to 10.0f or 30.0f to see how the map changes.
		float lotSize = 20.0f;

		// first the number of lots you can fit by width and length
		// we will cast to int so the lots will not be perfectly square
		// but there will be no remainder in the terrain
		lotsByWidth = (int)(terrainWidth / lotSize);
		lotsByLength = (int)(terrainLength / lotSize);

		// create a new array which holds the coordinates for the midpoint of each lot
		Vector3[,] lotCoordinates = new Vector3[lotsByWidth, lotsByLength];

		// get the actual dimensions for each lot
		float lotWidth = (float)terrainWidth / lotsByWidth;
		float lotLength = (float)terrainLength / lotsByLength;

		// now, iterate across the terrain and get the midpoint coordinate for each lot
		for (int i = 0; i < lotsByWidth; i++) {
			for (int j = 0; j < lotsByLength; j++) {
				// first get the current position by adding up all the pixels we have already traversed
				float currentWidthPosition = i * lotWidth;
				float currentLengthPosition = j * lotLength;
				// then add the midpoints for the length and width of the current lot
				float widthMidPoint = currentWidthPosition + (lotWidth / 2);
				float lengthMidPoint = currentLengthPosition + (lotLength / 2);
				var lotCoords = new Vector3 (widthMidPoint, GenerateBuildingHeights(widthMidPoint, lengthMidPoint), lengthMidPoint);
				lotCoordinates [i, j] = lotCoords;
			}
		}

		return lotCoordinates;
	}

	public void CreateBuilding(Vector3 position) {

		// Possible extenion: build an array of empty lots and randomize or procedurally add 
		// vegetation or other terrain features, or wild animals, etc.	

		// If the perlin value (building height factor) is less than 3, we do not build
		if (position.y < 3) {
			return;
		}

		// Otherwise, we create a building at the correct location and scale it
		// according to which bucket it falls into based on the perlin value

		// create
		var building = GameObject.CreatePrimitive (PrimitiveType.Cube);
		//building.transform.position = position;

		if (position.y > 7) {
			// create big building
			building.transform.localScale += new Vector3(10.0f, 50.0f, 10.0f);
		} else if (position.y > 5) {
			building.transform.localScale += new Vector3(6.0f, 20.0f, 10.0f);
		} else {
			// create small building
			building.transform.localScale += new Vector3(5.0f, 10.0f, 5.0f);
		}

		// We need to figure out how high the terrain is at the point where the building will be generated
		// We subtract 2.0f height so that the building sinks into the ground slightly, to look more natural 
		// when sitting on a slope
		float terrainHeightFactor = Terrain.activeTerrain.SampleHeight (new Vector3 (position.x, 0, position.z)) - 5.0f;

		// place at the correct location
		building.transform.position = new Vector3(position.x, (building.transform.localScale.y / 2) + terrainHeightFactor, position.z);

		//building.AddComponent<Rigidbody>();
		CreateBuildingTexture(building);
	}



	/*
	 * Create a simple texture for the building using code and return it
	 */
	public void CreateBuildingTexture(GameObject building)
	{
		var buildingRenderer = building.GetComponent<Renderer> ();
		buildingRenderer.material.mainTexture = Resources.Load("BricksTexture") as Texture;
	}
}