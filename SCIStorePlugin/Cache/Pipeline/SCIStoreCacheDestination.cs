using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Rdmp.Core.Caching.Requests;
using Rdmp.Core.ReusableLibraryCode;
using SCIStorePlugin.Data;
using SCIStorePlugin.Repositories;
using Rdmp.Core.Caching.Pipeline.Destinations;
using Rdmp.Core.Caching.Layouts;

namespace SCIStorePlugin.Cache.Pipeline;

public class SCIStoreCacheDestination : CacheFilesystemDestination
{

    [DemandsInitialization("The healthboard you are trying to load - do not change this mid lifespan of your corporation - set once EVER")]
    public HealthBoard HealthBoard { get; set; }

    [DemandsInitialization("The type of lab to be retrieved")]
    public Discipline Discipline { get; set; }

    Dictionary<string,int> _xmlFileCountWrittenToEachZipFile = new();
    private SCIStoreCacheLayout _layout;

    public override ICacheLayout CreateCacheLayout()
    {
        return new SCIStoreCacheLayout(CacheDirectory, new SCIStoreLoadCachePathResolver(HealthBoard, Discipline));
    }

    public virtual SCIStoreCacheChunk ProcessPipelineData(SCIStoreCacheChunk toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        if(_layout == null)
            _layout = (SCIStoreCacheLayout) CreateCacheLayout();

        if (toProcess.Request == null)
            throw new InvalidOperationException("The current FetchRequest is null, meaning we have lost track of which cache fetch this chunk belongs to.");

        if(toProcess.HealthBoard != HealthBoard)
            throw new NotSupportedException(
                $"Cache ICacheChunk healthboard {toProcess.HealthBoard} did not match this destinations healthboard {HealthBoard}");

        if (toProcess.Discipline != Discipline)
            throw new NotSupportedException(
                $"Cache ICacheChunk Discipline {toProcess.Discipline} did not match this destinations Discipline {Discipline}");

        if (toProcess.DownloadRequestFailedException != null)
        {
            // There was an issue downloading from the web service. We'll insert the exception detail into the archive.
            var errorDirectory = new DirectoryInfo(Path.Combine(_layout.RootDirectory.FullName, "Errors", toProcess.HealthBoard.ToString(), toProcess.Discipline.ToString()));
            if (!errorDirectory.Exists)
            {
                errorDirectory.Create();
                errorDirectory.Refresh();
            }

            var errorFilename =
                $"{Path.Combine(errorDirectory.FullName, toProcess.FetchDate.ToString("yyyy-MM-dd HHmmss"))}.txt";
            File.WriteAllText(errorFilename, ExceptionHelper.ExceptionToListOfInnerMessages(toProcess.DownloadRequestFailedException));
            return toProcess;
        }
            
        if (cancellationToken.IsAbortRequested) return null;

        // Save out the combined reports as XML files

        var destRepo = new CombinedReportDataCacheXmlRepository(_layout);
        destRepo.Create(toProcess.CombinedReports, listener);

        if (cancellationToken.IsAbortRequested) return null;

        // Create an archive of the saved XML files
        var archiveDate = toProcess.FetchDate;

        var zipArchivePath = _layout.GetArchiveFileInfoForDate(archiveDate,listener).FullName;
            
        if(!_xmlFileCountWrittenToEachZipFile.ContainsKey(zipArchivePath))
            _xmlFileCountWrittenToEachZipFile.Add(zipArchivePath,0);
            
        var sw = new Stopwatch();
        sw.Start();

        //Get where the cache will be creating zip files
        var downloadDir = _layout.GetLoadCacheDirectory(listener);

        //get the list of files its about to cache (because sadly CreateArchive actually does the cleanup itself and doesnt tell us how many files it processed
        var xmlFileCount = downloadDir.EnumerateFiles("*.xml").Count();

        _layout.CreateArchive(archiveDate);

        if (cancellationToken.IsAbortRequested) return null;

        // clean up download directory (this is actually redundant double action since SciStoreCacheLayout already nukes all the xml files).
        downloadDir.EnumerateFiles("*.xml").ToList().ForEach(info => info.Delete());

        _xmlFileCountWrittenToEachZipFile[zipArchivePath] += xmlFileCount;
    
        listener.OnProgress(this,new ProgressEventArgs(zipArchivePath, new ProgressMeasurement(_xmlFileCountWrittenToEachZipFile[zipArchivePath], ProgressType.Records), sw.Elapsed));

        if (cancellationToken.IsAbortRequested) return null;

        // todo: move this to a base class so the plugin author does not need to remember (and forget) to do this
        toProcess.Request.RequestSucceeded();
            
        sw.Stop();

        return toProcess;
    }

    public override ICacheChunk ProcessPipelineData(ICacheChunk toProcess, IDataLoadEventListener listener,GracefulCancellationToken cancellationToken)
    {
        return ProcessPipelineData((SCIStoreCacheChunk) toProcess, listener, cancellationToken);
    }

    public override void Abort(IDataLoadEventListener listener)
    {
            
    }
}