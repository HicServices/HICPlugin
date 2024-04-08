using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataExport.DataExtraction.Commands;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace DrsPlugin.Extraction;

public abstract class ImageExtraction : IPluginDataFlowComponent<DataTable>, IPipelineRequirement<IExtractCommand>
{
    [DemandsInitialization("The path to the root of the identifiable image archive")]
    public string PathToImageArchive { get; set; }

    [DemandsInitialization("The name of the column in the DataTable which carries the image filename/uri")]
    public string ImageUriColumnName { get; set; }

    [DemandsInitialization("A pattern for the name of any dataset bundle that references images (bundles not matching this pattern will be ignored by this plugin)")]
    public Regex DatasetName { get; set; }

    protected IExtractDatasetCommand Request { get; private set; }

    public abstract DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken);

    protected bool PreProcessingCheck(IDataLoadEventListener listener)
    {
        //Context of pipeline execution is the extraction of a command that does not relate to a dataset (e.g. a command to extract custom data).  We don't care about those commands so just make this component transparent
        if (Request == null)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Pipeline execution is of a non dataset command "));
            return false;
        }


        var datasetName = Request.DatasetBundle.DataSet.ToString();
        if (DatasetName.IsMatch(datasetName)) return true;

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Ignoring batch because it does not come from an image bundle (DatasetName). Table name was: {datasetName}, didn't match regex: {DatasetName}"));
        return false;

    }

    public void PreInitialize(IExtractCommand request, IDataLoadEventListener listener)
    {
        Request = request as IExtractDatasetCommand;

        // We only care about dataset extraction requests
        if (Request == null)
            return;

        if (Request.Directory is null)
            throw new InvalidOperationException("The Extraction Directory must be set.");

        if (Request.Catalogue is null)
            throw new InvalidOperationException("The request must have a Catalogue reference.");

        if (Request.ColumnsToExtract is null)
            throw new InvalidOperationException("The request must contain a list of ColumnsToExtract (even if empty)");

        if (Request.Catalogue.CatalogueRepository.GetAllObjectsWhere<ILoadMetadataCatalogueLinkage>("CatalogueID",Request.Catalogue.ID).Length == 0)
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                "The request has no associated metadata file. You may need to add a Data Load Configuration if you intend to extract the image files.")); //May be able to get rid of this warning entirely
    }

    public abstract void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny);
    public abstract void Abort(IDataLoadEventListener listener);
    public abstract void Check(ICheckNotifier notifier);
}