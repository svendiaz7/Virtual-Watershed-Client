﻿using UnityEngine;
using System.Collections;
using System;

public class ProjectorObject : WorldObject {
    // projectors are placed based on the center...


    bool Trans = false;
	// Use this for initialization
	void Start () {
        IsRaster = true;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public override bool saveData(string filename, string format = "")
    {
        return false;
    }

    public override bool moveObject(Vector3 displacement)
    {
        Offset = displacement;
        gameObject.transform.position += displacement;
        return true;
    }

    public override bool changeProjection(string projectionString)
    {
        return false;
    }

    public override void alterData()
    {

    }

    public override void getData()
    {

    }

    public override SessionObjectStructure saveSessionData()
    {
        SessionObjectStructure structure = new SessionObjectStructure();
        structure.Name = record.name;
        structure.GameObjectPosition = gameObject.transform.position;
        structure.GameObjectOffset += Offset;
        structure.Projection = record.projection;
        structure.Sources = record.services;
        //structure.Modified =
        //structure.Created = record.s Need to acquire created data for datarecord. --- In the json from the virtual watershed 
        return structure;
    }

    public void PlaceProjector(DataRecord record, out Vector2 BoundingScale)
    {
		Projector projector = gameObject.GetComponent<Projector> ();
        BoundingScale = Vector2.one;
        Debug.LogError("<size=16>We Are Still In A Temp Patch</size>");
		if (record.bbox2 == "" || record.bbox2 == null || (Math.Abs(Utilities.bboxSplit(record.bbox2).x) > 180 && Math.Abs(Utilities.bboxSplit(record.bbox2).y) > 180))
        {
			record.boundingBox = new SerialRect(Utilities.bboxSplit(record.bbox));
        }
        else
        {
			record.boundingBox = new SerialRect(Utilities.bboxSplit(record.bbox2));
        }
        PlaceProjector(record.boundingBox, record.projection, out BoundingScale);
    }

    public void PlaceProjector(Rect boundingBox, String projection, out Vector2 BoundingScale)
    {
        Projector projector = gameObject.GetComponent<Projector>();
        BoundingScale = Vector2.one;

        projector.gameObject.GetComponent<WorldTransform>();
        if (projector.gameObject.GetComponent<WorldTransform>() != null)
        {
            Component.Destroy(projector.gameObject.GetComponent<WorldTransform>());
        }
        var tran = projector.gameObject.AddComponent<WorldTransform>();
        tran.createCoordSystem(projection); // Create a coordinate worldTransform
        //Debug.Log("coordsystem.worldTransformToUTM(record.boundingBox.x, record.boundingBox.y)" + coordsystem.transformToUTM(record.boundingBox.x, record.boundingBox.y));

        //tran.setOrigin(coordsystem.WorldOrigin);

        Vector2 point = tran.transformPoint(new Vector2(boundingBox.x, boundingBox.y));
        Vector2 upperLeft = tran.translateToGlobalCoordinateSystem(new Vector2(boundingBox.x, boundingBox.y));
        //Vector2 upperRight = tran.translateToGlobalCoordinateSystem(new Vector2(boundingBox.x + boundingBox.width, boundingBox.y));
        Vector2 lowerRight = tran.translateToGlobalCoordinateSystem(new Vector2(boundingBox.x + boundingBox.width, boundingBox.y - boundingBox.height)); ;
        //Vector2 lowerLeft = tran.translateToGlobalCoordinateSystem(new Vector2(boundingBox.x, boundingBox.y - boundingBox.height));

        point = upperLeft;
        Vector3 pos = mouseray.raycastHitFurtherest(new Vector3(point.x, 0, point.y), Vector3.up);
        pos.y += 10;
        point = tran.translateToGlobalCoordinateSystem(new Vector2(boundingBox.x + boundingBox.width/2.0f, boundingBox.y + boundingBox.height/2.0f));

        Debug.LogError("POINT: " + point.x + " " + point.y);

        float dim = Math.Max(Math.Abs((upperLeft - lowerRight).x) / 2.0f, Math.Abs((upperLeft - lowerRight).y) / 2.0f);
        float dim2 = Math.Min(Math.Abs((upperLeft - lowerRight).x) / 2.0f, Math.Abs((upperLeft - lowerRight).y) / 2.0f);
        float offset = dim - dim2;

        pos = mouseray.raycastHitFurtherest(new Vector3(point.x, 0, point.y), Vector3.up);
        pos.y += 3000;

        if (Math.Abs((upperLeft - lowerRight).x) / 2.0f > Math.Abs((upperLeft - lowerRight).y) / 2.0f)
        {
            pos.z += offset;
        }
        else
        {
            pos.x += offset;
        }
        Debug.LogError("OFFSET: " + offset);
        //pos.x += dim;
        //pos.z += dim;
        //pos.x += Math.Abs((upperLeft - lowerRight).x) / 2.0f;
        //pos.z += Math.Abs((upperLeft - lowerRight).y) / 2.0f;
        projector.transform.position = pos;

        var pro = projector.GetComponent<Projector>();
        pro.farClipPlane = 10000;
        pro.orthographicSize = Math.Max(Math.Abs((upperLeft - lowerRight).x) / 2.0f, Math.Abs((upperLeft - lowerRight).y) / 2.0f);

        float boundingAreaX = Mathf.Abs((upperLeft.x - lowerRight.x) / (2.0f * pro.orthographicSize));
        float boundingAreaY = Mathf.Abs((upperLeft.y - lowerRight.y) / (2.0f * pro.orthographicSize));
        // Debug.LogError ("MAX X: " + boundingAreaX + " MAX Y: " + boundingAreaY);
        // Debug.LogError ("MAX X: " + (upperLeft.x - upperRight.x) + " MAX Y: " + (upperLeft.y - lowerLeft.y));
        pro.material = Material.Instantiate(pro.material);
        pro.material.SetFloat("_MaxX", boundingAreaX);
        pro.material.SetFloat("_MaxY", boundingAreaY);
        Debug.LogError("BOUNDING AREA X: " + boundingAreaX);
        Debug.LogError("BOUNDING AREA Y: " + boundingAreaY);
        Debug.LogError("ORTHOGRAPHIC SIZE X: " + pro.orthographicSize*2 * boundingAreaX);
        Debug.LogError("ORTHOGRAPHIC SIZE Y: " + pro.orthographicSize * 2 * boundingAreaY);
        Debug.LogError("BOUNDINGBOX HEIGHT: " + GlobalConfig.BoundingBox.height);
        Debug.LogError("BOUNDINGBOX WIDTH: " + GlobalConfig.BoundingBox.width);
        BoundingScale.x = boundingAreaX;
        BoundingScale.y = boundingAreaY;
    }

    // Here are some projector building functions that need to be addressed
    public GameObject buildProjector(DataRecord record, bool type = false)
    {
        // First Create a projector
        GameObject projector = this.gameObject;
        projector.name = "SPAWNED PROJECTOR";

        // Second Place the projector
        // Time to place this guy somewhere
        // Set Gameobject Transform
        var tran = projector.AddComponent<WorldTransform>();
        tran.createCoordSystem(record.projection); // Create a coordinate transform
        // Debug.Log("coordsystem.transformToUTM(record.boundingBox.x, record.boundingBox.y)" + coordsystem.transformToUTM(record.boundingBox.x, record.boundingBox.y));

        //tran.setOrigin(coordsystem.WorldOrigin);

        //Vector2 origin = new Vector2(record.boundingBox.x + record.boundingBox.width, record.boundingBox.y));

        // tran.setOrigin(origin);


        Vector2 point = tran.transformPoint(new Vector2(record.boundingBox.x, record.boundingBox.y));

        Vector2 upperLeft = tran.translateToGlobalCoordinateSystem(new Vector2(record.boundingBox.x, record.boundingBox.y));
        //Vector2 upperRight = tran.translateToGlobalCoordinateSystem(new Vector2(record.boundingBox.x + record.boundingBox.width, record.boundingBox.y));
        Vector2 lowerRight = tran.translateToGlobalCoordinateSystem(new Vector2(record.boundingBox.x + record.boundingBox.width, record.boundingBox.y - record.boundingBox.height)); ;
        //Vector2 lowerLeft = tran.translateToGlobalCoordinateSystem(new Vector2(record.boundingBox.x, record.boundingBox.y - record.boundingBox.height));

        point = upperLeft;
        Vector3 pos = mouseray.raycastHitFurtherest(new Vector3(point.x, 0, point.y), Vector3.up);
        pos.y += 10;


        // GO.transform.position = pos;
        // GO.transform.localScale = new Vector3 (100, 100, 100);

        point = new Vector2(lowerRight.x, upperLeft.y); // upperRight;
        pos = mouseray.raycastHitFurtherest(new Vector3(point.x, 0, point.y), Vector3.up);

        // GO2.transform.position = pos;
        // GO2.transform.localScale = new Vector3 (100, 100, 100);

        point = lowerRight;
        pos = mouseray.raycastHitFurtherest(new Vector3(point.x, 0, point.y), Vector3.up);

        // GO3.transform.position = pos;
        // GO3.transform.localScale = new Vector3 (100, 100, 100);

        point = new Vector2(upperLeft.x, lowerRight.y);//lowerLeft;
        pos = mouseray.raycastHitFurtherest(new Vector3(point.x, 0, point.y), Vector3.up);

        // GO4.transform.position = pos;
        // GO4.transform.localScale = new Vector3 (100, 100, 100);


        // Projector placement code
        point = tran.translateToGlobalCoordinateSystem(new Vector2(record.boundingBox.x, record.boundingBox.y));
        float dim = Math.Max(Math.Abs((upperLeft - lowerRight).x) / 2.0f, Math.Abs((upperLeft - lowerRight).y) / 2.0f);

        pos = mouseray.raycastHitFurtherest(new Vector3(point.x, 0, point.y), Vector3.up);
        pos.y += 3000;
        pos.x += dim;
        pos.z -= dim;
        projector.transform.position = pos;

        var pro = projector.GetComponent<Projector>();
        pro.farClipPlane = 10000;
        pro.orthographicSize = Math.Max(Math.Abs((upperLeft - lowerRight).x) / 2.0f, Math.Abs((upperLeft - lowerRight).y) / 2.0f);

        // Ignoring terrain layer with this created projector!
        pro.ignoreLayers = (1 << 8);


        float boundingAreaX = Mathf.Abs((upperLeft.x - lowerRight.x) / (2.0f * pro.orthographicSize));
        float boundingAreaY = Mathf.Abs((upperLeft.y - lowerRight.y) / (2.0f * pro.orthographicSize));
        // Debug.LogError ("MAX X: " + boundingAreaX + " MAX Y: " + boundingAreaY);
        // Debug.LogError ("MAX X: " + (upperLeft.x - upperRight.x) + " MAX Y: " + boundingAreaY);
        pro.material = Material.Instantiate(pro.material);
        pro.material.SetFloat("_MaxX", boundingAreaX);
        pro.material.SetFloat("_MaxY", boundingAreaY);
        pro.material.SetInt("_UsePoint", 0);
        pro.material.SetFloat("_Opacity", .678f);
        if (record.texture != null)
        {
            Vector2 BoundingScale;
            PlaceProjector(record, out BoundingScale);
            Texture2D image = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);
            image.wrapMode = TextureWrapMode.Clamp;
            image.LoadImage(record.texture);
            for (int i = 0; i < image.width; i++)
            {
                image.SetPixel(0, i, Color.clear);
                image.SetPixel(i, 0, Color.clear);
                image.SetPixel(image.width - 1, i, Color.clear);
                image.SetPixel(i, image.height - 1, Color.clear);
            }
            image.Apply();
            pro.material.SetTexture("_ShadowTex", image);
            pro.material.SetFloat("_MaxX", BoundingScale.x);
            pro.material.SetFloat("_MaxY", BoundingScale.y);
        }
        else if (record.Data.Count > 0)
        {
            Vector2 BoundingScale;
            PlaceProjector(record, out BoundingScale);
			Texture2D image = Utilities.buildTextures(Utilities.normalizeData(record.Data[0]), Color.grey, Color.blue);
            image.wrapMode = TextureWrapMode.Clamp;

            for (int i = 0; i < image.width; i++)
            {
                image.SetPixel(i, 0, Color.clear);
                image.SetPixel(i, image.height - 1, Color.clear);
            }

            for (int i = 0; i < image.height; i++)
            {
                image.SetPixel(0, i, Color.clear);
                image.SetPixel(image.width - 1, i, Color.clear);
            }
            image.Apply();

            pro.material.SetTexture("_ShadowTex", image);
            pro.material.SetFloat("_MaxX", BoundingScale.x);
            pro.material.SetFloat("_MaxY", BoundingScale.y);
        }

        // Third what type of data are we visualizing...
        // Determine if the data is a square or rect
        // if square add it to the project material
        //else it is a rectangle....
        // pad the texture with the approriate bounds
        // add it to the projector or material

        //var TempCol = projector.GetComponent<Projector>().material.color;
        //TempCol.a = .5f;
        //projector.GetComponent<Projector>().material.color = TempCol;

        // Return the object 
        return projector;
    }

    public override void Transpose()
    {
        Trans = !Trans;
        GetComponent<Projector>().material.SetInt("_Transpose", Trans ? 1 : 0);
    }
}
