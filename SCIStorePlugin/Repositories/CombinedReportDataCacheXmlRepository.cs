using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Serialization;
using Rdmp.Core.Caching.Layouts;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Cache;
using NLog;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories;

public class CombinedReportDataCacheXmlRepository : IRepository<CombinedReportData>
{
    private readonly ICacheLayout _cacheLayout;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly XmlSerializer Serialiser = new (typeof(CombinedReportData));


    public CombinedReportDataCacheXmlRepository(ICacheLayout cacheLayout)
    {
        _cacheLayout = cacheLayout;
    }

    /// <summary>
    /// The information will be in the filesystem under two directories: T and F, for the relevant health board
    /// </summary>
    /// <returns></returns>
    public IEnumerable<CombinedReportData> ReadAll()
    {
        Log.Info($"Reading all data from {_cacheLayout.RootDirectory}");

        if (_cacheLayout.RootDirectory == null)
            throw new Exception("Root directory can't be null");

        var rootDir = _cacheLayout.RootDirectory;
        var reports = new List<CombinedReportData>();

        ((SCIStoreCacheLayout)_cacheLayout).ValidateLayout();

        var subDirs = rootDir.EnumerateDirectories().ToList();
        if (!subDirs.Any())
        {
            // the report files have not been unzipped in T/F directories
            // however the XML should contain an HbExtract tag
            ReadDir(reports, rootDir);
        }
        else
        {
            foreach (var path in rootDir.EnumerateDirectories())
            {
                var hbExtract = path.Name;
                if (hbExtract is "T" or "F")
                    ReadDir(reports, path, hbExtract);
                else
                    throw new Exception($"Unknown Health Board reference: {hbExtract}");
            }
        }

        Log.Info($"Read {reports.Count} reports");
        return reports;
    }

    private static void ReadDir(ICollection<CombinedReportData> reports, DirectoryInfo rootPath)
    {
        Log.Info($"Reading reports from {rootPath}");
        var serializer = new XmlSerializer(typeof(CombinedReportData));
        foreach (var file in rootPath.EnumerateFiles("*.xml"))
        {
            var report = ReadFile(file, serializer);

            if (report.HbExtract == null)
                throw new Exception($"Lab {report.SciStoreRecord.LabNumber} has no HbExtract tag");

            reports.Add(report);
        }
    }

    private static void ReadDir(List<CombinedReportData> reports, DirectoryInfo rootPath, string hbExtract)
    {
        Log.Info($"Reading reports from {rootPath}");
        reports.AddRange(rootPath.EnumerateFiles("*.xml").Select(file => ReadFile(hbExtract, file, Serialiser)));
    }

    public static CombinedReportData ReadFile(string hbExtract, FileInfo file, XmlSerializer serializer)
    {
        var report = ReadFile(file, serializer);
        report.HbExtract = hbExtract;
        return report;
    }

    public static CombinedReportData ReadFile(FileInfo file, XmlSerializer serializer)
    {
        using var stream = new StreamReader(file.FullName);
        try
        {
            return serializer.Deserialize(stream) as CombinedReportData ?? throw new Exception("Couldn't deserialise the CombinedReportData object");
        }
        catch (InvalidOperationException e)
        {
            throw new InvalidOperationException($"Error deserializing file {file}", e);
        }
    }

    public IEnumerable<CombinedReportData> ReadSince(DateTime day)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IEnumerable<CombinedReportData>> ChunkedReadFromDateRange(DateTime start, DateTime end, IDataLoadEventListener job)
    {
        throw new NotImplementedException();
    }

    public void Create(IEnumerable<CombinedReportData> reports, IDataLoadEventListener listener)
    {
        foreach (var report in reports)
        {
            if (report == null)
                throw new Exception("Could not cast CombinedReportData object");

            if (report.HbExtract != "T" && report.HbExtract != "F")
                throw new Exception($"Unknown Health Board parameter (HbExtract): {report.HbExtract}");

            try
            {
                var reportDir = _cacheLayout.GetLoadCacheDirectory(listener);
                if (!reportDir.Exists)
                    reportDir.Create();

                if (_cacheLayout.Resolver is not SCIStoreLoadCachePathResolver resolver)
                    throw new Exception("Could not cast the Resolver as a SCIStoreLoadCachePathResolver");

                var path = Path.Combine(reportDir.FullName, SCIStoreLoadCachePathResolver.GetFilename(report));



                //write results to file
                var sw = new StringWriter();

                try
                {
                    Serialiser.Serialize(sw,report);
                }
                catch(Exception ex)
                {
                    listener?.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning,"Failed to serialize report: serialiser.Serialize failed",ex));
                    continue;
                }

                sw.Flush();
                var text = sw.ToString();
                sw.Close();
                sw.Dispose();

                // ensure that the text is valid xml
                var doc = new XmlDocument();
                doc.LoadXml(text);

                foreach(XmlNode t in doc.GetElementsByTagName("LastEncounteredDate"))
                {
                    t.InnerText = "0001-01-01";
                }

                foreach (XmlNode t in doc.GetElementsByTagName("LastEncounteredTime"))
                {
                    t.InnerText = "00:00:00.0000000+00:00";
                }

                doc.Save(path);

                listener?.OnProgress(this, new ProgressEventArgs(path, new ProgressMeasurement((int)(new FileInfo(path).Length * 0.001), ProgressType.Kilobytes), TimeSpan.Zero));
            }
            catch (Exception e)
            {
                throw new Exception($"Could not open stream to write CombinedReportData files: {e}");
            }
        }
    }

    public static string MD5Stream(Stream stream)
    {
        using var md5 = MD5.Create();
        return BitConverter.ToString(md5.ComputeHash(stream));
    }

    public void DeleteAll()
    {
        throw new NotImplementedException();
        // todo: rethink this
/*

            foreach (var dir in Directory.EnumerateDirectories(_rootPath))
            {
                var info = new DirectoryInfo(dir);
                var hbExtract = info.Name;

                if (hbExtract == "T" || hbExtract == "F")
                {
                    foreach (var file in Directory.EnumerateFiles(dir))
                    {
                        File.Delete(file);
                    }
                    Directory.Delete(dir);
                }
                else
                    throw new Exception("Unknown Health Board directory: " + dir);
            }*/
    }
}