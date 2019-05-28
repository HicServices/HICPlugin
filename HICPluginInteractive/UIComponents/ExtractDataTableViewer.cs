using System.Data;

namespace HICPluginInteractive.UIComponents
{
    /// <summary>
    /// Allows you to view DataTable data, with option to specify current cell.
    /// </summary>
    public partial class ExtractDataTableViewer : UserControl
    {
        private readonly string _colName;
        private readonly int? _rowIndex;

        public ExtractDataTableViewer(DataTable source, string caption, string colName, int? rowIndex)
        {
            _colName = colName;
            _rowIndex = rowIndex;
            InitializeComponent();
            
            this.Text = caption;
            dataGridView1.DataSource = source;
        }

        private void ExtractDataTableViewer_Load(object sender, System.EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_colName) && _rowIndex.HasValue)
                dataGridView1.CurrentCell = dataGridView1[_colName, _rowIndex.Value];
        }
    }
}
