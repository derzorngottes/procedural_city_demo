using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class startup : MonoBehaviour {

	// these variables can be changed in the Start function to experiment with generating different types of cities
	public float FrequencyOfHills;		
	public float HighestHillHeight;
	public float LowestHillHeight;
	public float lotSize;
	public int buildingSeed;
	public Vector3 minBuildingDimens;
	public Vector3 medBuildingDimens;
	public Vector3 bigBuildingDimens;

	// global variables
	public Terrain terrainMap;
	public int terrainWidth;
	public int terrainLength;
	public int lotsByLength;
	public int lotsByWidth;
	Texture buildingTexture;

	// Use this for initialization
	void Start () {
		// Initialize global variable values
		FrequencyOfHills = 2.0f;			// higher number means more hills in the terrain
		HighestHillHeight = 5.0f;			// higher number means hillier landscape									
		LowestHillHeight = 0.0f;
		lotSize = 20.0f;					// a lower number means smaller lots and narrower passages (if too big buildings will overlap)
		buildingSeed = 47;					// the seed value for generating Perlin noise, changing it will change the layout of the buildings
		// building dimensions
		// changing these values will affect the size and shape of the buildings placed on the map
		minBuildingDimens = new Vector3 (5.0f, 10.0f, 5.0f);
		medBuildingDimens = new Vector3 (6.0f, 20.0f, 10.0f);
		bigBuildingDimens = new Vector3 (10.0f, 40.0f, 10.0f);

		// Call the methods to create and place terrain and buildings
		CreateTerrain ();
		GenerateHeights (terrainMap, FrequencyOfHills);
		AddTerrainTexture ();
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

		// terrain is 10 x 10, try changing to 50 x 50 for a much larger city
		_TerrainData.size = new Vector3(10, 600, 10);
		_TerrainData.heightmapResolution = 512;
		_TerrainData.baseMapResolution = 1024;
		_TerrainData.SetDetailResolution(1024, 16);

		TerrainCollider _TerrainCollider = TerrainObj.AddComponent<TerrainCollider>();
		terrainMap = TerrainObj.AddComponent<Terrain>();

		_TerrainCollider.terrainData = _TerrainData;
		terrainMap.terrainData = _TerrainData;

		// Capture the width and length of the terrain to use in Perlin noise functions
		terrainWidth = (int)terrainMap.terrainData.size.x;
		terrainLength = (int)terrainMap.terrainData.size.z;
	}

	/*
	 * Creates a height map using Perlin noise to create hills and valleys for the terrain
	 */
	public void GenerateHeights(Terrain terrain, float tileSize) {
		tileSize = 2.0f;
		HighestHillHeight = 5.0f;
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

		// apply the heights to the terrain
		terrain.terrainData.SetHeights(0, 0, heights);
	}

	/*
	 * Sets terrain texture (texture should be present as a file in Resources folder)
	 */
	public void AddTerrainTexture(){
		SplatPrototype[] terrainTexture = new SplatPrototype[1];
		terrainTexture [0] = new SplatPrototype ();
		terrainTexture [0].texture = Resources.Load ("GroundTexture") as Texture2D;
		terrainMap.terrainData.splatPrototypes = terrainTexture;
	}

	/*
	 *  Splits the terrain into building lots, then goes through each lot and
	 *  generates a building for that lot and places it on the terrain
	 */
	public void PlaceBuildings() {

		// get all the coordinates for the lots on the map
		Vector3[,] coordinates = CreateLots ();

		// for each lot, generate a building and place it on the map
		for (int i = 0; i < lotsByWidth; i++) {
			for (int j = 0; j < lotsByLength; j++) {
				CreateBuilding(coordinates[i, j]);
			}
		}
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
				var lotCoords = new Vector3 (widthMidPoint, GenerateBuildingHeight(widthMidPoint, lengthMidPoint), lengthMidPoint);
				lotCoordinates [i, j] = lotCoords;
			}
		}

		return lotCoordinates;
	}

	public void CreateBuilding(Vector3 position) {

		// If the Perlin noise value for the Y axis is less than 3, we do not build
		if (position.y < 3) {
			return;
		}

		// Otherwise, we create a building at the correct location and scale it
		// according to which bucket it falls into based on the perlin value

		// create the building GameObject
		var building = GameObject.CreatePrimitive (PrimitiveType.Cube);

		if (position.y > 8) {
			// create big building
			building.transform.localScale += bigBuildingDimens;
		} else if (position.y > 5) {
			// create medium building
			building.transform.localScale += medBuildingDimens;
		} else {
			// create small building
			building.transform.localScale += minBuildingDimens;
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
	 * Generates a Perlin noise value for the coordinatees of the building, weighted using a seed value
	 * change the value of the seed to change the height and distribution of the buildings
	 */
	public float GenerateBuildingHeight(float xCoord, float yCoord) {
		return 100 * Mathf.PerlinNoise(((float)xCoord / (float)terrainMap.terrainData.heightmapWidth) * buildingSeed, ((float)yCoord / (float)terrainMap.terrainData.heightmapHeight) * buildingSeed)/10.0f;
	}

	/*
	 * Create a simple texture for the building using code and return it
	 */
	public void CreateBuildingTexture(GameObject building)
	{
		var buildingRenderer = building.GetComponent<Renderer> ();
		buildingRenderer.material.mainTexture = Resources.Load("BuildingTexture") as Texture;
	}
}