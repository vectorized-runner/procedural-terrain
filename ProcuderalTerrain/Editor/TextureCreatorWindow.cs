using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq; 

public class TextureCreatorWindow : EditorWindow
{
	string fileName = "proceduralTexture";
	float perlinXScale;
	float perlinYScale;
	int perlinOctaves;
	float perlinePersistance;
	float perlineHeightScale;
	int perlinOffsetX;
	int perlinOffsetY;
	bool alphaToggle = false;
	bool seamlessToggle = false;
	bool mapToggle = false;

	float brightness = 0.5f;
	float contrast = 0.5f;

	Texture2D pTexture;

	[MenuItem("Window/TextureCreatorWindow")]
	public static void ShowWindow()
	{
		GetWindow<TextureCreatorWindow>(); 
	}

	private void OnEnable()
	{
		pTexture = new Texture2D(513, 513, TextureFormat.ARGB32, false);
	}

	private void OnGUI()
	{
		GUILayout.Label("Settings", EditorStyles.boldLabel);
		fileName = EditorGUILayout.TextField("Texture Name", fileName);

		int wSize = (int)(EditorGUIUtility.currentViewWidth - 100);

		perlinXScale = EditorGUILayout.Slider("X Scale", perlinXScale, 0, 0.1f);
		perlinYScale = EditorGUILayout.Slider("Y Scale", perlinYScale, 0, 0.1f);
		perlinOctaves = EditorGUILayout.IntSlider("Octaves", perlinOctaves, 1, 10);
		perlinePersistance = EditorGUILayout.Slider("Persistance", perlinePersistance, 1, 10);
		perlineHeightScale = EditorGUILayout.Slider("Height Scale", perlineHeightScale, 0, 1);
		perlinOffsetX = EditorGUILayout.IntSlider("Offset X", perlinOffsetX, 0, 10000);
		perlinOffsetY = EditorGUILayout.IntSlider("Offset Y", perlinOffsetY, 0, 10000);
		brightness = EditorGUILayout.Slider("Brightness", brightness, 0, 2);
		contrast = EditorGUILayout.Slider("Contrast", contrast, 0, 2);
		alphaToggle = EditorGUILayout.Toggle("Alpha", alphaToggle);
		mapToggle = EditorGUILayout.Toggle("Map", mapToggle);
		seamlessToggle = EditorGUILayout.Toggle("Seamless", seamlessToggle);

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();

		float minColor = 1;
		float maxColor = 0;

		if(GUILayout.Button("Generate", GUILayout.Width(wSize)))
		{
			int w = 513;
			int h = 513;
			float pValue;
			Color pixCol = Color.white;

			for(int y = 0; y < h; ++y)
			{
				for(int x = 0; x < w; ++x)
				{
					if(seamlessToggle)
					{
						float u = x / w;
						float v = y / h;

						float noise00 = TerrainUtility.FractalBrownianMotion((x + perlinOffsetX) * perlinXScale,
											(y + perlinOffsetY) * perlinYScale,
											perlinOctaves,
											perlinePersistance) * perlineHeightScale;
						float noise01 = TerrainUtility.FractalBrownianMotion((x + perlinOffsetX) * perlinXScale,
											(y + perlinOffsetY + h) * perlinYScale,
											perlinOctaves,
											perlinePersistance) * perlineHeightScale;
						float noise10 = TerrainUtility.FractalBrownianMotion((x + perlinOffsetX + w) * perlinXScale,
											(y + perlinOffsetY) * perlinYScale,
											perlinOctaves,
											perlinePersistance) * perlineHeightScale;
						float noise11 = TerrainUtility.FractalBrownianMotion((x + perlinOffsetX + w) * perlinXScale,
											(y + perlinOffsetY + h) * perlinYScale,
											perlinOctaves,
											perlinePersistance) * perlineHeightScale;
						float noiseTotal = u * v * noise00 +
											u * (1 - v) * noise01 +
											(1 - u) * v * noise10 +
											(1 - u) * (1 - v) * noise11;
						float value = (int)(256 * noiseTotal) + 50;
						float r = Mathf.Clamp((int)noise00, 0, 255);
						float g = Mathf.Clamp(value, 0, 255);
						float b = Mathf.Clamp(value + 50, 0, 255);
						float a = Mathf.Clamp(value + 100, 0, 255);

						pValue = (r + g + b) / (3 * 255.0f);
					}
					else
					{
						pValue = TerrainUtility.FractalBrownianMotion((x + perlinOffsetX) * perlinXScale,
											(y + perlinOffsetY) * perlinYScale,
											perlinOctaves,
											perlinePersistance) * perlineHeightScale;
					}
					float colValue = contrast * (pValue - 0.5f) + 0.5f * brightness;
					if(minColor > colValue) minColor = colValue;
					if(maxColor < colValue) maxColor = colValue;
					pixCol = new Color(colValue, colValue, colValue, alphaToggle ? colValue : 1);
					pTexture.SetPixel(x, y, pixCol);
				}
			}

			if(mapToggle)
			{
				for(int y = 0; y < h; ++y)
				{
					for(int x = 0; x < w; ++x)
					{
						pixCol = pTexture.GetPixel(x, y);
						float colValue = pixCol.r;
						colValue = TerrainUtility.Map(colValue, minColor, maxColor, 0, 1);
						pixCol.r = colValue;
						pixCol.g = colValue;
						pixCol.b = colValue;
						pTexture.SetPixel(x, y, pixCol);
					}
				}
			}

			pTexture.Apply(false, false);
		}

		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(pTexture, GUILayout.Width(wSize), GUILayout.Height(wSize));
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();

		if(GUILayout.Button("Save", GUILayout.Width(wSize)))
		{
			CreatePNGFromTex();
			MakeTexReadable(); 
		}

		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}

	void MakeTexReadable()
	{
		// Make texture readable by default (since you want to use it as heightmap) 
		string assetPath = "Assets/SavedTextures/" + fileName + ".png";
		AssetDatabase.Refresh();
		TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

		if(textureImporter != null)
		{
			textureImporter.isReadable = true;
		}

		AssetDatabase.ImportAsset(assetPath);
		AssetDatabase.Refresh();
	}

	void CreatePNGFromTex()
	{
		// Create png file from the texture 
		byte[] bytes = pTexture.EncodeToPNG();
		string path = Application.dataPath + "/SavedTextures";
		if(!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		File.WriteAllBytes(path + "/" + fileName + ".png", bytes);
	}
}
