using System;
using System.Windows.Forms;
using Xsd2Code.Library;

namespace Xsd2Code.Addin
{
    public partial class FormOption : Form
    {
        #region Property : GeneratorParams

        private GeneratorParams generatorParams;

        public GeneratorParams GeneratorParams
        {
            get { return this.generatorParams; }
            set { this.generatorParams = value; }
        }

        #endregion

        #region Property

        public string InputFile { get; set; }

        public string OutputFile { get; set; }


        #endregion

        #region cTor

        /// <summary>
        /// Constructor
        /// </summary>
        public FormOption()
        {
            this.InitializeComponent();
        }

        #endregion

        #region Method

        /// <summary>
        /// Analyse file to find generation option.
        /// </summary>
        /// <param name="xsdFilePath">The XSD file path.</param>
        public void Init(string xsdFilePath)
        {
            string outputFile;
            var paramFieldFromFile = GeneratorParams.LoadFromFile(xsdFilePath, out outputFile);
            this.generatorParams = paramFieldFromFile ?? new GeneratorParams();

            this.propertyGrid.SelectedObject = this.generatorParams;
            this.OutputFile = outputFile;
        }

        /// <summary>
        /// Cancel the validation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Validate the generation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            var result = this.generatorParams.Validate();
            if(!result.Success)
            {
                MessageBox.Show(result.Messages.ToString());
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        #endregion

        /// <summary>
        /// Close form if press esc.
        /// </summary>
        /// <param name="sender">Object sender</param>
        /// <param name="e">EventArgs param</param>
        private void FormOption_KeyPress(object sender, KeyPressEventArgs e)
        {            
            int ascii = Convert.ToInt16(e.KeyChar);
            if (ascii == 27) this.Close();
        }


   }
}