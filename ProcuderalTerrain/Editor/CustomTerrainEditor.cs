using UnityEngine;
using UnityEditor;
using EditorGUITable; // Asset Store

[CustomEditor(typeof(CustomTerrain))]
[CanEditMultipleObjects]
public class CustomTerrainEditor : Editor
{
	// Erosion 
	SerializedProperty erosionType;
	SerializedProperty erosionStrength;
	SerializedProperty solubility;
	SerializedProperty droplets;
	SerializedProperty heightDifference; 
	SerializedProperty erosionSmoothAmount;
	SerializedProperty springsPerRiver; 
	// Water 
	SerializedProperty waterHeight;
	SerializedProperty waterMesh;
	SerializedProperty shorelineMaterial;	
	// Voronoi
	SerializedProperty voronoiType;
	SerializedProperty voronoiPeaks;
	SerializedProperty voronoiMinHeight;
	SerializedProperty voronoiMaxHeight;
	SerializedProperty voronoiFallOff;
	SerializedProperty voronoiDropOff;
	// Perlin
	SerializedProperty perlinXScale;
	SerializedProperty perlinYScale;
	SerializedProperty perlinOffsetX;
	SerializedProperty perlinOffsetY;
	SerializedProperty perlinOctaves;
	SerializedProperty perlinPersistence;
	SerializedProperty perlinHeightScale;
	SerializedProperty perlinParameters;
	// MDP 
	SerializedProperty mpdRandomness; 
	SerializedProperty mpdHeightMin;
	SerializedProperty mpdHeightMax;
	SerializedProperty mpdHeightDampenerPower;
	SerializedProperty mpdRoughness;
	// Vegetation 
	SerializedProperty vegetation;
	SerializedProperty maxTrees;
	SerializedProperty treeSpacing;
	// Details
	SerializedProperty details;
	SerializedProperty maxDetails;
	SerializedProperty detailSpacing; 
	// Heights
	SerializedProperty heightMapScale;
	SerializedProperty heightMapTexture;
	SerializedProperty randomHeightRange;
	// Smoothing 
	SerializedProperty smoothAmount;
	// Reset 
	SerializedProperty resetTerrain;
	// Splatmaps
	SerializedProperty splatHeights;
	// Wind 
	SerializedProperty windAngle;
	SerializedProperty windScaleMultiplier;
	SerializedProperty windNoiseMultiplier; 

	//SerializedProperty splatNoiseXScale;
	//SerializedProperty splatNoiseYScale;
	//SerializedProperty splatOffset;
	//SerializedProperty splatNoiseMultiplier; 

	GUITableState perlinParametersTable;
	GUITableState splatHeightsTable;
	GUITableState vegetationTable;
	GUITableState detailTable;

	// Foldouts
	bool showVegetation = false;
	bool showSplatMaps = false;
	bool showSmooth = false;
	bool showMDP = false;
	bool showRandom = false;
	bool showLoadHeights = false;
	bool showPerlinNoise = false;
	bool showMultiplePerlin = false;
	bool showVoronoi = false;
	bool showDetail = false;
	bool showHeightMap = false;
	bool showWater = false;
	bool showErosion = false; 

	Vector2 scrollPos;
	CustomTerrain terrain;
	Texture2D hmTexture; 

	void OnEnable()
	{
		// GUITables
		detailTable = new GUITableState("detailTable");
		perlinParametersTable = new GUITableState("perlinParametersTable");
		splatHeightsTable = new GUITableState("splatHeightsTable");
		vegetationTable = new GUITableState("vegetationTable");
		// Wind 
		windAngle = serializedObject.FindProperty("windAngle");
		windScaleMultiplier = serializedObject.FindProperty("windScaleMultiplier");
		windNoiseMultiplier = serializedObject.FindProperty("windNoiseMultiplier"); 
		// Erosion
		erosionType = serializedObject.FindProperty("erosionType");
		erosionStrength = serializedObject.FindProperty("erosionStrength");
		solubility = serializedObject.FindProperty("solubility");
		droplets = serializedObject.FindProperty("droplets");
		erosionSmoothAmount = serializedObject.FindProperty("erosionSmoothAmount");
		springsPerRiver = serializedObject.FindProperty("springsPerRiver");
		heightDifference = serializedObject.FindProperty("heightDifference"); 
		// Water
		waterHeight = serializedObject.FindProperty("waterHeight");
		waterMesh = serializedObject.FindProperty("waterMesh");
		shorelineMaterial = serializedObject.FindProperty("shorelineMaterial"); 
		// Voronoi params
		voronoiFallOff = serializedObject.FindProperty("fallOff");
		voronoiDropOff = serializedObject.FindProperty("dropOff");
		voronoiType = serializedObject.FindProperty("voronoiType");
		voronoiDropOff = serializedObject.FindProperty("voronoiDropOff");
		voronoiFallOff = serializedObject.FindProperty("voronoiFallOff");
		voronoiMinHeight = serializedObject.FindProperty("voronoiMinHeight");
		voronoiMaxHeight = serializedObject.FindProperty("voronoiMaxHeight");
		voronoiPeaks = serializedObject.FindProperty("voronoiPeaks");
		// Perlin params 
		perlinXScale = serializedObject.FindProperty("perlinXScale");
		perlinYScale = serializedObject.FindProperty("perlinYScale");
		perlinOffsetX = serializedObject.FindProperty("perlinOffsetX");
		perlinOffsetY = serializedObject.FindProperty("perlinOffsetY");
		perlinOctaves = serializedObject.FindProperty("perlinOctaves");
		perlinPersistence = serializedObject.FindProperty("perlinPersistence");
		perlinHeightScale = serializedObject.FindProperty("perlinHeightScale");
		perlinParameters = serializedObject.FindProperty("perlinParameters");
		// MDP params 
		mpdRandomness = serializedObject.FindProperty("mpdRandomness"); 
		mpdHeightMin = serializedObject.FindProperty("mpdHeightMin");
		mpdHeightMax = serializedObject.FindProperty("mpdHeightMax");
		mpdHeightDampenerPower = serializedObject.FindProperty("mpdHeightDampenerPower");
		mpdRoughness = serializedObject.FindProperty("mpdRoughness");
		// Heights
		heightMapScale = serializedObject.FindProperty("heightMapScale");
		heightMapTexture = serializedObject.FindProperty("heightMapTexture");
		randomHeightRange = serializedObject.FindProperty("randomHeightRange");
		// Smooth
		smoothAmount = serializedObject.FindProperty("smoothAmount");
		// Reset 
		resetTerrain = serializedObject.FindProperty("resetTerrain");
		// Vegetation
		vegetation = serializedObject.FindProperty("vegetation");
		maxTrees = serializedObject.FindProperty("maxTrees");
		treeSpacing = serializedObject.FindProperty("treeSpacing");
		// Details
		details = serializedObject.FindProperty("details");
		maxDetails = serializedObject.FindProperty("maxDetails");
		detailSpacing = serializedObject.FindProperty("detailSpacing"); 
		// Splatmap
		splatHeights = serializedObject.FindProperty("splatHeights");
		//splatNoiseXScale = serializedObject.FindProperty("splatNoiseXScale");
		//splatNoiseYScale = serializedObject.FindProperty("splatNoiseYScale"); 
		//splatNoiseMultiplier = serializedObject.FindProperty("splatNoiseMultiplier");
		//splatOffset = serializedObject.FindProperty("splatOffset");

		terrain = (CustomTerrain)target;
		hmTexture = new Texture2D(513, 513, TextureFormat.ARGB32, false);
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		// Scrollbar start code
		Rect rect = EditorGUILayout.BeginVertical();
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(rect.width), GUILayout.Height(rect.height));
		EditorGUI.indentLevel++;

		// Display the script on editor
		GUI.enabled = false;
		EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoBehaviour), false);
		GUI.enabled = true;

		EditorGUILayout.PropertyField(resetTerrain);
		Voronoi();
		MultiplePerlin();
		SinglePerlin();
		RandomHeights();
		LoadHeights();
		MidpointDisplacement();
		Smoothing();
		SplatMaps();
		Vegetation();
		Detail();
		HeightMap();
		Water();
		Erosion(); 
		ResetHeights();

		// Scrollbar end code 
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		serializedObject.ApplyModifiedProperties();
	}

	void Erosion()
	{
		showErosion = EditorGUILayout.Foldout(showErosion, "Erosion");

		if(showErosion)
		{
			EditorGUILayout.PropertyField(erosionType);

			if(erosionType.enumValueIndex != (int)CustomTerrain.ErosionType.Tidal)
			{
				EditorGUILayout.Slider(erosionStrength, 0f, 1f);
			}
			if(erosionType.enumValueIndex == (int)CustomTerrain.ErosionType.Rain
				|| erosionType.enumValueIndex == (int)CustomTerrain.ErosionType.River)
			{
				EditorGUILayout.IntSlider(droplets, 0, 1000);
			}
			if(erosionType.enumValueIndex == (int)CustomTerrain.ErosionType.Thermal)
			{
				EditorGUILayout.Slider(heightDifference, 0f, 0.01f); 
			}
			if(erosionType.enumValueIndex == (int)CustomTerrain.ErosionType.River)
			{

				EditorGUILayout.Slider(solubility, 0f, 1f);
				EditorGUILayout.IntSlider(springsPerRiver, 0, 20);
			}
			if(erosionType.enumValueIndex == (int)CustomTerrain.ErosionType.Wind)
			{
				EditorGUILayout.Slider(windAngle, 0f, 360f);
				EditorGUILayout.Slider(windScaleMultiplier, -0.1f, 0.1f);
				EditorGUILayout.Slider(windNoiseMultiplier, 0f, 100f);
			}

			EditorGUILayout.IntSlider(erosionSmoothAmount, 0, 10); 
			
			if(GUILayout.Button("Erode"))
			{
				terrain.Erode(); 
			}
		}
	}

	void Water()
	{
		showWater = EditorGUILayout.Foldout(showWater, "Water");

		if(showWater)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			EditorGUILayout.Slider(waterHeight, 0f, 1f, new GUIContent("Water Height"));
			EditorGUILayout.ObjectField(waterMesh, typeof(GameObject));

			if(GUILayout.Button("Add Water"))
			{
				terrain.AddWater(); 
			}

			EditorGUILayout.ObjectField(shorelineMaterial, typeof(Material));

			if(GUILayout.Button("Add Shoreline"))
			{
				terrain.DrawShoreline(); 
			}
		}
	}

	void HeightMap()
	{
		showHeightMap = EditorGUILayout.Foldout(showHeightMap, "HeightMap"); 

		if(showHeightMap)
		{
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace(); 
			int size = (int)(EditorGUIUtility.currentViewWidth - 100f);
			GUILayout.Label(hmTexture, GUILayout.Width(size), GUILayout.Height(size));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			if(GUILayout.Button("Refresh"))
			{
				terrain.LoadHeightMap(hmTexture);
			}
		}
	}

	void Detail()
	{
		showDetail = EditorGUILayout.Foldout(showDetail, "Detail");

		if(showDetail)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Detail", EditorStyles.boldLabel);
			EditorGUILayout.IntSlider(maxDetails, 0, 10000, new GUIContent("Max Details"));
			EditorGUILayout.IntSlider(detailSpacing, 1, 20, new GUIContent("Detail Spacing"));

			detailTable = GUITableLayout.DrawTable(detailTable, details, GUITableOption.Reorderable(true));

			GUILayout.Space(20);
			EditorGUILayout.BeginHorizontal();

			if(GUILayout.Button("+"))
			{
				terrain.AddNewDetails();
			}
			if(GUILayout.Button("-"))
			{
				terrain.RemoveDetails();
			}
			EditorGUILayout.EndHorizontal();

			if(GUILayout.Button("Apply Details"))
			{
				terrain.PlantDetails();
			}

			if(GUILayout.Button("Clear Details"))
			{
				terrain.ClearDetails(); 
			}
		}
	}

	void Vegetation()
	{
		showVegetation = EditorGUILayout.Foldout(showVegetation, "Vegetation");

		if(showVegetation)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Vegetation", EditorStyles.boldLabel);
			EditorGUILayout.IntSlider(maxTrees, 0, 10000, "Max Trees");
			EditorGUILayout.IntSlider(treeSpacing, 2, 20, "Tree Spacing");

			vegetationTable = GUITableLayout.DrawTable(
				vegetationTable, vegetation, GUITableOption.Reorderable(true));

			EditorGUILayout.BeginHorizontal();

			if(GUILayout.Button("+"))
			{
				terrain.AddNewVegetation();
			}

			if(GUILayout.Button("-"))
			{
				terrain.RemoveVegetation();
			}

			EditorGUILayout.EndHorizontal();

			if(GUILayout.Button("Apply Vegetation"))
			{
				terrain.PlantVegetation();
			}

			if(GUILayout.Button("Clear Vegetation"))
			{
				terrain.ClearVegetation();
			}
		}
	}

	void SplatMaps()
	{
		showSplatMaps = EditorGUILayout.Foldout(showSplatMaps, "Splat Maps");

		if(showSplatMaps)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Splat Maps", EditorStyles.boldLabel);

			//EditorGUILayout.Slider(splatOffset, 0f, 1f, "Offset");
			//EditorGUILayout.Slider(splatNoiseXScale, 0f, 1f, "Noise Scale X");
			//EditorGUILayout.Slider(splatNoiseYScale, 0f, 1f, "Noise Y Scale"); 
			//EditorGUILayout.Slider(splatNoiseMultiplier, 0f, 1f, "Noise Multipler");

			splatHeightsTable = GUITableLayout.DrawTable(
				splatHeightsTable, splatHeights, GUITableOption.Reorderable(true));

			EditorGUILayout.BeginHorizontal();

			if(GUILayout.Button("+"))
			{
				terrain.AddNewSplatHeight();
			}

			if(GUILayout.Button("-"))
			{
				terrain.RemoveSplatHeight();
			}
			EditorGUILayout.EndHorizontal();

			if(GUILayout.Button("Apply SplatMaps"))
			{
				terrain.SplatMaps();
			}
		}
	}

	void Smoothing()
	{
		showSmooth = EditorGUILayout.Foldout(showSmooth, "Smooth");

		if(showSmooth)
		{
			EditorGUILayout.IntSlider(smoothAmount, 1, 10, "Smooth Amount");

			if(GUILayout.Button("Smooth"))
			{
				terrain.SmoothTerrain(smoothAmount.intValue);
			}
		}
	}

	void MidpointDisplacement()
	{
		showMDP = EditorGUILayout.Foldout(showMDP, "Midpoint Displacement");

		if(showMDP)
		{
			EditorGUILayout.Slider(mpdHeightMin, -2f, 0f, "Height Min");
			EditorGUILayout.Slider(mpdHeightMax, 0f, 2f, "Height Max");
			EditorGUILayout.Slider(mpdHeightDampenerPower, 0f, 5f, "Dampener Power");
			EditorGUILayout.Slider(mpdRoughness, 0f, 5f, "Roughness");
			EditorGUILayout.Slider(mpdRandomness, 0f, 1f, "Randomness"); 

			if(GUILayout.Button("Midpoint Displacement"))
			{
				terrain.MidpointDisplacement();
			}
		}
	}

	void ResetHeights()
	{
		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

		if(GUILayout.Button("Reset Heights"))
		{
			terrain.ResetTerrain();
		}
	}

	void LoadHeights()
	{
		showLoadHeights = EditorGUILayout.Foldout(showLoadHeights, "Load Heights");

		if(showLoadHeights)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Load Heights From Texture", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(heightMapTexture);
			EditorGUILayout.PropertyField(heightMapScale);

			if(GUILayout.Button("Load Texture"))
			{
				terrain.LoadHeightsFromTexture();
			}
		}
	}

	void RandomHeights()
	{
		showRandom = EditorGUILayout.Foldout(showRandom, "Random");

		if(showRandom)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Set Heights Between Random Values", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(randomHeightRange);

			if(GUILayout.Button("Random Heights"))
			{
				terrain.RandomTerrain();
			}
		}
	}

	void SinglePerlin()
	{
		showPerlinNoise = EditorGUILayout.Foldout(showPerlinNoise, "Single Perlin Noise");

		if(showPerlinNoise)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Perlin Noise", EditorStyles.boldLabel);
			EditorGUILayout.IntSlider(perlinOffsetX, 0, 10000, "X Offset");
			EditorGUILayout.IntSlider(perlinOffsetY, 0, 10000, "Y Offset");
			EditorGUILayout.Slider(perlinXScale, 0, 0.01f, "X Scale");
			EditorGUILayout.Slider(perlinYScale, 0, 0.01f, "Y Scale");
			EditorGUILayout.IntSlider(perlinOctaves, 1, 10, "Octaves");
			EditorGUILayout.Slider(perlinPersistence, 1, 10, "Persistence");
			EditorGUILayout.Slider(perlinHeightScale, 0, 1, "Height Scale");

			if(GUILayout.Button("Perlin"))
			{
				terrain.Perlin();
			}
		}
	}

	void MultiplePerlin()
	{
		showMultiplePerlin = EditorGUILayout.Foldout(showMultiplePerlin, "Multiple Perlin");

		if(showMultiplePerlin)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Multiple Perlin Noise", EditorStyles.boldLabel);

			perlinParametersTable = GUITableLayout.DrawTable(
				perlinParametersTable, perlinParameters, GUITableOption.Reorderable(true));

			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			if(GUILayout.Button("+"))
			{
				terrain.AddNewPerlin();
			}

			if(GUILayout.Button("-"))
			{
				terrain.RemovePerlin();
			}

			EditorGUILayout.EndHorizontal();

			if(GUILayout.Button("Apply Multiple Perlin"))
			{
				terrain.MultiplePerlinTerrain();
			}
		}
	}

	void Voronoi()
	{
		showVoronoi = EditorGUILayout.Foldout(showVoronoi, "Voronoi");

		if(showVoronoi)
		{
			EditorGUILayout.IntSlider(voronoiPeaks, 1, 10, "Peak Count");
			EditorGUILayout.Slider(voronoiFallOff, 0f, 10f, "Fall Off");
			EditorGUILayout.Slider(voronoiDropOff, 0f, 10f, "Drop Off");
			EditorGUILayout.Slider(voronoiMinHeight, 0, 1, "Min Height");
			EditorGUILayout.Slider(voronoiMaxHeight, 0, 1, "Max Height");
			EditorGUILayout.PropertyField(voronoiType);

			if(GUILayout.Button("Voronoi"))
			{
				terrain.Voronoi();
			}
		}
	}
}
