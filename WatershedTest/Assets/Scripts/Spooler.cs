﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using VTL.SimTimeControls;
using System.Threading;
using VTL;
using VTL.TrendGraph;


public struct Frame
{
	public Sprite Picture;
	public DateTime starttime;
	public DateTime endtime;
	public float[,] Data;
};


public class FrameEndDateAscending : IComparer<Frame>
{
	public int Compare(Frame first, Frame second)
	{
		if (first.starttime > second.starttime) { return 1; }
		else if (first.starttime == second.starttime) { return 0; }
		else { return -1; }
	}
}


public class Spooler : MonoBehaviour
{
	float frameRatio = 0.0f;
	bool WMS = false;
    static readonly object LOCK;
	public TrendGraphController trendGraph;
	string selectedModelRun="";
	public ModelRunVisualizer visual;
	Simulator simulator = new Simulator();
	// This will hold all of the Reel...
	List<Frame> Reel = new List<Frame>();
	public Image testImage;
	public Slider gridSlider;
	public TimeSlider timeSlider;
	public Queue<DataRecord> SliderFrames = new Queue<DataRecord>();
	public Projector TimeProjector;
    public Material colorWindow, colorProjector, slideProjector;
    private ColorPicker colorPicker;
    private ModelRun modelrun;
    public GameObject cursor;
    
	// BoundingBox used for the time series graph...
	public Rect BoundingBox;
	
	public void GenerateTimeSeries()
	{
		
	}
	
	int TOTAL = 0;
	private static int CompareFrames(Frame x, Frame y)
	{
		if (x.starttime == y.starttime)
		{
			return 0;
		}
		else if (x.starttime < y.starttime)
		{
			return -1;
		}
		return 1;
	}
	
	Vector2 NormalizedPoint=Vector2.zero;
	
	// Use this for initialization
	void Start()
	{
		//testImage.sprite = Reel[0].Picture;
		point = 0;
        colorPicker = GameObject.Find("ColorSelector").GetComponent<ColorPicker>();
        if (swapProjector)
        {
            TimeProjector.material = colorProjector;
            testImage.material = colorWindow;
        }
        else
        {
            TimeProjector.material = slideProjector;
        }
	}
	int count = 10;

    bool swapProjector = true;
	float point;
	public Text downloadTextBox;
	public Text selectedVariableTextBox;
    transform tran;
    // Update is called once per frame
    void Update()
    {
        if (swapProjector && colorPicker.ColorBoxes.Count > 0)
        {
			// Add the colors to the timeprojector and image
			for(int i = 0; i < 6; i++)
			{
				TimeProjector.material.SetColor("_SegmentData00" + i.ToString(), colorPicker.ColorBoxes[i].GetComponent<Image>().color);
				testImage.material.SetColor("_SegmentData00" + i.ToString(), colorPicker.ColorBoxes[i].GetComponent<Image>().color);
			}

			// Add the ranges to the timeprojector and image
			for(int i = 0; i < 5; i++)
			{
				TimeProjector.material.SetFloat("_x" + i.ToString(), (float.Parse(colorPicker.ColorBoxes[i].transform.GetChild(0).GetComponent<Text>().text)));
				testImage.material.SetFloat("_x" + i.ToString(), (float.Parse(colorPicker.ColorBoxes[i].transform.GetChild(0).GetComponent<Text>().text)));
			}

            TimeProjector.material.SetInt("_NumLines", (int)gridSlider.value);

        }

        // Debug.LogError("NEW COUNT: " + Reel.Count);
        TimeProjector.material.SetVector("_Point", NormalizedPoint);

        // Enable the point to be used.
        TimeProjector.material.SetInt("_UsePoint", 1);
        
        if (SliderFrames.Count > 0 && count > 0)
        {
			// Set a dequeue size for multiple dequeus in one update
            int dequeueSize = 5;

            if (SliderFrames.Count < dequeueSize)
            {
                dequeueSize = SliderFrames.Count;
            }
            
            // Run the dequeue dequeueSize times
            for (int i = 0; i < dequeueSize; i++)
            {
                // Clear time series gra
                DataRecord record = SliderFrames.Dequeue();
                
                if (Reel.Count == 0)
                {
                    // Set projector...
                    Utilities utilites = new Utilities();

                    utilites.PlaceProjector2(TimeProjector, record);
                    if(record.bbox2 != "" && record.bbox2 != null)
                    {
                        Debug.LogError("We added BBox TWO.");
                    	BoundingBox = Utilities.bboxSplit(record.bbox2);
                    }
                    else
                    {
                        Debug.LogError("We added BBox ONE.");
						BoundingBox = Utilities.bboxSplit(record.bbox);
                    }

                    // Set the bounding box to the trendgraph
                    trendGraph.SetBoundingBox(BoundingBox);

                    tran = new transform();
                    Debug.LogError("Coord System: " + record.projection);
                    tran.createCoordSystem(record.projection); // Create a coordinate transform
                    //Debug.Log("coordsystem.transformToUTM(record.boundingBox.x, record.boundingBox.y)" + coordsystem.transformToUTM(record.boundingBox.x, record.boundingBox.y));

                    tran.setOrigin(coordsystem.WorldOrigin);
                    Vector2 point = tran.transformPoint(new Vector2(BoundingBox.x, BoundingBox.y));
                    Vector2 point2 = tran.transformPoint(new Vector2(BoundingBox.x + BoundingBox.width, BoundingBox.y - BoundingBox.height));
                    // BoundingBox = new Rect(point.x, point.y, Math.Abs(point.x - point2.x), Math.Abs(point.y - point2.y));
                    // Debug.LogError(BoundingBox);
                }

                count--;
                //Debug.LogError("BUILDING BAND NUM: "  + record.band_id);
                textureBuilder(record);

                if(record.Max > modelrun.MinMax[oldSelectedVariable].y)
                {
                    float max = record.Max;
                    modelrun.MinMax[oldSelectedVariable] = new SerialVector2(new Vector2(modelrun.MinMax[oldSelectedVariable].x, max)); 
                    TimeProjector.material.SetFloat("_FloatMax", max);
                    testImage.material.SetFloat("_FloatMax", max);
                    colorPicker.SetMax(max);
                    trendGraph.SetMax((int)max);
                }
                if(record.Min < modelrun.MinMax[oldSelectedVariable].x)
                {
                    float min = record.Min;
                    modelrun.MinMax[oldSelectedVariable] = new SerialVector2(new Vector2(min, modelrun.MinMax[oldSelectedVariable].y));
                    TimeProjector.material.SetFloat("_FloatMin", min);
                    testImage.material.SetFloat("_FloatMin", min);
                    colorPicker.SetMin(min);
                    trendGraph.SetMin((int)min);
                }
            }
            if (downloadTextBox)
            {
                downloadTextBox.text = "Downloaded: " + ((float)Reel.Count / (float)TOTAL).ToString("P");
            }

			timeSlider.SetTimeDuration(Reel[0].starttime, Reel[Reel.Count - 1].endtime, Math.Min((float)(Reel[Reel.Count - 1].endtime-Reel[0].starttime).TotalHours,30*24));

        }
        
		if (Input.GetMouseButtonDown (0) && cursor.GetComponent<mouselistener>().state == cursor.GetComponent<mouselistener>().states[1]) 
		{
			// Check if mouse is inside bounding box 
			Vector3 WorldPoint = coordsystem.transformToWorld(mouseray.CursorWorldPos);
			Vector2 CheckPoint = new Vector2(WorldPoint.x,WorldPoint.z);

			if( BoundingBox.Contains(CheckPoint) && !WMS)
			{
				// Debug.LogError("CONTAINS " + CheckPoint + " Width: " + BoundingBox.width + " Height: " +  BoundingBox.height);
                NormalizedPoint = TerrainUtils.NormalizePointToTerrain(WorldPoint, BoundingBox);
                trendGraph.SetCoordPoint(WorldPoint);
                trendGraph.SetPosition(Reel[textureIndex].Data.GetLength(0) - 1 - (int)Math.Min(Math.Round(Reel[textureIndex].Data.GetLength(0) * NormalizedPoint.x), (double)Reel[textureIndex].Data.GetLength(0) - 1),
                                       Reel[textureIndex].Data.GetLength(1) - 1 - (int)Math.Min(Math.Round(Reel[textureIndex].Data.GetLength(1) * NormalizedPoint.y), (double)Reel[textureIndex].Data.GetLength(1) - 1));
			}
		}

        // This if statement is used for debugging code
        if(Input.GetKeyDown(KeyCode.L))
        {
            Debug.LogError("The count / total: " + Reel.Count + " / " + TOTAL);
            // trendGraph.SetPosition(50, 50);
            // trendGraph.PresetData();
        }
    }

    public void textureBuilder(DataRecord rec)
    {

		// Caching 
		if (!FileBasedCache.Exists (rec.id))  
		{
            //Debug.LogError("INSERTING INTO CACHE " + rec.id);
			FileBasedCache.Insert<DataRecord>(rec.id,rec);
		}
        else
        {
            //Debug.LogError("IN CACHE " + rec.id);
        }
		
		// Else update the cache
		if (rec.modelRunUUID != selectedModelRun) 
		{
			// This is not the model run we want because sometime else was selected.
			return;
		}
		
		// Frame to pass in
		Frame frame = new Frame();
		Utilities utilities = new Utilities();
		
		frame.starttime = rec.start.Value;
		frame.endtime = rec.end.Value;
		frame.Data = rec.Data;
        if(rec.Data == null)
        {
            Debug.LogError("The data at UUID = " + rec.id + " was null.");
            return;
        }
        trendGraph.Add(rec.start.Value, 1.0f, rec.Data);
		//Debug.LogError(rec.start + " | " + rec.end);
		Logger.enable = true;
		//frame.Picture = Sprite.Create(new Texture2D(100, 100), new Rect(0, 0, 100, 100), Vector2.zero);
		Texture2D tex = new Texture2D(rec.width, rec.height);
		if(!WMS)
	    {
            tex = utilities.BuildDataTexture(rec.Data, out rec.Min, out rec.Max);
        	//tex = utilities.buildTextures (utilities.normalizeData(rec.Data), Color.grey, Color.green);s
        }
        else
        {
        	tex.LoadImage(rec.texture);
        }
        
        //utilities.buildGradientContourTexture( frame.Data,new List<Color>{ Color.clear,Color.red,Color.blue,Color.green},new List<float> { 0.01f, 0.5f, 1.0f });

		for(int i = 0; i < tex.width; i++)
		{
			
			tex.SetPixel(i,0,Color.clear);
			tex.SetPixel(i,tex.height-1,Color.clear);
		}
		for(int i = 0; i < tex.height; i++)
		{
			tex.SetPixel(tex.width-1,i,Color.clear);
			tex.SetPixel(0,i,Color.clear);
		}
		
		tex.Apply ();
        frame.Picture = Sprite.Create(tex, new Rect(0, 0, 100, 100), Vector2.zero);//new Texture2D();// Generate Sprite
		//tex.EncodeToPNG()
		 //File.WriteAllBytes(Application.dataPath + "/../"+frame.endtime.Year + "" + frame.endtime.Month+".png",tex.EncodeToPNG());
		// second hand to spooler
		Insert(frame);
		count++;
		
	}
	
	void RandomMovie()
	{
		for (int i = 0; i < 10; i++)
		{
			AddRandomImage();
		}
	}
	
	public void AddRandomImage()
	{
		Texture2D image = new Texture2D(1000, 1000);
		Color[] colors = new Color[1000 * 1000];
		for (int i = 0; i < 1000; i++)
		{
			for (int j = 0; j < 1000; j++)
			{
				//Vector3 abc = UnityEngine.Random.onUnitSphere;
				float intensity = UnityEngine.Random.value;
				Color Set = Color.Lerp(Color.white,Color.black,UnityEngine.Random.value);
				Set.r = Set.r *intensity;
				Set.b = Set.b *intensity;
				Set.g = Set.g *intensity;
				colors[i * 1000 + j] = Set;
			}
		}
		image.SetPixels(colors);
		image.Apply();
		DateTime RandomTime = new DateTime(UnityEngine.Random.Range(1995, 2015), UnityEngine.Random.Range(1, 12), UnityEngine.Random.Range(1, 30));
		Debug.Log(RandomTime);
		Frame frame = new Frame();
		frame.Picture = Sprite.Create(image, new Rect(0, 0, 100, 100), Vector2.zero);
		frame.starttime = RandomTime;
		frame.endtime = RandomTime.AddHours(1.0);
		
		//Reel.Add(frame);
		// SortList();
		Insert(frame);
	}
	
	void SortList()
	{
		Reel.Sort(CompareFrames);
		timeSlider.SetStartTime(Reel[0].starttime);
		//timeSlider.si
	}
	
	
	void LoadModelRun()
	{
		
	}
	
	void Insert(Frame frame)
	{
		// Does this handle duplicates..
		int index = Reel.BinarySearch(frame,new FrameEndDateAscending());

        if(Reel.Count >= TOTAL)
        {
            Debug.LogError("Why is there more records being added to the Reel?");
            Debug.LogError("Here is out frame starttime: " + frame.starttime + " and the count is: " + count);
        }

		//if index >= 0 there is a duplicate 
		if(index >= 0)
		{
			//handle the duplicate!
			//throw new Exception("Duplicate Handling not implemented!!!!!");
            TOTAL--;
		}
		else
		{
			// new item
			// Debug.LogError("INSERTTING FRAMME " + ~index);
			Reel.Insert(~index, frame);
		}
	}
	
	
	// This can handle WMS Requests
	
	public void Insert(DataRecord data, bool FromData)
	{
		var frame = new Frame();
        Texture2D image = new Texture2D(data.width, data.height);
		if (!FromData)
		{
			image.LoadImage(data.texture);
			
			
		}
		else
		{
			// Build a color map from Raw Data...
			// Create a sprite
            frame.Picture = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 100, 100), new Vector2(0, 0));
		}
		// Create a sprite
        frame.Picture = Sprite.Create(image, new Rect(0, 0, 100, 100), new Vector2(0, 0));
		// Attached an associate Date Time Object
		frame.starttime = data.start.Value;
		frame.endtime = data.end.Value;
		//Reel.Add(frame);
		//SortList();
		Insert(frame);
	}
	
	int FindNearestFrame(DateTime Time)
	{
		Frame temp = new Frame();
		temp.starttime = Time;
		int index = Reel.BinarySearch(temp, new FrameEndDateAscending());
		//Debug.LogError(index);
		return index < 0 ? ~index-1 : index; 
	}
	
	// This will handle wcs related requests..
	void HandDataToSpooler(List<DataRecord> Records)
	{
		//Add Caching here.
        /*if (FileBasedCache.Exists(Records[0].id))
        {
            FileBasedCache.Insert<DataRecord>(Records[0].id, Records[0]);
        }*/
        

		// Handing to spooler 
		// first build a color map
		SliderFrames.Enqueue(Records[0]);
	}
	
	
	int previ=0,prevj=0;
    string oldSelectedVariable;
    public void LoadSelected()
    {		
		// Load this 
		var temp = visual.listView.GetSelectedModelRuns();
		var seled = visual.listView.GetSelectedRowContent();
		string variable = seled[0][2].ToString();
        trendGraph.SetUnit(variable);

		selectedVariableTextBox.text = "Current Model Run: " + seled[0][0].ToString() + " Variable: "+ variable;

		if (selectedModelRun != "") 
		{
			//ModelRunManager.RemoveRecordData(selectedModelRun);

			Reel.Clear();
            SliderFrames.Clear();
            trendGraph.Clear();
			// Add some code here to handle the time slider.
			//timeslider.reset();
            modelrun = ModelRunManager.GetByUUID(selectedModelRun);
            modelrun.ClearData(oldSelectedVariable);
		}
		if(temp!= null)
		{
			
			// Time to load some things
			//ModelRunManager.
			//temp[0]
			SystemParameters sp = new SystemParameters();
			
			// This is not getting passed into WCS UGH! Right now width and height come out to equal 0!!!!!!
			
			sp.interpolation = "bilinear";

			var Records = temp[0].FetchVariableData(variable);
			TOTAL = Records.Count;
			selectedModelRun = temp[0].ModelRunUUID;
            modelrun = ModelRunManager.GetByUUID(selectedModelRun);
            oldSelectedVariable = variable;
			Logger.WriteLine("Load Selected: Null with Number of Records: " + Records.Count);

            sp.width = 100;
            sp.height = 100;
			 

			if(temp[0].Description.ToLower().Contains("doqq"))
		    {
		    	WMS=true;
                TimeProjector.material = slideProjector;
				ModelRunManager.Download(Records, HandDataToSpooler, param: sp, operation: "wms");
			}
			else
		    {
		    	WMS=false;
                TimeProjector.material = colorProjector;
                testImage.material = colorWindow;
				ModelRunManager.Download(Records, HandDataToSpooler, param: sp);
			}
		}
		//visual.listView.GetSelectedModelRuns()[0];
	}
	
	
	private int textureIndex = 0;
	int prevTextureIndex = 0;
	
	public void ChangeTexture()
	{
		if (Reel.Count > 0)
		{
			textureIndex = FindNearestFrame(timeSlider.SimTime);//(int)slider.value;

            // Update the index on the trendgraph
            trendGraph.SetDataIndex(textureIndex);
			
			//Debug.Log("CHANGING");
			//Debug.Log(textureIndex);
			// Debug.Log(timeSlider.SimTime);
			//Debug.Log(textureIndex);
			if (textureIndex < 0)
			{
				textureIndex = 0;
			}
			else if (textureIndex >= Reel.Count)
			{
				textureIndex = Reel.Count - 1;
			}
			
			testImage.sprite = Reel[textureIndex].Picture;
			int i = -1;
			int j = -1;
			if(!WMS)
			{
				i = (int)Math.Min(Math.Round(Reel[textureIndex].Data.GetLength(0)*NormalizedPoint.x),(double)Reel[textureIndex].Data.GetLength(0)-1);
				j = (int)Math.Min(Math.Round(Reel[textureIndex].Data.GetLength(1)*NormalizedPoint.y),(double)Reel[textureIndex].Data.GetLength(1)-1);
			}
			if(previ != i || prevj != j)
			{
				// Clear time series graph
				
				// set new previ and prevj
				previ = i;
				prevj = j;
			}
			
			if(textureIndex != prevTextureIndex)
			{
				// Send data to time series graph.
				// trendGraph.Add(Reel[textureIndex].starttime,Reel[textureIndex].Data[i,j]);
			}
			
			// Set projector image
			if(textureIndex == Reel.Count - 1 || Reel.Count < 2)
			{
				//Debug.LogError("End of the Reel");
				// Set both textures to last reel texture
				TimeProjector.material.SetTexture("_ShadowTex",Reel[Reel.Count-1].Picture.texture);
				TimeProjector.material.SetTexture("_ShadowTex2",Reel[Reel.Count-1].Picture.texture);

                testImage.material.SetTexture("_MainTex", Reel[Reel.Count - 1].Picture.texture);
                testImage.material.SetTexture("_MainTex2", Reel[Reel.Count - 1].Picture.texture);
			}
			else
			{
				//Debug.LogError("Reeling");
				// Set current texture
				TimeProjector.material.SetTexture("_ShadowTex",Reel[textureIndex].Picture.texture);
				
				// Set future texture
				TimeProjector.material.SetTexture("_ShadowTex2",Reel[textureIndex+1].Picture.texture);

                testImage.material.SetTexture("_MainTex", Reel[textureIndex].Picture.texture);
                testImage.material.SetTexture("_MainTex2", Reel[textureIndex + 1].Picture.texture);
			}
		}
	}

    
}
