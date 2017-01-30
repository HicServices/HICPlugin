using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using DataLoadEngine.Mutilators;
using Microsoft.SqlServer.Management.Smo;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;

namespace HICPlugin.Mutilators
{
    public class CHIMutilator:IPluginMutilateDataTables
    {
        private DiscoveredDatabase _dbInfo;
        private LoadStage _loadStage;

        [DemandsInitialization("The CHI column you want to mutilate based on")]
        public ColumnInfo ChiColumn { get; set; }


        [DemandsInitialization("If true, program will attempt to add zero to the front of 9 digit CHIs prior to running the CHI validity check",DemandType.Unspecified,true)]
        public bool TryAddingZeroToFront { get; set; }

        [DemandsInitialization("Columns failing validation will have this consequence applied to them", DemandType.Unspecified, true)]
        public MutilationAction FailedRows { get; set; }
        

        public void Check(ICheckNotifier notifier)
        {
            
        }

        public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
        {
            
        }

        public bool DisposeImmediately { get; set; }

        public void Initialize(DiscoveredDatabase dbInfo, LoadStage loadStage)
        {
            _dbInfo = dbInfo;
            _loadStage = loadStage;
        }

        private string DropCHIFunctionIfExists()
        {
            return @"IF OBJECT_ID('dbo.checkCHI') IS NOT NULL
  DROP FUNCTION checkCHI";
        }


        private string CreateCHIFunction()
        {
            return @"
CREATE FUNCTION [dbo].[checkCHI](@CHI as varchar(255))
RETURNS bit AS
BEGIN

    DECLARE @SumTotal int
    DECLARE @CheckDigit int
    DECLARE @Result bit
    DECLARE @i int
    SET @i = 0
    SET @SumTotal = 0
    SET @Result = 0
    --return 0 if the CHI is non-numeric
    IF(ISNUMERIC(@CHI) <> 1)
        RETURN 0
        
    --return 0 if the day of birth is greater than 31
    IF(LEFT(@CHI, 2) > 31)
        RETURN 0    
    
    --return 0 if the month of birth is greater than 12
    IF(SUBSTRING(@CHI, 3, 2) > 12)
        RETURN 0    
        
    --return 0 if the CHI is not 10 digits long
    IF(LEN(@CHI) = 10)
    BEGIN
        --Calculate the sum
        WHILE @i < 9
        BEGIN
            SET @SumTotal = @SumTotal + (convert(int,substring(@CHI,@i+1,1)) * (10 - @i))
            SET @i = @i + 1
        END

        --Obtain Check Digit
        SET @CheckDigit = 11 - (@SumTotal % 11)

        IF @CheckDigit = 11
            SET @CheckDigit = 0

        --Compare Check Digit
        IF @CheckDigit = convert(int,substring(@CHI,10,1))
            SET @Result = 1

        RETURN @Result
    END
    
    RETURN 0
END
";
        }

        private string GetUpdateSQL(LoadStage loadStage)
        {
            var tableName = ChiColumn.TableInfo.GetRuntimeName(loadStage);
            var colName = ChiColumn.GetRuntimeName(loadStage);

            switch (FailedRows)
                {
                    case MutilationAction.SetNull:
                        return "UPDATE " + tableName + " SET " + colName + " = NULL WHERE dbo.checkCHI(" + colName + ") = 0";
                    case MutilationAction.DeleteRows:
                        return "DELETE FROM " + tableName + " WHERE dbo.checkCHI(" + colName + ") = 0";
                    case MutilationAction.CrashDataLoad:
                        return "IF EXISTS (SELECT 1 FROM " + tableName + " WHERE dbo.checkCHI(" + colName + ") = 0) raiserror('Found Dodgy CHIs', 16, 1);";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }

        public ProcessExitCode Mutilate(IDataLoadEventListener job)
        { 
            
            if (_loadStage == LoadStage.AdjustRaw || _loadStage == LoadStage.AdjustStaging)
            {
                using (var con = _dbInfo.Server.GetConnection())
                {
                    con.Open();

                    _dbInfo.Server.GetCommand(DropCHIFunctionIfExists(), con).ExecuteNonQuery();
                    _dbInfo.Server.GetCommand(CreateCHIFunction(), con).ExecuteNonQuery();

                    if(TryAddingZeroToFront)
                        _dbInfo.Server.GetCommand(PrePendNineDigitCHIs(_loadStage), con).ExecuteNonQuery();

                    int affectedRows = _dbInfo.Server.GetCommand(GetUpdateSQL(_loadStage), con).ExecuteNonQuery();

                    job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, "CHIMutilator affected " + affectedRows + " rows"));
                }
            }
            else
                throw new NotSupportedException("This mutilator can only run in AdjustRaw or AdjustStaging");

            return ProcessExitCode.Success;
        }

        
        private string PrePendNineDigitCHIs(LoadStage loadStage)
        {
            var tableName = ChiColumn.TableInfo.GetRuntimeName(loadStage);
            var colName = ChiColumn.GetRuntimeName(loadStage);

            return "UPDATE " + tableName + " SET " + colName + " ='0' + "+colName+" WHERE LEN(" + colName + ") = 9 ";
        }
    }

    public enum MutilationAction
    {
        SetNull,
        DeleteRows,
        CrashDataLoad
    }
}
