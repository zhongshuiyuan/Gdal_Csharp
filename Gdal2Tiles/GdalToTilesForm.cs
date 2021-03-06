﻿using System;
using System.Windows.Forms;
using GDAL.Config;
using MngImg;
using GeoAPI.Geometries;
using System.Drawing;

namespace Gdal2Tiles
{
    public partial class GdalToTilesForm : Form
    {
        private GdalImage _Img;

        public GdalToTilesForm()
        {
            InitializeComponent();
            _Img = null;

            GdalConfiguration.ConfigureAll();

            SetItemCmbSD();
            SetItemCmbSizeTile();
        }

        private void ShowDescriptImg()
        {
            treeViewDescript.Nodes.Clear();

            //Array -> north[0], south[1], west[2], east[3];
            var box = _Img.Extent;

            int idNode = 0;
            treeViewDescript.Nodes.Add("Source");
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("Path {0}", System.IO.Path.GetDirectoryName(_Img.FullName)));
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("Name {0}", _Img.FileName));
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("Format {0}", _Img.Format));

            idNode++;
            treeViewDescript.Nodes.Add("Raster");
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("Size X/Y {0}/{1}", _Img.Width, _Img.Height));
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("ResolutionX/Y {0:0.0000}/{1:0.0000}", _Img.XResolution, _Img.YResolution));
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("Number Bands {0}", _Img.BandsNumber));
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("Type {0}", _Img.Type));

            idNode++;
            treeViewDescript.Nodes.Add("Extent");
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("North {0:0.0000}", box.Top()));
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("West {0:0.0000}", box.Left()));
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("South {0:0.0000}", box.Bottom()));
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("East {0:0.0000}", box.Right()));

            idNode++;
            treeViewDescript.Nodes.Add("Spatial Reference");
            treeViewDescript.Nodes[idNode].Nodes.Add(string.Format("{0} Geodesic WGS84(EPSG:4326)", _Img.IsSameCS("EPSG:4326") ? "It is" : "It is not"));
            treeViewDescript.Nodes[idNode].Nodes.Add(_Img.Projection);
        }

        private void SetItemCmbOrder()
        {
            // Clear
            cmbBxBand1.Items.Clear();
            cmbBxBand2.Items.Clear();
            cmbBxBand3.Items.Clear();

            int nBand = _Img.BandsNumber;
            for (int i = 0; i < nBand; i++)
            {
                cmbBxBand1.Items.Add(i + 1);
                cmbBxBand2.Items.Add(i + 1);
                cmbBxBand3.Items.Add(i + 1);
            }
            cmbBxBand1.SelectedIndex = cmbBxBand2.SelectedIndex = cmbBxBand3.SelectedIndex = 0;

            if (nBand > 1) cmbBxBand2.SelectedIndex = cmbBxBand3.SelectedIndex = 1;
            if (nBand > 2) cmbBxBand3.SelectedIndex = 2;
        }

        private void SetItemCmbSD()
        {
            for (int i = 1; i < 5; i++)
                cmBxSD.Items.Add(i);

            cmBxSD.SelectedIndex = 1; // SD = 2
        }

        private void SetItemCmbSizeTile()
        {
            // 2^8 = 256
            for (int i = 8; i < 11; i++)
                cmBxSize.Items.Add(System.Convert.ToInt32(Math.Pow(2, i)));

            cmBxSize.SelectedIndex = 1; // 512
        }

        private string MakePathTiles(string sSelectedPath)
        {
            string sPathTiles = sSelectedPath + "\\" + _Img.FileName + "_tiles";

            if (System.IO.Directory.Exists(sPathTiles))
                foreach (string file in System.IO.Directory.GetFiles(sPathTiles))
                    System.IO.File.Delete(file);
            else System.IO.Directory.CreateDirectory(sPathTiles);

            return sPathTiles;
        }

        private void RunWriteTiles(string sSelectedPath)
        {
            txtBxStatus.Text = "";

            if (!_Img.IsSameCS("EPSG:4326")) _Img.Warp("EPSG:4326");

            int sizeTile = (int)cmBxSize.SelectedItem;

            string sPathTiles = MakePathTiles(sSelectedPath);
            ImageWriteTilesGdal imgWrite = new ImageWriteTilesGdal(_Img.Dataset, sizeTile, sPathTiles, new StatusText(FuncStatusText), new StatusProgressBar(FuncStatusProgressBar));

            imgWrite.SetOptionNullData(0);
            imgWrite.SetOptionMakeAlphaBand((byte)0);

            if (ckOrder.Checked)
            {
                int[] Order = new int[3];
                Order[0] = (int)cmbBxBand1.SelectedItem;
                Order[1] = (int)cmbBxBand2.SelectedItem;
                Order[2] = (int)cmbBxBand3.SelectedItem;
                imgWrite.SetOptionOrder(Order);
            }

            if (ckStrech.Checked)
                imgWrite.SetOptionStretchStardDesv((int)cmBxSD.SelectedItem);

            FuncStatusText("Saving tiles");
            imgWrite.Write();

            KMLWriteTilesGdal kmlWrite = new KMLWriteTilesGdal(_Img.Dataset, sizeTile, sPathTiles, _Img.FileName);
            kmlWrite.Write();

            FuncStatusText(string.Format(
               "\r\nSuccess writed tiles!\r\nPath tiles: {0}\r\nSource KML: {1}.kml", sPathTiles, _Img.FileName));

            progressBar1.Value = 0;
        }

        public void FuncStatusText(string msgStatus)
        {
            txtBxStatus.AppendText("\r\n" + msgStatus);
            txtBxStatus.Refresh();
        }

        public void FuncStatusProgressBar(int Min, int Max, int Step, int id)
        {
            if (Max > 0)
            {
                progressBar1.Maximum = Max;
                progressBar1.Minimum = Min;
                progressBar1.Value = 0;

                txtBxStatus.Refresh();
                progressBar1.Refresh();

                Application.DoEvents();
            }
            if (Step > 0) progressBar1.Step = Step;
            if ((id + 1) > 0)
            {
                if ((id + 1) > progressBar1.Maximum) id = 0;
                progressBar1.Value = id + 1;
            }
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Geo Image Files(*.IMG;*.TIF)|*.IMG;*.TIF";
            dialog.Title = "Select a image files";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (GdalImage.IsValidImage(dialog.FileName))
                {
                    _Img = new GdalImage(dialog.FileName);

                    grpBxOptions.Enabled = true;
                    btnSelectPath.Enabled = true;

                    FuncStatusText(string.Format("Getting description\r\n{0}...", dialog.FileName));

                    // pctBoxImg.Image = _Img.GetBitmap(pctBoxImg.Size, null, 0);
                    _Img.Warp("EPSG:4326");
                    //pctBoxImg.Image = _Img.GetNonRotatedPreview(new Size(_Img.Width / 20, _Img.Height / 20), _Img.Extent);
                    ShowDescriptImg();
                    SetItemCmbOrder();

                    txtBxStatus.Text = "";
                }
                else
                {
                    txtBxStatus.Text = string.Format("Invalid Format image\r\n{0}", dialog.FileName);
                }
            }
        }

        private void btnSelectPath_Click(object sender, EventArgs e)
        {
            // RunWriteTiles(@"C:\_trabalhos\BD_test\tileskml");

            FolderBrowserDialog fdlBrw = new FolderBrowserDialog();
            fdlBrw.Description = "Select Path for Tiles(KML & images)";
            fdlBrw.ShowNewFolderButton = true;

            if (fdlBrw.ShowDialog() == DialogResult.OK)
                RunWriteTiles(fdlBrw.SelectedPath);
        }

        private void ckStrech_CheckedChanged(object sender, EventArgs e)
        {
            cmBxSD.Enabled = ckStrech.Checked;
        }

        private void ckOrder_CheckedChanged(object sender, EventArgs e)
        {
            cmbBxBand1.Enabled = cmbBxBand2.Enabled = cmbBxBand3.Enabled = ckOrder.Checked;
        }

        private void btPreview_Click(object sender, EventArgs e)
        {
            int[] Order = null;
            int SD = 0;

            if (ckOrder.Checked)
            {
                Order = new int[3];
                Order[0] = (int)cmbBxBand1.SelectedItem;
                Order[1] = (int)cmbBxBand2.SelectedItem;
                Order[2] = (int)cmbBxBand3.SelectedItem;
            }

            if (ckStrech.Checked)
                SD = (int)cmBxSD.SelectedItem;

            pctBoxImg.Image = _Img.GetBitmap(pctBoxImg.Size, Order, SD);
        }

        private void treeViewDescript_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                Clipboard.SetDataObject(treeViewDescript.SelectedNode.Text, true);
            }
            catch (Exception)
            {
            }
        }
    }
}
