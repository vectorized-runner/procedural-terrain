using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteInEditMode]
public class CustomTerrain : MonoBehaviour
{
	public enum TagType { Tag = 0, Layer = 1 }
	public enum VoronoiType { Linear, Power, Combined, SinPow }
	public enum ErosionType { Rain, Thermal, Tidal, River, Wind, Canyon }

	[System.Serializable]
	public class Detail
	{
		public GameObject prototype = null;
		public Texture2D prototypeTexture = null;
		public Color dryColor = Color.white;
		public Color healthyColor = Color.white;
		public Vector2 widthRange = Vector2.one;
		public Vector2 heightRange = Vector2.one;
		public float bendFactor = 1f;
		public float noiseSpread = 1f;
		[Range(-1f, 1f)] public float minHeight = 0.1f;
		[Range(-1f, 1f)] public float maxHeight = 0.2f;
		[Range(0f, 90f)] public float minSlope = 0f;
		[Range(0f, 90f)] public float maxSlope = 45f;
		[Range(0f, 1f)] public float density = 0.5f;
		public float heightNoiseMultiplier = 0.05f;
		public bool remove = false;
	}

	[System.Serializable]
	public class Vegetation
	{
		public GameObject mesh;
		public Color color1 = Color.white;
		public Color color2 = Color.white;
		public Color lightColor = Color.white;
		[Range(0f, 100f)] public float minScale = 0.5f;
		[Range(0f, 100f)] public float maxScale = 0.5f;
		[Range(-1f, 1f)] public float minHeight = 0.1f;
		[Range(-1f, 1f)] public float maxHeight = 0.2f;
		[Range(0f, 90f)] public float minSlope = 0f;
		[Range(0f, 90f)] public float maxSlope = 45f;
		[Range(0f, 1f)] public float density = 0.5f;
		public float bendFactor = 1f;
		public bool remove = false;
	}

	[System.Serializable]
	public class SplatHeights
	{
		public Texture2D texture = null;
		[Range(-1f, 1f)] public float minHeight = 0.1f;
		[Range(-1f, 1f)] public float maxHeight = 0.2f;
		[Range(0f, 90f)] public float minSlope = 0f;
		[Range(0f, 90f)] public float maxSlope = 45f;
		public float splatNoiseXScale = 0.01f;
		public float splatNoiseYScale = 0.01f;
		public float splatOffset = 0.01f;
		public float splatNoiseMultiplier = 0.5f;
		public Vector2 tileOffset = new Vector2(0, 0);
		public Vector2 tileSize = new Vector2(50, 50);
		public bool remove = false;
	}

	[System.Serializable]
	public class PerlinParameters
	{
		public float perlinXScale = 0.01f;
		public float perlinYScale = 0.01f;
		public int perlinOctaves = 3;
		public float perlinPersistance = 8f;
		public float perlinHeightScale = 0.09f;
		public int perlinOffsetX = 0;
		public int perlinOffsetY = 0;
		public bool remove = false;
	}

	public int terrainLayer = -1;
	public bool resetTerrain = true;
	// Essential code to start creating more tables 
	public List<Detail> details = new List<Detail>();
	public List<Vegetation> vegetation = new List<Vegetation>();
	public List<PerlinParameters> perlinParameters = new List<PerlinParameters>();
	public List<SplatHeights> splatHeights = new List<SplatHeights>();
	// Voronoi
	public VoronoiType voronoiType = VoronoiType.Linear;
	public float voronoiFallOff = 0.2f;
	public float voronoiDropOff = 0.6f;
	public int voronoiPeaks = 5;
	public float voronoiMaxHeight = 0.5f;
	public float voronoiMinHeight = 0.1f;
	// Perlin Noise
	public float perlinXScale = 0.01f;
	public float perlinYScale = 0.01f;
	public int perlinOffsetX = 0;
	public int perlinOffsetY = 0;
	public int perlinOctaves = 3;
	public float perlinPersistence = 8f;
	public float perlinHeightScale = 0.09f;
	public Vector2 randomHeightRange = new Vector2(0, 0.1f);
	public Texture2D heightMapTexture;
	public Vector3 heightMapScale = Vector3.one;
	public Terrain terrain;
	public TerrainData terrainData;
	// Midpoint Displacement
	public float mpdHeightMin = -2f;
	public float mpdHeightMax = 2f;
	public float mpdHeightDampenerPower = 2f;
	public float mpdRoughness = 2f;
	public float mpdRandomness = 0.5f;
	// Smooth 
	public int smoothAmount = 1;
	// Vegetation 
	public int maxTrees = 5000;
	public int treeSpacing = 5;
	// Detail 
	public int maxDetails = 5000;
	public int detailSpacing = 5;
	// Water 
	public float waterHeight = 0.5f;
	public GameObject waterMesh;
	public Material shorelineMaterial;
	// Erosion 
	public ErosionType erosionType;
	public float erosionStrength = 0.1f;
	public float heightDifference = 0.01f;
	public float solubility = 0.01f;
	public int droplets = 10;
	public int erosionSmoothAmount = 5;
	public int springsPerRiver = 5;
	// Wind
	public float windAngle = 30f;
	public float windScaleMultiplier = 0.05f;
	public float windNoiseMultiplier = 25f;
	// Splatmap
	//public float splatNoiseXScale = 0.01f;
	//public float splatNoiseYScale = 0.01f; 
	//public float splatOffset = 0.01f;
	//public float splatNoiseMultiplier = 0.5f;

	void Awake()
	{
		SetupTags(); 
	}

	void OnEnable()
	{
		terrain = GetComponent<Terrain>();
		terrainData = Terrain.activeTerrain.terrainData;
	}

	public void Erode()
	{
		switch(erosionType)
		{
			case ErosionType.Rain:
			RainErosion();
			break;
			case ErosionType.River:
			RiverErosion();
			break;
			case ErosionType.Thermal:
			ThermalErosion();
			break;
			case ErosionType.Tidal:
			TidalErosion();
			break;
			case ErosionType.Wind:
			Wind();
			break;
			case ErosionType.Canyon:
			Canyon();
			break; 
		}

		SmoothTerrain(erosionSmoothAmount);
	}



	void SetupTags()
	{
		// Get tag manager
		SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
		// Get tags property of tag manager
		SerializedProperty tagsProp = tagManager.FindProperty("tags");
		// Get layers property of tag manager 
		SerializedProperty layerProp = tagManager.FindProperty("layers");
		// Adding new tags at runtime 
		AddTag(tagsProp, "Terrain", TagType.Tag);
		AddTag(tagsProp, "Cloud", TagType.Tag);
		AddTag(tagsProp, "Shore", TagType.Tag);
		// Apply new tags 
		tagManager.ApplyModifiedProperties();
		// Add new layers at runtime 
		terrainLayer = AddTag(layerProp, "Terrain", TagType.Layer);
		// Apply new layers 
		tagManager.ApplyModifiedProperties();
		// Set tag and layer of this object
		gameObject.tag = "Terrain";
		gameObject.layer = terrainLayer;
	}

	void RainErosion()
	{
		float[,] heightMap = GetHeightMap();

		for(int i = 0; i < droplets; i++)
		{
			int x = Random.Range(0, terrainData.heightmapWidth);
			int y = Random.Range(0, terrainData.heightmapHeight);
			heightMap[x, y] -= erosionStrength;
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	void RiverErosion()
	{
		float[,] heightMap = GetHeightMap();
		float[,] erosionMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];

		for(int i = 0; i < droplets; i++)
		{
			int x = Random.Range(0, terrainData.heightmapWidth);
			int y = Random.Range(0, terrainData.heightmapHeight);
			Vector2 dropletPos = new Vector2(x, y);

			erosionMap[x, y] = erosionStrength;

			for(int j = 0; j < springsPerRiver; j++)
			{
				erosionMap = RunRiver(dropletPos, heightMap, erosionMap);
			}
		}

		for(int y = 0; y < terrainData.heightmapHeight; y++)
		{
			for(int x = 0; x < terrainData.heightmapWidth; x++)
			{
				if(erosionMap[x, y] > 0)
				{
					heightMap[x, y] -= erosionMap[x, y];
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	float[,] RunRiver(Vector2 dropletPos, float[,] heightMap, float[,] erosionMap)
	{
		if(solubility < 0.01f)
		{
			solubility = 0.01f;
		}

		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);

		while(erosionMap[(int)dropletPos.x, (int)dropletPos.y] > 0)
		{
			List<Vector2> neighbours = GenerateNeighbours(dropletPos, width, height);
			neighbours.Shuffle();

			bool foundLower = false;

			foreach(Vector2 n in neighbours)
			{
				if(heightMap[(int)n.x, (int)n.y] < heightMap[(int)dropletPos.x, (int)dropletPos.y])
				{
					erosionMap[(int)n.x, (int)n.y] = erosionMap[(int)dropletPos.x, (int)dropletPos.y] - solubility;
					dropletPos = n;
					foundLower = true;
					break;
				}
			}

			if(!foundLower)
			{
				erosionMap[(int)dropletPos.x, (int)dropletPos.y] -= solubility;
			}
		}

		return erosionMap;
	}

	void TidalErosion()
	{
		float[,] heightMap = GetHeightMap();

		for(int y = 0; y < terrainData.heightmapHeight; y++)
		{
			for(int x = 0; x < terrainData.heightmapWidth; x++)
			{
				Vector2 thisLocation = new Vector2(x, y);
				List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.heightmapWidth, terrainData.heightmapHeight);

				foreach(Vector2 n in neighbours)
				{
					// If we are at the shore
					if(heightMap[x, y] < waterHeight && heightMap[(int)n.x, (int)n.y] > waterHeight)
					{
						// Set this point and all its neighbours to water height
						heightMap[x, y] = waterHeight;
						heightMap[(int)n.x, (int)n.y] = waterHeight;
					}
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	void ThermalErosion()
	{
		float[,] heightMap = GetHeightMap();

		for(int y = 0; y < terrainData.heightmapHeight; y++)
		{
			for(int x = 0; x < terrainData.heightmapWidth; x++)
			{
				Vector2 thisLocation = new Vector2(x, y);
				List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.heightmapWidth, terrainData.heightmapHeight);

				foreach(Vector2 n in neighbours)
				{
					float currentHeight = heightMap[x, y];
					float currentNeighbour = heightMap[(int)n.x, (int)n.y];

					if(currentHeight > currentNeighbour + heightDifference)
					{
						heightMap[x, y] -= currentHeight * erosionStrength;
						heightMap[(int)n.x, (int)n.y] += currentHeight * erosionStrength;
					}
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	void Canyon()
	{
		float[,] heightMap = GetHeightMap();

		float digDepth = 0.05f;
		float bankSlope = 0.001f;
		float maxDepth = 0f;

		int x = 1;
		int y = Random.Range(10, terrainData.heightmapHeight - 10);

		while(y >= 0 && y < terrainData.heightmapHeight 
			&& x > 0 && x < terrainData.heightmapWidth)
		{
			float height = heightMap[x, y] - digDepth;
			CrawlCanyon(x, y, height, bankSlope, maxDepth, ref heightMap);
			x += Random.Range(1, 3);
			y += Random.Range(-2, 3); 
		}

		terrainData.SetHeights(0, 0, heightMap); 
	}

	void CrawlCanyon(int x, int y, float height, float slope, float maxDepth, ref float[,] tempHM)
	{
		if(x < 0 || x >= terrainData.heightmapWidth 
			|| y < 0 || y > terrainData.heightmapHeight
			|| height <= maxDepth || tempHM[x, y] <= height)
		{
			return;
		}
			
		tempHM[x, y] = height;

		CrawlCanyon(x + 1, y, height + Random.Range(slope, slope + 0.01f), slope, maxDepth, ref tempHM);
		CrawlCanyon(x - 1, y, height + Random.Range(slope, slope + 0.01f), slope, maxDepth, ref tempHM);
		CrawlCanyon(x + 1, y + 1, height + Random.Range(slope, slope + 0.01f), slope, maxDepth, ref tempHM);
		CrawlCanyon(x - 1, y + 1, height + Random.Range(slope, slope + 0.01f), slope, maxDepth, ref tempHM);
		CrawlCanyon(x, y - 1, height + Random.Range(slope, slope + 0.01f), slope, maxDepth, ref tempHM);
		CrawlCanyon(x, y + 1, height + Random.Range(slope, slope + 0.01f), slope, maxDepth, ref tempHM);
	}

	void Wind()
	{
		float[,] heightMap = GetHeightMap();

		int width = terrainData.heightmapWidth;
		int height = terrainData.heightmapHeight;

		float sinAngle = -Mathf.Sin(Mathf.Deg2Rad * windAngle);
		float cosAngle = Mathf.Cos(Mathf.Deg2Rad * windAngle); 

		for(int y = -(height - 1) * 2; y < height * 2; y += 10)
		{
			for(int x = -(width - 1) * 2; x <= width * 2; x += 1)
			{
	
				int noise = (int)(Mathf.PerlinNoise(
					x * windScaleMultiplier, y * windScaleMultiplier) * windNoiseMultiplier * erosionStrength);

				int nx = x;
				int digy = y + noise; 
				int ny = y + noise + 5;

				// Rotate x and y coords
				Vector2 digCoords = new Vector2(x * cosAngle - digy * sinAngle, digy * cosAngle + x * sinAngle);
				Vector2 pileCoords = new Vector2(nx * cosAngle - ny * sinAngle, ny * cosAngle + nx * sinAngle);

				bool valid = !(pileCoords.x < 0 || pileCoords.x > (width - 1)
					|| pileCoords.y < 0 || pileCoords.y > (height - 1)
					|| digCoords.x < 0 || digCoords.x > (width - 1)
					|| digCoords.y < 0 || digCoords.y > (height - 1));

				if(valid)
				{
					heightMap[(int)digCoords.x, (int)digCoords.y] -= 0.001f;
					heightMap[(int)pileCoords.x, (int)pileCoords.y] += 0.001f; 
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap); 
	}

	public void DrawShoreline()
	{
		float[,] heightMap = GetHeightMap();

		for(int y = 0; y < terrainData.heightmapHeight; y++)
		{
			for(int x = 0; x < terrainData.heightmapWidth; x++)
			{
				// Find spot on shore 
				Vector2 thisLocation = new Vector2(x, y);
				List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.heightmapWidth, terrainData.heightmapHeight);

				foreach(Vector2 n in neighbours)
				{
					// Water height is higher than the height map and one of the neighbors is higher than water,
					// then we are at the shore 
					if(heightMap[x, y] < waterHeight && heightMap[(int)n.x, (int)n.y] > waterHeight)
					{
						GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
						go.transform.localScale *= 10f;

						// x and y is swapped when taking from heightmap
						float xPos = y / (float)terrainData.heightmapWidth * terrainData.size.x;
						float yPos = waterHeight * terrainData.size.y;
						float zPos = x / (float)terrainData.heightmapHeight * terrainData.size.z;
						Vector3 offset = new Vector3(xPos, yPos, zPos);

						go.transform.LookAt(offset);
						go.transform.Rotate(90f, 0f, 0f);

						//go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

						go.transform.position = transform.position + offset;
						go.tag = "Shore";
					}
				}
			}
		}

		List<MeshFilter> meshFilters = GameObject.FindGameObjectsWithTag("Shore")
			.ToList()
			.Select(q => q.GetComponent<MeshFilter>())
			.ToList();

		CombineInstance[] combine = new CombineInstance[meshFilters.Count];

		for(int i = 0; i < meshFilters.Count; i++)
		{
			combine[i].mesh = meshFilters[i].sharedMesh;
			combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
			// You can't combine unless objects are disabled
			meshFilters[i].gameObject.SetActive(false);
		}

		GameObject shoreline = GameObject.Find("Shoreline");

		// Destroy previous created shoreline 
		if(shoreline != null)
		{
			DestroyImmediate(shoreline);
		}

		shoreline = new GameObject("Shoreline");
		shoreline.AddComponent<WaveAnimation>();
		shoreline.transform.position = transform.position;
		shoreline.transform.rotation = transform.rotation;

		MeshFilter meshFilter = shoreline.AddComponent<MeshFilter>();
		meshFilter.mesh = new Mesh();
		meshFilter.sharedMesh.CombineMeshes(combine);
		MeshRenderer meshRenderer = shoreline.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = shorelineMaterial;

		// Destroy all created quads after combine 
		meshFilters.ForEach(g => DestroyImmediate(g.gameObject));

	}

	public void AddWater()
	{
		GameObject water = FindObjectsOfType<GameObject>().ToList().Find(obj => obj.layer == 4);

		if(water == null)
		{
			water = Instantiate(waterMesh, transform.position, transform.rotation);
		}

		water.transform.position = transform.position +
			new Vector3(terrainData.size.x / 2f, waterHeight * terrainData.size.y, terrainData.size.z / 2f);

		water.transform.localScale = new Vector3(terrainData.size.x * 0.75f, 1, terrainData.size.z * 0.75f);
	}

	public void PlantVegetation()
	{
		vegetation.RemoveAll(v => v.mesh == null);

		TreePrototype[] newTreePrototypes = new TreePrototype[vegetation.Count];

		// Create TreePrototypes and set values from vegetation 
		for(int i = 0; i < vegetation.Count; i++)
		{
			newTreePrototypes[i] = new TreePrototype();
			newTreePrototypes[i].prefab = vegetation[i].mesh;
			newTreePrototypes[i].bendFactor = vegetation[i].bendFactor;
		}

		// Apply tree prototypes of terrain data 
		terrainData.treePrototypes = newTreePrototypes;

		List<TreeInstance> allVegetation = new List<TreeInstance>();

		for(int tp = 0; tp < terrainData.treePrototypes.Length; tp++)
		{
			Vegetation veg = vegetation[tp];

			EditorUtility.DisplayProgressBar("Planting Vegetation", veg.mesh.name, (float)tp / terrainData.treePrototypes.Length);

			for(int z = 0; z < terrainData.size.z; z += treeSpacing)
			{
				for(int x = 0; x < terrainData.size.x; x += treeSpacing)
				{
					// Lower density means more trees will be skipped randomly at (x,z)
					if(Random.Range(0f, 1f) > veg.density)
					{
						continue;
					}

					float height = terrainData.GetHeight(x, z) / terrainData.size.y;
					float steepness = terrainData.GetSteepness(x / terrainData.size.x,
															   z / terrainData.size.z);

					// If height at this point is in the vegetation height limits
					if((height >= veg.minHeight && height <= veg.maxHeight) &&
						// If steepness at this point is in the slope limits 
						(steepness >= veg.minSlope && steepness <= veg.maxSlope))
					{
						// Create new tree instance and set its properties
						TreeInstance instance = new TreeInstance();
						float randX = x + Random.Range(-5f, 5f);
						float randZ = z + Random.Range(-5f, 5f);
						// Clamp random position so trees won't go out of terrain 
						randX = Mathf.Clamp(randX, 0, terrainData.size.x);
						randZ = Mathf.Clamp(randZ, 0, terrainData.size.z);
						// Set initial position of tree instance 
						float posX = randX / terrainData.size.x;
						float posY = height;
						float posZ = randZ / terrainData.size.z;

						instance.position = new Vector3(posX, posY, posZ);

						// Scale instance position with terrain scale (handles terrain fitting the resolution)
						float scaledX = instance.position.x * terrainData.size.x / terrainData.alphamapWidth;
						float scaledZ = instance.position.z * terrainData.size.z / terrainData.alphamapHeight;
						// Prevent vegetation stacking at corners
						if(scaledX > 1f || scaledZ > 1f)
						{
							break;
						}

						instance.position = new Vector3(scaledX, instance.position.y, scaledZ);

						// Get the exact world position of this tree by 
						// raycasting up and down to find the exact height on the terrain
						float worldPosX = instance.position.x * terrainData.size.x;
						float worldPosY = instance.position.y * terrainData.size.y;
						float worldPosZ = instance.position.z * terrainData.size.z;
						Vector3 treeWorldPos = new Vector3(worldPosX, worldPosY, worldPosZ) + transform.position;
						RaycastHit hit;
						int layerMask = 1 << terrainLayer;

						if(Physics.Raycast(treeWorldPos + Vector3.up * 10, -Vector3.up, out hit, 100, layerMask) ||
							Physics.Raycast(treeWorldPos - Vector3.up * 10, Vector3.up, out hit, 100, layerMask))
						{
							float treeHeight = (hit.point.y - transform.position.y) / terrainData.size.y;
							instance.position = new Vector3(instance.position.x, treeHeight, instance.position.z);
						}

						float randScale = Random.Range(veg.minScale, veg.maxScale);
						float randRotRads = Random.Range(0f, 360f) * Mathf.Deg2Rad;
						Color randColor = Color.Lerp(veg.color1, veg.color2, Random.Range(0f, 1f));

						// Set other properties of the tree instance from vegetation
						instance.prototypeIndex = tp;
						instance.lightmapColor = veg.lightColor;
						instance.color = randColor;
						instance.rotation = randRotRads;
						instance.heightScale = randScale;
						instance.widthScale = randScale;

						// Add this instance to all vegetation
						allVegetation.Add(instance);
						// Don't exceed max tree count 
						if(allVegetation.Count >= maxTrees)
						{
							goto TREESDONE;
						}
					}
				}
			}
		}

TREESDONE:
// Apply tree instances to terrain data
		terrainData.treeInstances = allVegetation.ToArray();
		EditorUtility.ClearProgressBar();
	}

	public void PlantDetails()
	{
		// If both the prototype and the texture are missing
		details.RemoveAll(detail => detail.prototype == null && detail.prototypeTexture == null);

		// Create new detail prototype and set its properties from detail class
		DetailPrototype[] newDetailPrototypes = new DetailPrototype[details.Count];

		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth,
									terrainData.heightmapHeight);

		for(int i = 0; i < details.Count; i++)
		{
			Detail detail = details[i];

			newDetailPrototypes[i] = new DetailPrototype
			{
				prototype = detail.prototype,
				prototypeTexture = detail.prototypeTexture,
				healthyColor = detail.healthyColor,
				dryColor = detail.dryColor,
				bendFactor = detail.bendFactor,
				noiseSpread = detail.noiseSpread,
				maxHeight = detail.heightRange.y,
				minHeight = detail.heightRange.x,
				maxWidth = detail.widthRange.y,
				minWidth = detail.widthRange.x
			};

			DetailPrototype detailPrototype = newDetailPrototypes[i];

			// If prototype mesh is given, the mesh is used.
			// If the texture is given, then the texture is used, not both. 
			if(detailPrototype.prototype != null)
			{
				detailPrototype.usePrototypeMesh = true;
				detailPrototype.renderMode = DetailRenderMode.VertexLit;
				// Set prototype texture to null for better understanding in editor.
				detailPrototype.prototypeTexture = null;
			}
			else if(detailPrototype.prototypeTexture != null)
			{
				detailPrototype.usePrototypeMesh = false;
				detailPrototype.renderMode = DetailRenderMode.GrassBillboard;
			}
		}
		// Apply detail prototypes to terrain
		terrainData.detailPrototypes = newDetailPrototypes;

		for(int i = 0; i < terrainData.detailPrototypes.Length; i++)
		{
			Detail detail = details[i];
			int[,] detailMap = new int[terrainData.detailWidth, terrainData.detailHeight];

			EditorUtility.DisplayProgressBar("Planting Details", detail.prototype != null ? detail.prototype.name : detail.prototypeTexture.name, (float)i / terrainData.detailPrototypes.Length);

			// Go through all terrain and plant details
			for(int y = 0; y < terrainData.detailHeight; y += detailSpacing)
			{
				for(int x = 0; x < terrainData.detailWidth; x += detailSpacing)
				{
					// Less density means more details will be skipped at this (x,z) position
					if(Random.Range(0f, 1f) > detail.density)
					{
						continue;
					}

					// Calculate x and y of the heightmap from detail width/height
					int xHM = (int)(x / (float)terrainData.detailWidth * terrainData.heightmapWidth);
					int yHM = (int)(y / (float)terrainData.detailHeight * terrainData.heightmapHeight);

					float perlin = Mathf.PerlinNoise(x * detail.heightNoiseMultiplier, y * detail.heightNoiseMultiplier);
					float thisNoise = TerrainUtility.Map(perlin, 0, 1, 0.5f, 1);

					float thisHeightStart = thisNoise * detail.minHeight;
					float nextHeightStart = thisNoise * detail.maxHeight;
					// Detailmap is rotated 90 degrees, so when converting to heightmap swap x and z
					float thisHeight = heightMap[yHM, xHM];

					float steepness = terrainData.GetSteepness(
						xHM / terrainData.size.x,
						yHM / terrainData.size.z);

					// If the height is in between boundaries
					if(thisHeight >= thisHeightStart && thisHeight <= nextHeightStart &&
						// If steepness is between boundaries
						steepness >= detail.minSlope && steepness <= detail.maxSlope)
					{
						// x and y is backwards for the detail map, detail map is rotated 90 degrees
						// Apply detail map on this point 
						//detailMap[y, x] = 1;
						detailMap[y, x] = 1;
					}
				}
			}

			// Apply this detail on terrain with detailmap
			terrainData.SetDetailLayer(0, 0, i, detailMap);
		}

		EditorUtility.ClearProgressBar();
	}

	public void ClearDetails()
	{
		for(int i = 0; i < terrainData.detailPrototypes.Length; i++)
		{
			int[,] detailMap = new int[terrainData.detailWidth, terrainData.detailHeight];

			for(int y = 0; y < terrainData.detailHeight; y++)
			{
				for(int x = 0; x < terrainData.detailWidth; x++)
				{
					detailMap[y, x] = 0;
				}
			}

			terrainData.SetDetailLayer(0, 0, i, detailMap);
		}
	}

	public void SmoothTerrain(int amount)
	{
		float[,] heightMap = GetHeightMap();

		for(int i = 0; i < amount; i++)
		{
			EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", (float)i / amount);

			for(int y = 0; y < terrainData.heightmapHeight; y++)
			{
				for(int x = 0; x < terrainData.heightmapWidth; x++)
				{
					float avgHeight = heightMap[x, y];

					List<Vector2> neighbours = GenerateNeighbours(
						new Vector2(x, y),
						terrainData.heightmapWidth,
						terrainData.heightmapHeight);

					foreach(Vector2 n in neighbours)
					{
						avgHeight += heightMap[(int)n.x, (int)n.y];
					}

					heightMap[x, y] = avgHeight / (neighbours.Count + 1);
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
		EditorUtility.ClearProgressBar();
	}

	public void MidpointDisplacement()
	{
		float[,] heightMap = resetTerrain ? GetNewMap() : GetHeightMap();
		int width = terrainData.heightmapWidth - 1;
		int squareSize = width;

		// Extra adjustment variables to the height 
		float heightMin = mpdHeightMin;
		float heightMax = mpdHeightMax;
		float heightDampener = Mathf.Pow(mpdHeightDampenerPower, -mpdRoughness);

		// [x, y] is left downmost of the terrain, [cornerX, cornerY] is the right upmost.
		int cornerX, cornerY;
		int midX, midY;
		// X left-right, Y up-down 
		int pmidXL, pmidXR, pmidYU, pmidYD;

		while(squareSize > 0)
		{
			// Calculating the midpoint that has 4 corners inside the square
			for(int x = 0; x < width; x += squareSize)
			{
				for(int y = 0; y < width; y += squareSize)
				{
					// Get the coordinates of right upmost point
					cornerX = (x + squareSize);
					cornerY = (y + squareSize);

					// Get the coordinates of the middle point
					midX = (int)(x + squareSize / 2f);
					midY = (int)(y + squareSize / 2f);

					// Set the height of the middle to the average of the 4 corners
					heightMap[midX, midY] = (heightMap[x, y] + heightMap[cornerX, y] + heightMap[x, cornerY] + heightMap[cornerX, cornerY]) / 4f;
					heightMap[midX, midY] += Random.Range(heightMin, heightMax) * mpdRandomness;
				}
			}

			// Calculating the midpoint that has one of its corners outside the square
			for(int x = 0; x < width; x += squareSize)
			{
				for(int y = 0; y < width; y += squareSize)
				{
					cornerX = (x + squareSize);
					cornerY = (y + squareSize);

					midX = (int)(x + squareSize / 2f);
					midY = (int)(y + squareSize / 2f);

					pmidXR = midX + squareSize;
					pmidXL = midX - squareSize;
					pmidYU = midY + squareSize;
					pmidYD = midY - squareSize;

					if(pmidXL <= 0 || pmidYD <= 0 || pmidXR >= width - 1 || pmidYU >= width - 1)
					{
						continue;
					}

					// Calculate square value for the bottom side
					heightMap[midX, y] =
						(heightMap[x, y] +
						heightMap[cornerX, y] +
						heightMap[midX, midY] +
						heightMap[midX, pmidYD]) / 4f;

					// Left
					heightMap[x, midY] =
						(heightMap[x, y] +
						heightMap[x, cornerY] +
						heightMap[midX, midY] +
						heightMap[pmidXL, midY]) / 4f;

					// Up
					heightMap[midX, cornerY] =
						(heightMap[x, cornerY] +
						heightMap[cornerX, cornerY] +
						heightMap[midX, midY] +
						heightMap[midX, pmidYU]) / 4f;

					// Right
					heightMap[cornerX, midY] =
						(heightMap[cornerX, y] +
						heightMap[cornerX, cornerY] +
						heightMap[midX, midY] +
						heightMap[pmidXR, midY]) / 4f;

					heightMap[midX, y] += Random.Range(-heightMin, heightMax) * mpdRandomness;
					heightMap[x, midY] += Random.Range(-heightMin, heightMax) * mpdRandomness;
					heightMap[midX, cornerY] += Random.Range(-heightMin, heightMax) * mpdRandomness;
					heightMap[cornerX, midY] += Random.Range(-heightMin, heightMax) * mpdRandomness;
				}
			}

			squareSize = (int)(squareSize / 2f);
			heightMin *= heightDampener;
			heightMax *= heightDampener;
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	public void Voronoi()
	{
		float[,] heightMap = resetTerrain ? GetNewMap() : GetHeightMap();

		for(int p = 0; p < voronoiPeaks; p++)
		{

			// Set the peak point randomly (x z for position on terrain, y for height of the peak) 
			Vector3 peak = new Vector3(
				Random.Range(0, terrainData.heightmapWidth),
				Random.Range(voronoiMinHeight, voronoiMaxHeight),
				Random.Range(0, terrainData.heightmapHeight));

			if(heightMap[(int)peak.x, (int)peak.z] < peak.y)
			{
				heightMap[(int)peak.x, (int)peak.z] = peak.y;
			}
			else
			{
				continue;
			}

			Vector2 peakLocation = new Vector2(peak.x, peak.z);
			float maxDistance = Vector2.Distance(new Vector2(0, 0),
			new Vector2(terrainData.heightmapWidth, terrainData.heightmapHeight));

			for(int y = 0; y < terrainData.heightmapHeight; y++)
			{
				for(int x = 0; x < terrainData.heightmapWidth; x++)
				{
					if(!(x == peak.x && y == peak.z))
					{
						float distanceToPeak = Vector2.Distance(new Vector2(x, y), peakLocation) / maxDistance;
						float heightAtPoint;

						if(voronoiType == VoronoiType.Linear)
						{
							heightAtPoint = peak.y - distanceToPeak * voronoiFallOff;
						}
						else if(voronoiType == VoronoiType.Combined)
						{
							heightAtPoint = peak.y - distanceToPeak * voronoiFallOff - Mathf.Pow(distanceToPeak, voronoiDropOff);
						}
						else if(voronoiType == VoronoiType.Power)
						{
							heightAtPoint = peak.y - Mathf.Pow(distanceToPeak, voronoiDropOff) * voronoiFallOff;
						}
						else if(voronoiType == VoronoiType.SinPow)
						{
							heightAtPoint = peak.y - Mathf.Pow(distanceToPeak * 3, voronoiFallOff) -
								Mathf.Sin(distanceToPeak * 2 * Mathf.PI) / voronoiDropOff;
						}
						else
						{
							heightAtPoint = 0;
						}

						if(heightMap[x, y] < heightAtPoint)
						{
							heightMap[x, y] = heightAtPoint;
						}
					}
				}
			}

			terrainData.SetHeights(0, 0, heightMap);
		}
	}

	public void Perlin()
	{
		float[,] heightMap = resetTerrain ? GetNewMap() : GetHeightMap();
		for(int y = 0; y < terrainData.heightmapHeight; y++)
		{
			for(int x = 0; x < terrainData.heightmapWidth; x++)
			{
				heightMap[x, y] += TerrainUtility.FractalBrownianMotion((x + perlinOffsetX) * perlinXScale, (y + perlinOffsetY) * perlinYScale, perlinOctaves, perlinPersistence) * perlinHeightScale;
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
	}

	public void MultiplePerlinTerrain()
	{
		float[,] heightMap = resetTerrain ? GetNewMap() : GetHeightMap();
		for(int y = 0; y < terrainData.heightmapHeight; y++)
		{
			for(int x = 0; x < terrainData.heightmapWidth; x++)
			{
				foreach(PerlinParameters p in perlinParameters)
				{
					heightMap[x, y] += TerrainUtility.FractalBrownianMotion((x + p.perlinOffsetX) * p.perlinXScale,
													 (y + p.perlinOffsetY) * p.perlinYScale,
													  p.perlinOctaves, p.perlinPersistance)
													  * p.perlinHeightScale;
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	public void SplatMaps()
	{
		splatHeights.RemoveAll(h => h.texture == null);

		TerrainLayer[] terrainLayers = new TerrainLayer[splatHeights.Count];

		for(int i = 0; i < splatHeights.Count; i++)
		{
			SplatHeights height = splatHeights[i];

			terrainLayers[i] = new TerrainLayer
			{
				diffuseTexture = height.texture,
				tileOffset = height.tileOffset,
				tileSize = height.tileSize
			};

			TerrainLayer layer = terrainLayers[i];

			// Create terrain layers folder
			string path = Application.dataPath + "/TerrainLayers";
			if(System.IO.Directory.Exists(path))
			{
				System.IO.Directory.CreateDirectory(path);
			}
			// Create new terrain layer asset
			string assetPath = "Assets/TerrainLayers/New Terrain Layer " + i + ".terrainlayer";
			AssetDatabase.CreateAsset(layer, assetPath);
			terrainLayers[i].diffuseTexture.Apply(true);
			Selection.activeObject = gameObject;
		}

		terrainData.terrainLayers = terrainLayers;

		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
		float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

		for(int y = 0; y < terrainData.alphamapHeight; ++y)
		{
			for(int x = 0; x < terrainData.alphamapWidth; ++x)
			{
				float[] splat = new float[terrainData.alphamapLayers];
				for(int i = 0; i < splatHeights.Count; ++i)
				{
					float noise = Mathf.PerlinNoise(x * splatHeights[i].splatNoiseXScale, y
														* splatHeights[i].splatNoiseYScale)
														* splatHeights[i].splatNoiseMultiplier;
					float offset = splatHeights[i].splatOffset + noise;
					float thisHeightStart = splatHeights[i].minHeight - offset;
					float thisHeightStop = splatHeights[i].maxHeight + offset;
					//float steepness = GetSteepness( heightMap, x, y, 
					//                                terrainData.heightmapWidth,
					//                                terrainData.heightmapHeight);
					float steepness = terrainData.GetSteepness(y / (float)terrainData.alphamapHeight,
																x / (float)terrainData.alphamapWidth);

					if((heightMap[x, y] >= thisHeightStart &&
						heightMap[x, y] <= thisHeightStop) &&
						(steepness >= splatHeights[i].minSlope &&
						steepness <= splatHeights[i].maxSlope))
					{
						splat[i] = 1;
					}
				}
				NormalizeVector(splat);
				for(int j = 0; j < splatHeights.Count; ++j)
				{
					splatmapData[x, y, j] = splat[j];
				}
			}
		}

		terrainData.SetAlphamaps(0, 0, splatmapData);
	}

	public void LoadHeightMap(Texture2D texture)
	{
		float[,] heightMap = GetHeightMap();

		for(int x = 0; x < texture.width; x++)
		{
			for(int y = 0; y < texture.height; y++)
			{
				float colValue = heightMap[x, y];
				Color pixColor = new Color(colValue, colValue, colValue);
				texture.SetPixel(x, y, pixColor);
			}
		}

		texture.Apply(false, false);
	}

	public void LoadHeightsFromTexture()
	{
		float[,] heightMap = GetHeightMap();

		for(int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for(int y = 0; y < terrainData.heightmapHeight; y++)
			{
				heightMap[x, y] += heightMapTexture.GetPixel((int)(x * heightMapScale.x), (int)(y * heightMapScale.y)).grayscale * heightMapScale.z;
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	public void AddNewDetails()
	{
		details.Add(new Detail());
	}

	public void AddNewSplatHeight()
	{
		splatHeights.Add(new SplatHeights());
	}

	public void AddNewPerlin()
	{
		perlinParameters.Add(new PerlinParameters());
	}

	public void AddNewVegetation()
	{
		vegetation.Add(new Vegetation());
	}

	public void ClearVegetation()
	{
		terrainData.treeInstances = new TreeInstance[0];
	}

	public void RemoveDetails()
	{
		List<Detail> keptDetails = new List<Detail>();

		for(int i = 0; i < details.Count; i++)
		{
			if(!details[i].remove)
			{
				keptDetails.Add(details[i]);
			}
		}

		details = keptDetails;
	}

	public void RemoveVegetation()
	{
		List<Vegetation> keptVegetation = new List<Vegetation>();

		for(int i = 0; i < vegetation.Count; i++)
		{
			if(!vegetation[i].remove)
			{
				keptVegetation.Add(vegetation[i]);
			}
		}

		vegetation = keptVegetation;
	}

	public void RemoveSplatHeight()
	{
		List<SplatHeights> keptSplatheights = new List<SplatHeights>();

		for(int i = 0; i < splatHeights.Count; i++)
		{
			if(!splatHeights[i].remove)
			{
				keptSplatheights.Add(splatHeights[i]);
			}
		}

		splatHeights = keptSplatheights;
	}

	public void RemovePerlin()
	{
		List<PerlinParameters> keptPerlin = new List<PerlinParameters>();
		for(int i = 0; i < perlinParameters.Count; i++)
		{
			if(!perlinParameters[i].remove)
			{
				keptPerlin.Add(perlinParameters[i]);
			}
		}

		perlinParameters = keptPerlin;
	}

	public void RandomTerrain()
	{
		float[,] heightMap = resetTerrain ? GetNewMap() : GetHeightMap();

		for(int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for(int y = 0; y < terrainData.heightmapHeight; y++)
			{
				heightMap[x, y] += Random.Range(randomHeightRange.x, randomHeightRange.y);
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
	}

	public void ResetTerrain()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

		for(int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for(int z = 0; z < terrainData.heightmapHeight; z++)
			{
				heightMap[x, z] = 0;
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	float GetSteepness(float[,] heightMap, int x, int y, int width, int height)
	{
		float h = heightMap[x, y];
		int nx = x + 1;
		int ny = y + 1;

		// if on the upper edge of the map find gradient by going backward.
		if(nx > width - 1)
		{
			nx = x - 1;
		}
		if(ny > height - 1)
		{
			ny = y - 1;
		}

		float dx = heightMap[nx, y] - h;
		float dy = heightMap[x, ny] - h;
		Vector2 gradient = new Vector2(dx, dy);
		float steep = gradient.magnitude;
		return steep;
	}

	List<Vector2> GenerateNeighbours(Vector2 pos, int width, int height)
	{
		List<Vector2> neighbours = new List<Vector2>();

		for(int y = -1; y < 2; y++)
		{
			for(int x = -1; x < 2; x++)
			{
				if(!(x == 0 && y == 0))
				{
					Vector2 nPos = new Vector2(
						Mathf.Clamp(pos.x + x, 0, width - 1),
						Mathf.Clamp(pos.y + y, 0, height - 1));

					if(!neighbours.Contains(nPos))
					{
						neighbours.Add(nPos);
					}
				}
			}
		}

		return neighbours;
	}

	float[] NormalizeVector(float[] v)
	{
		float total = 0;
		for(int i = 0; i < v.Length; i++)
		{
			total += v[i];
		}
		for(int i = 0; i < v.Length; i++)
		{
			v[i] /= total;
		}

		return v;
	}

	float[,] GetHeightMap()
	{
		return terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
	}

	float[,] GetNewMap()
	{
		return new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
	}

	int AddTag(SerializedProperty tagsProp, string newTag, TagType tagType)
	{
		bool found = false;

		// Ensure the tag does not already exist
		for(int i = 0; i < tagsProp.arraySize; i++)
		{
			SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
			if(t.stringValue.Equals(newTag))
			{
				found = true;
				return i;
			}
		}

		// Add your new tag
		if(!found && tagType == TagType.Tag)
		{
			tagsProp.InsertArrayElementAtIndex(0);
			SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
			newTagProp.stringValue = newTag;
		}
		// Add your new layer 
		else if(!found && tagType == TagType.Layer)
		{
			for(int j = 8; j < tagsProp.arraySize; j++)
			{
				SerializedProperty newLayer = tagsProp.GetArrayElementAtIndex(j);
				// add layer in next empty slot
				if(newLayer.stringValue == "")
				{
					newLayer.stringValue = newTag;
					return j;
				}
			}
		}

		return -1;
	}
}
