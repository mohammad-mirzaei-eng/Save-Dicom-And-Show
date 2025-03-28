using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Show_Dicom
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "DICOM files (*.dcm)|*.dcm|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    DisplayDicomImage(filePath);
                }
            }
        }

        private void DisplayDicomImage(string filePath)
        {
            try
            {
                DicomImage dicomImage = new DicomImage(filePath);
                var image = dicomImage.RenderImage();

                if (image != null)
                {
                    // Create bitmap without using statement
                    var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var pixel = image.GetPixel(x, y);
                            bitmap.SetPixel(x, y, Color.FromArgb(pixel.R, pixel.G, pixel.B));
                        }
                    }

                    if (pictureBox1.Image != null)
                    {
                        pictureBox1.Image?.Dispose(); // Dispose existing image
                    }
                    pictureBox1.Image = bitmap;
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                }
                else
                {
                    MessageBox.Show("Failed to render DICOM image.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading DICOM image: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    ConvertImageToDicom(filePath);
                }
            }
        }

        private void ConvertImageToDicom(string imagePath)
        {
            using (Bitmap bitmap = new Bitmap(imagePath))
            {
                int width = bitmap.Width;
                int height = bitmap.Height;
                int bytesPerPixel = 3; // RGB 
                byte[] pixelDataArray = new byte[width * height * bytesPerPixel];

                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);

                Marshal.Copy(bmpData.Scan0, pixelDataArray, 0, pixelDataArray.Length);
                bitmap.UnlockBits(bmpData);

                for (int i = 0; i < pixelDataArray.Length; i += 3)
                {
                    byte temp = pixelDataArray[i];       // Blue
                    pixelDataArray[i] = pixelDataArray[i + 2];   // Red
                    pixelDataArray[i + 2] = temp;        // Blue to Red 
                }

                DicomDataset dataset = new DicomDataset
        {
            { DicomTag.PatientName, "Test^Patient" },
            { DicomTag.PatientID, "123456" },
            { DicomTag.BitsAllocated, (ushort)8 },
            { DicomTag.BitsStored, (ushort)8 },
            { DicomTag.HighBit, (ushort)7 },
            { DicomTag.SamplesPerPixel, (ushort)3 },
            { DicomTag.Rows, (ushort)height },
            { DicomTag.Columns, (ushort)width },
            { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage },
            { DicomTag.SOPInstanceUID, DicomUID.Generate() },
            { DicomTag.PixelRepresentation, (ushort)0 },
            { DicomTag.PlanarConfiguration, (ushort)0 }
        };

                // **مهم: اطمینان از RGB بودن داده‌ها**
                dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);

                DicomPixelData pixelData = DicomPixelData.Create(dataset, true);
                pixelData.PlanarConfiguration = PlanarConfiguration.Interleaved;
                pixelData.AddFrame(new MemoryByteBuffer(pixelDataArray));

                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "DICOM files (*.dcm)|*.dcm|All files (*.*)|*.*";
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string dicomFilePath = saveFileDialog.FileName;
                        DicomFile dicomFile = new DicomFile(dataset);
                        dicomFile.Save(dicomFilePath);
                    }
                }
            }
        }
    }
}
