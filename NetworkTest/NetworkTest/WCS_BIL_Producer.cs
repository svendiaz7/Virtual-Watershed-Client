﻿using System;
// We will need to change this when the time comes around to do the networking version...
using System.IO;

/// <summary>
/// The following WCS Product is used to download any data and populate the passed in DataRecord.
/// </summary>
public class WCS_BIL_Producer : DataProducer
{
    // Fields
    NetworkManager nm;
    mimeparser mp = new mimeparser();

    // Possible constructor
    public WCS_BIL_Producer( NetworkManager refToNM ) 
    {
        nm = refToNM;
    }

    ///////////////////////////////////////////////////////////////////////////
    // Overrides Below
    ///////////////////////////////////////////////////////////////////////////
    protected override DataRecord ImportFromURL(DataRecord Record, string path, int priority = 1)
    {
        // Beautiful Lambda here
        // Downloads the bytes and uses the ByteFunction lambda described in the passed parameter which will call the mime parser and populate the record.
        //nc.DownloadBytes(path, ((DownloadBytes) => mp.Parse(Record, DownloadBytes)), priority);
        nm.AddDownload(new DownloadRequest(path, (ByteFunction)((DownloadBytes) => mp.Parse(Record, DownloadBytes)), priority));

        // Return
        return Record;
    }

    protected override DataRecord ImportFromFile(DataRecord Record, string path)
    {
        // Get the file name
        string filename = Path.GetFileNameWithoutExtension(path);
        string fileDirPath = Path.GetDirectoryName(path);
        string hdrName = fileDirPath + '\\' + filename + ".hdr";
        string bilName = fileDirPath + '\\' + filename + ".bil";

        if (File.Exists(hdrName) && File.Exists(bilName))
        {
            // Open the files
            StreamReader hdrReader = new StreamReader(hdrName);
            FileInfo bilFI = new FileInfo(bilName);
            BinaryReader bilReader = new BinaryReader(File.Open(bilName, FileMode.Open));

            // Read the header and data
            string header = hdrReader.ReadToEnd();
            byte[] dataBytes = bilReader.ReadBytes((int)bilFI.Length);

            // Save into record
            Record.Data = bilreader.parse(header, dataBytes);

            // Close the files
            hdrReader.Dispose();
            bilReader.Close();

            // Return
            return Record;
        }
        else
        {
            // Throw an exception that the file does not exist
            throw new System.ArgumentException("File does not exist: " + path);
        }
    }

    public override bool ExportToFile(string Path, string outputPath, string outputName)
    {
        // The getType function will determine the type of transfer (file or url) and strip off special tokens to help determine the type.
        TransferType type = getType(ref Path);

        // Put Try Catch HERE
        // If file does not exist 
        if (type == TransferType.URL)
        {
            // Beautiful Lambda here
            // Downloads the bytes and uses the ByteFunction lambda described in the passed parameter which will call the mime parser and populate the record.
            // Network Manager download
            Console.WriteLine("URL: " + Path);
            nm.AddDownload(new DownloadRequest(Path, (ByteFunction)((DownloadBytes) => mp.Parse(outputPath,outputName,DownloadBytes))));
        }

        // Return
        return true;
    }
}
