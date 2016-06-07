using System.ComponentModel.Composition;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using DataLoadEngine.Mutilators;
using ReusableLibraryCode;

namespace LoadModules.Specific.HIC
{
    public class CHIPopulatorRAW : CHIPopulator
    {
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn Surname { get; set; }
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn Forename { get; set; }
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn DateOfBirth { get; set; }
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn Sex { get; set; }

        [DemandsInitialization("")]
        public PreLoadDiscardedColumn Postcode { get; set; }
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn AddressLine1 { get; set; }
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn AddressLine2 { get; set; }
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn AddressLine3 { get; set; }
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn AddressLine4 { get; set; }


        [DemandsInitialization("")]
        public PreLoadDiscardedColumn OtherPostcode { get; set; }
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn OtherAddressLine1 { get; set; }
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn OtherAddressLine2 { get; set; }
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn OtherAddressLine3 { get; set; }
        [DemandsInitialization("")]
        public PreLoadDiscardedColumn OtherAddressLine4 { get; set; }

        protected override IHasRuntimeName GetSurname { get { return Surname; } }
        protected override IHasRuntimeName GetForename { get { return Forename; } }
        protected override IHasRuntimeName GetDateOfBirth { get { return DateOfBirth; } }
        protected override IHasRuntimeName GetPostcode { get { return Postcode; } }
        protected override IHasRuntimeName GetSex { get { return Sex; } }
        protected override IHasRuntimeName GetAddressLine1 { get { return AddressLine1; } }
        protected override IHasRuntimeName GetAddressLine2 { get { return AddressLine2; } }
        protected override IHasRuntimeName GetAddressLine3 { get { return AddressLine3; } }
        protected override IHasRuntimeName GetAddressLine4 { get { return AddressLine4; } }

        protected override IHasRuntimeName GetOtherAddressLine1 { get { return OtherAddressLine1; } }
        protected override IHasRuntimeName GetOtherAddressLine2 { get { return OtherAddressLine2; } }
        protected override IHasRuntimeName GetOtherAddressLine3 { get { return OtherAddressLine3; } }
        protected override IHasRuntimeName GetOtherAddressLine4 { get { return OtherAddressLine4; } }
        protected override IHasRuntimeName GetOtherPostcode { get { return OtherPostcode; } }
    }
}