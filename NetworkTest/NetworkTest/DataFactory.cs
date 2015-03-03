﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Defines a general interface for parsing different file formats
/// </summary>
public abstract class Parser
{
    private string name = "Unnamed Parser";

    /// <summary>
    /// This function parses any dataset given to it and populates the DataRecord with the results
    /// </summary>
    /// <param name="record">The DataRecord to be populated</param>
    /// <param name="Contents">The file to be parsed into the DataRecord</param>
    /// <returns>DataRecord containing information of the parsed "Contents" file</returns>
    public virtual DataRecord Parse(DataRecord record, string Contents)
    {
        throw new System.NotImplementedException();
    }

    // Define custom functionality here (i.e. writing to files)
    public virtual void Parse(string Path, string OutputName, string Contents)
    {
        throw new System.NotImplementedException();
    }

    // Define custom functionality here (i.e. writing to files)
    public virtual void Parse(string Path, string OutputName, byte[] Contents)
    {
        throw new System.NotImplementedException();
    }

    public virtual DataRecord Parse(DataRecord record, byte[] Contents)
    {
        throw new System.NotImplementedException();
    }

    public string Name
    {
        get
        {
            return name;
        }
        set
        {
            name = value;
        }
    }
}

/// <summary>
/// This class is the base class describing how DataFactories will be constructed
/// + i.e. FileFactory or NetworkingFactory
/// </summary>
public class DataFactory
{
    // Maps Strings to the their corresponding Producers
    private NetworkManager manager = new NetworkManager(4);
    protected Dictionary<String, DataProducer> Products = new Dictionary<String, DataProducer>();

    public DataFactory()
    {
        // Initialize the products --- We need to streamline this in someway.....
        Products.Add("WCS_BIL", new WCS_BIL_Producer(manager));
        Products.Add("WMS_PNG", new WMS_PNG_Producer(manager));
        Products.Add("WCS_1", null);
        Products.Add("WCS_2", null);
        Products.Add("WCS_3", null);
    }

    /// <summary>
    /// Creates a new DataProduct of the specified product type,
    /// with the correct DataProduct class functions and members
    /// </summary>
    /// <returns>A DataProduct of one specified type
    /// + i.e. WCS Product, WMS Product, WFS Product, etc.</returns>
    public DataRecord Import(String type, DataRecord Record, string Path, int priority = 1)
    {
        // Check if the product exists
        if( Products.ContainsKey( type ) )
        {
            return Products[type].Import(Record, Path, priority);
        }
        else
        {
            // Unsupported type
            throw new System.ArgumentException("Type: " + type + " is not supported.");
        }
    }

    public void Export(String type,string Path, string outputPath,string name)
    {
        // Check if the product exists
        if (Products.ContainsKey(type))
        {
            Products[type].ExportToFile(Path,outputPath,name);
        }
        else
        {
            // Unsupported type
            throw new System.ArgumentException("Type: " + type + " is not supported.");
        }
    }

    /// Some test download functions 
    void PrintString(string str)
    {
        Console.WriteLine(str);
    }
    void PrintBytes(byte[] bytes)
    {
        Console.WriteLine(bytes);
    }
    public void TestByteDownload(string url)
    {
        manager.AddDownload(new DownloadRequest(url, PrintBytes));
    }
    public void TestStringDownload(string url)
    {
        manager.AddDownload(new DownloadRequest(url,PrintString));
    }
}


