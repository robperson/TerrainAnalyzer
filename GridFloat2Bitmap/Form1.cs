using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GridFloat2Bitmap
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        List<MapGroup> allGroups = new List<MapGroup>();
        List<MapGroup> elevatedGroups = new List<MapGroup>();
        float[] heights;
        int[] histHeights;
        float min = float.MaxValue;
        float max = float.MinValue;
        float scalefactor;
        int width = 0;
        int height = 0;
        float scale = 1f;
        int groupfactor = 0;
        int minArea = 20; // meters^2
        int numpoints = 0;
        float mean = float.MinValue;
        float stdDev = float.MinValue;
        float cellsize = 1;
        float maxArea = 2500; //meters^2

        bool dragging = false;
        Point dragStart;
        Point dragCurr;

        private int CompareGroups(MapGroup m1, MapGroup m2)
        {
            if (m1.zval < m2.zval)
            {
                return -1;
            }
            else if (m1.zval == m2.zval)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        private int CompareGroupsArea(MapGroup m1, MapGroup m2)
        {
            if (m1.Area < m2.Area)
            {
                return 1;
            }
            else if (m1.Area == m2.Area)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }

        private int ReverseCompareGroups(MapGroup m1, MapGroup m2)
        {
            if (m1.zval < m2.zval)
            {
                return 1;
            }
            else if (m1.zval == m2.zval)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }

        private int GetAvgHeight(int oldAvg, int numPoints, float newHeight)
        {
            return (int)(((float)(oldAvg * numPoints) + newHeight) / (float)(numPoints + 1));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.Cancel)
                return;
            string filename = ofd.FileName;
            
            BinaryReader binreader = new BinaryReader(File.OpenRead(filename));
            //binreader.BaseStream.Seek(264, SeekOrigin.Begin);
            //byte tiletype = binreader.ReadByte();
            
            string hdrfile = ofd.FileName.Substring(0, ofd.FileName.Length - 4) + ".hdr";

            
            
            try //Open hdr file to get width and height
            {
                FileStream hdrstream = File.OpenRead(hdrfile);
                StreamReader sr = new StreamReader(hdrstream);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("ncols"))
                    {
                        width = int.Parse(line.Split(new char[] {' ','\t'},StringSplitOptions.RemoveEmptyEntries)[1]);
                    }
                    if (line.Contains("nrows"))
                    {
                        height = int.Parse(line.Split(new char[] {' ','\t'},StringSplitOptions.RemoveEmptyEntries)[1]);
                    }  
                    if (line.Contains("cellsize"))
                    {
                        cellsize = float.Parse(line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[1]);
                    } 
                }
                if (width == 0 || height == 0)
                {
                    throw new Exception();
                }

                if (cellsize < 1) cellsize = 9;
            }
            catch (Exception)
            {

                //throw;
                MessageBox.Show("An error occurred while loading the map data.");
                toolStripStatusLabel1.Text = "Please restart the application or load a different map";
                return;
                //width = 4749;
                //height = 6624;
            }
            txtHeight.Text = height.ToString();
            txtWidth.Text = width.ToString();
            heights = new float[width * height];
            histHeights = new int[width * height];
            




            pictureBox1.Width = width;
            pictureBox1.Height = height;
            pictureBox2.Width = width;
            pictureBox2.Height = height;
            pictureBox3.Width = width;
            pictureBox3.Height = height;
            numpoints = width * height;
            
            scale = 1f;
            //int key = Int16.MinValue;
            int deviations;
            float currHeigh;
            Dictionary<int, MedianList> histogram = new Dictionary<int, MedianList>();
            try
            {
                //Read in all height values
                for (int point = 0; point < heights.Length; point++)
                {
                    currHeigh = binreader.ReadSingle();
                    heights[point] = currHeigh;
                   
                    //if (mean == float.MinValue)
                    //{
                    //    mean = heights[point];
                    //}
                    //else
                    //{
                    //    mean = ((mean * point) + heights[point]) / ((float)point + 1.0f);
                    //}



                    //if (heights[point] < min) min = heights[point];
                    //else if (heights[point] > max) max = heights[point];

                    
                }
                
            }
            catch
            { }
            

            
           
            

            this.rect = new Rectangle(0, 0, width, height);
            AnalyzeMap(0,0,width,height);
            DrawMaps();

            txtHigh.Text = max.ToString();
            txtLow.Text = min.ToString();
        }

        private void AnalyzeMap(int xmin, int ymin, int xmax, int ymax)
        {
            DateTime start = DateTime.Now;
            int deviations;
            Dictionary<int, MedianList> histogram = new Dictionary<int, MedianList>();
            float elevation = 0;
            int index = 0;
            //calculate mean
            for (int y = ymin; y < ymax; y++)
            {
                for (int x = xmin; x < xmax; x++)
                {
                    index = (y * width) + x;
                    elevation = heights[index];

                    if (mean == float.MinValue)
                    {
                        mean = elevation;
                    }
                    else
                    {
                        mean = ((mean * index) + elevation) / ((float)index + 1.0f);
                    }
                    
                    if (elevation < min) 
                        min = elevation;
                    else if (elevation > max) 
                        max = elevation;
                }

            }
            
            //Calculate StdDev
            float avgDist = float.MinValue;
            float temp;
            for (int y = ymin; y < ymax; y++)
            {
                for (int x = xmin; x < xmax; x++)
                {
                    index = (y * width) + x;
                    elevation = heights[index];

                    if (avgDist == float.MinValue)
                    {
                        avgDist = (elevation - mean) * (elevation - mean);
                    }
                    else
                    {
                        temp = (elevation - mean) * (elevation - mean);
                        avgDist = ((avgDist * index) + temp) / ((float)index + 1.0f);
                    }
                }
            }
            stdDev = (float)Math.Sqrt(avgDist);

            //Bucketize height values
            for (int y = ymin; y < ymax; y++)
            {
                for (int x = xmin; x < xmax; x++)
                {
                    index = (y * width) + x;
                    elevation = heights[index];
                    deviations = (int)((elevation - min) / stdDev);
                    if (histogram.Keys.Contains(deviations))
                    {
                        histogram[deviations].Add(index);
                    }
                    else
                    {
                        MedianList indexes = new MedianList();
                        indexes.Add(index);
                        histogram.Add(deviations, indexes);
                    }
                }

            }
            //Populate histogram heights list
            foreach (KeyValuePair<int, MedianList> kvp in histogram)
            {
                foreach (int idx in kvp.Value)
                {
                    histHeights[idx] = (int)(kvp.Key * stdDev);
                }
            }

            lblMoreinfo.Text = string.Format("Mean: {0}, StdDev: {1}", mean, stdDev);
           
            //Perform Analysis

            List<MapLine>[] allLines = new List<MapLine>[height];
            
            bool shade = true;
            if (shade)
            {
                
                int lastChange = -1; //used to store the xval of the last change in height
                float currheight = float.MinValue;
                float lastHeight = float.MinValue;
                allGroups.Clear();
                for (int y = ymin; y < ymax; y++)
                {
                    lastChange = -1;
                    currheight = float.MinValue;
                    lastHeight = float.MinValue;
                    allLines[y] = new List<MapLine>();
                    for (int x = xmin; x < xmax; x++)
                    {

                        currheight = GetNormalizedHeight(x, y);
                        //first line segment
                        if (lastChange == -1)
                        {
                            lastHeight = currheight;
                            lastChange = xmin;
                            continue;
                        }
                        else if (currheight != lastHeight) //End of line segment
                        {
                            MapLine newline = new MapLine(lastChange, x - 1, lastHeight, y);
                            allLines[y].Add(newline);
                            //determine if there is line before this one on x axis and check elevation
                            if (allLines[y].Count > 1)
                            {
                                int newLineIdx = allLines[y].IndexOf(newline);
                                if (allLines[y][newLineIdx - 1].zval < newline.zval)
                                {
                                    allLines[y][newLineIdx - 1].elevated = false;
                                }
                            }

                            if (y == ymin)//No lines above to group with, so make new group
                            {
                                
                                MapGroup newgroup = new MapGroup(newline);
                                lastHeight = currheight;
                                lastChange = x;
                                newline.group = newgroup;
                                allGroups.Add(newgroup);
                                newgroup.elevated = newline.elevated;
                                
                                continue;
                            }
                            else //find a group to add to
                            {
                                MapGroup firstGroup = null;
                                foreach (MapLine mline in allLines[y - 1])
                                {
                                    //Check to see if the two lines overlap and have same elevation
                                    if (((mline.xmax >= newline.xmax && mline.xmin <= newline.xmax) ||
                                        (mline.xmin <= newline.xmin && mline.xmax >= newline.xmin) ||
                                        (mline.xmax <= newline.xmax && mline.xmin >= newline.xmin)) &&
                                        (mline.zval == newline.zval))
                                    {
                                        if (firstGroup == null) //this is the first group that this line overlaps
                                        {
                                            firstGroup = mline.group;
                                        }
                                        else //combine this group with the other groups this line overlaps
                                        {
                                            MapGroup tmpGroup = mline.group;
                                            if (tmpGroup != firstGroup)
                                            {
                                                foreach (MapLine gline in tmpGroup.lines)
                                                {
                                                    gline.group = firstGroup;
                                                    //Update bounds for group
                                                    if (gline.xmax > firstGroup.xmax)
                                                    {
                                                        firstGroup.xmax = gline.xmax;
                                                    }
                                                    if (gline.yval < firstGroup.ymin)
                                                    {
                                                        firstGroup.ymin = gline.yval;
                                                    }
                                                    if (gline.xmin < firstGroup.xmin)
                                                    {
                                                        firstGroup.xmin = gline.xmin;
                                                    }
                                                }
                                                allGroups.Remove(tmpGroup);
                                                firstGroup.lines.AddRange(tmpGroup.lines);
                                                firstGroup.elevated = firstGroup.elevated && tmpGroup.elevated;
                                                tmpGroup.lines.Clear();
                                            }
                                        }
                                    }
                                    //check to see if overlapping line is higher in elevation
                                    else if (((mline.xmax >= newline.xmax && mline.xmin <= newline.xmax) ||
                                        (mline.xmin <= newline.xmin && mline.xmax >= newline.xmin) ||
                                        (mline.xmax <= newline.xmax && mline.xmin >= newline.xmin)) &&
                                        (mline.zval > newline.zval))
                                    {
                                        newline.elevated = false;
                                    }
                                    //check to see if overlapping line is lower in elevation
                                    else if (((mline.xmax >= newline.xmax && mline.xmin <= newline.xmax) ||
                                        (mline.xmin <= newline.xmin && mline.xmax >= newline.xmin) ||
                                        (mline.xmax <= newline.xmax && mline.xmin >= newline.xmin)) &&
                                        (mline.zval < newline.zval))
                                    {
                                        mline.elevated = false;
                                    }
                                    
                                }

                                if (firstGroup == null)//Never found an overlapping group, so make new one
                                {
                                    MapGroup newgroup = new MapGroup(newline);
                                    lastHeight = currheight;
                                    lastChange = x;
                                    newline.group = newgroup;
                                    newgroup.elevated = newline.elevated;
                                    allGroups.Add(newgroup);
                                }
                                else
                                {
                                    lastHeight = currheight;
                                    lastChange = x;
                                    newline.group = firstGroup;
                                    firstGroup.ymax = y;
                                    firstGroup.lines.Add(newline);
                                    //Update bounds for group
                                    if (newline.xmax > firstGroup.xmax)
                                    {
                                        firstGroup.xmax = newline.xmax;
                                    }
                                    if (newline.xmin < firstGroup.xmin)
                                    {
                                        firstGroup.xmin = newline.xmin;
                                    }
                                    firstGroup.elevated = firstGroup.elevated && newline.elevated;
                                }
                            }//End overlap test
                        }
                    }//end x loop
                    int yval = (y == ymax) ? y - 1 : y;
                    //Close up final line segment
                    MapLine lastline = new MapLine(lastChange, xmax - 1, lastHeight, yval);
                    allLines[yval].Add(lastline);
                    if (allLines[y].Count > 1)
                    {
                        int newLineIdx = allLines[y].IndexOf(lastline);
                        if (allLines[y][newLineIdx - 1].zval < lastline.zval)
                        {
                            allLines[y][newLineIdx - 1].elevated = false;
                        }
                    }
                    if (y == ymin)//No lines above to group with, so make new group
                    {
                        MapGroup newgroup = new MapGroup(lastline);
                        lastline.group = newgroup;
                        newgroup.elevated = lastline.elevated;
                        allGroups.Add(newgroup);

                    }
                    else
                    {
                        MapGroup firstGroup = null;
                        foreach (MapLine mline in allLines[yval - 1])
                        {
                            //Check to see if the two lines overlap
                            if (((mline.xmax >= lastline.xmax && mline.xmin <= lastline.xmax) ||
                                (mline.xmin <= lastline.xmin && mline.xmax >= lastline.xmin) ||
                                (mline.xmax <= lastline.xmax && mline.xmin >= lastline.xmin)) &&
                                (mline.zval == lastline.zval))
                            {
                                if (firstGroup == null)
                                {
                                    firstGroup = mline.group;
                                }
                                else
                                {
                                    MapGroup tmpGroup = mline.group;
                                    if (tmpGroup != firstGroup)
                                    {
                                        foreach (MapLine gline in tmpGroup.lines)
                                        {
                                            gline.group = firstGroup;
                                            //Update bounds for group
                                            if (gline.xmax > firstGroup.xmax)
                                            {
                                                firstGroup.xmax = gline.xmax;
                                            }
                                            if (gline.yval < firstGroup.ymin)
                                            {
                                                firstGroup.ymin = gline.yval;
                                            }
                                            if (gline.xmin < firstGroup.xmin)
                                            {
                                                firstGroup.xmin = gline.xmin;
                                            }
                                        }
                                        allGroups.Remove(tmpGroup);
                                        firstGroup.lines.AddRange(tmpGroup.lines);
                                        firstGroup.elevated = firstGroup.elevated && tmpGroup.elevated;
                                        tmpGroup.lines.Clear();
                                    }
                                }
                            }
                            //check to see if overlapping line is higher in elevation
                            else if (((mline.xmax >= lastline.xmax && mline.xmin <= lastline.xmax) ||
                                        (mline.xmin <= lastline.xmin && mline.xmax >= lastline.xmin) ||
                                        (mline.xmax <= lastline.xmax && mline.xmin >= lastline.xmin)) &&
                                        (mline.zval > lastline.zval))
                            {
                                lastline.elevated = false;
                            }
                            //check to see if overlapping line is lower in elevation
                            else if (((mline.xmax >= lastline.xmax && mline.xmin <= lastline.xmax) ||
                                (mline.xmin <= lastline.xmin && mline.xmax >= lastline.xmin) ||
                                (mline.xmax <= lastline.xmax && mline.xmin >= lastline.xmin)) &&
                                (mline.zval < lastline.zval))
                            {
                                mline.elevated = false;
                            }
                        }

                        if (firstGroup == null)//Never found an overlapping group, so make new one
                        {
                            MapGroup newgroup = new MapGroup(lastline);
                            lastline.group = newgroup;
                            newgroup.elevated = lastline.elevated;
                            allGroups.Add(newgroup);
                        }
                        else
                        {
                            firstGroup.lines.Add(lastline);
                            lastline.group = firstGroup;
                            firstGroup.ymax = yval;
                            //Update bounds for group
                            if (lastline.xmax > firstGroup.xmax)
                            {
                                firstGroup.xmax = lastline.xmax;
                            }
                            if (lastline.xmin < firstGroup.xmin)
                            {
                                firstGroup.xmin = lastline.xmin;
                            }
                            firstGroup.elevated = firstGroup.elevated && lastline.elevated;
                        }
                    }//End overlap test
                    //if (y > 65)
                    //    break;
                }//end y loop

                //Redeem groups with mostly elevated lines
                //foreach (MapGroup mgroup in allGroups)
                //{
                //    float elevatedarea = (from line in mgroup.lines where line.elevated select line).Sum(line => (float)(line.xmax - line.xmin));
                //    if ((elevatedarea / mgroup.Area) > 0.5)
                //    {
                //        mgroup.elevated = true;
                //    }
                //}

                //All lines and groups are made
                //Now determine which ones are elevated
                //DoElevationCheck();
                allGroups.Sort(CompareGroupsArea);
                FilterGroups();

            }
            DateTime stop = DateTime.Now;
            double secs = stop.Subtract(start).TotalSeconds;
            toolStripStatusLabel1.Text = "Analysis Completed in " + secs + " seconds!";

            
            
        }

        private void FilterGroups() // sets elevated groups list based on area bounds
        {
            elevatedGroups = (from grp in allGroups
                              where grp.elevated
                                  && grp.Area * cellsize >= minArea
                                  && grp.Area * cellsize <= maxArea
                              select grp).ToList();

            //Populate Groups ListBox
            listBox1.DataSource = null;
            listBox1.DataSource = elevatedGroups;
            listBox1.DisplayMember = "Info";
            listBox1.Refresh();
        }

        private void ShadeGroup(int group)
        {
            ShadeGroup(group, chkBounds.Checked, chkClear.Checked);
        }

        private void ShadeGroup(int group, bool drawBoundingBox,bool clearMap)
        {
            ShadeGroup(allGroups[group], drawBoundingBox, clearMap);
        }

        private void ShadeGroup(MapGroup group, bool drawBoundingBox, bool clearMap)
        {
            //if (clearMap)
            //{
            //    DrawMaps();
            //}
            Bitmap shaded = new Bitmap(pictureBox2.Image);
            Bitmap map = new Bitmap(pictureBox1.Image);
            // Specify a pixel format.
            PixelFormat pxf = PixelFormat.Format24bppRgb;

            //// Lock the bitmap's bits.
            //Rectangle rect = new Rectangle(0, 0, width, height);
            //BitmapData MapBmpData =
            //map.LockBits(rect, ImageLockMode.ReadWrite,
            //             pxf);
            //BitmapData ShadedData = shaded.LockBits(rect, ImageLockMode.ReadWrite,
            //             pxf);

            //// Get the address of the first line.
            //IntPtr ptr = MapBmpData.Scan0;
            //IntPtr shadedPtr = ShadedData.Scan0;



            //// Declare an array to hold the bytes of the bitmap.
            //// int numBytes = bmp.Width * bmp.Height * 3;
            //int numBytes = MapBmpData.Stride * height;
            //byte[] MapRgbValues = new byte[numBytes];
            //byte[] ShadedVals = new byte[numBytes];

            //Marshal.Copy(ptr, MapRgbValues, 0, numBytes);
            //Marshal.Copy(shadedPtr, ShadedVals, 0, numBytes);
            //int nIndex = 0;
            //foreach (MapLine line in group.lines)
            //{
            //    for (int x = line.xmin; x <= line.xmax; x++)
            //    {
            //        nIndex = (line.yval * MapBmpData.Stride) + (x * 3);
            //        int color = MapRgbValues[nIndex];
            //        MapRgbValues[nIndex] = (byte)(color / 2);
            //        MapRgbValues[nIndex + 1] = (byte)(color / 2);
            //        MapRgbValues[nIndex + 2] = (byte)(128 + ((int)Math.Abs(color - 128f) / 2));
            //        //shaded.SetPixel(x, line.yval, Color.FromArgb(128 + ((int)Math.Abs(color - 128f) / 2), (int)color / 2, (int)color / 2));


            //        color = ShadedVals[nIndex];
            //        ShadedVals[nIndex] = (byte)(color / 2);
            //        ShadedVals[nIndex + 1] = (byte)(color / 2);
            //        ShadedVals[nIndex + 2] = (byte)(128 + ((int)Math.Abs(color - 128f) / 2));
            //        //color = map.GetPixel(x, line.yval).R;
            //        //map.SetPixel(x, line.yval, Color.FromArgb(128 + ((int)Math.Abs(color - 128f) / 2), (int)color / 2, (int)color / 2));
            //    }
            //}


            
            //Marshal.Copy(MapRgbValues, 0, ptr, numBytes);
            //Marshal.Copy(ShadedVals, 0, shadedPtr, numBytes);


            //// Unlock the bits.
            //map.UnlockBits(MapBmpData);
            //shaded.UnlockBits(ShadedData);


            //this.Cursor = Cursors.Default;


            //if (drawBoundingBox)
            //{
                Graphics g = Graphics.FromImage(shaded);
                Graphics g2 = Graphics.FromImage(map);
                Pen p = new Pen(Color.Blue, 1);
                MapGroup mg = group;
                g.DrawRectangle(p, mg.xmin, mg.ymin, mg.xmax - mg.xmin, mg.ymax - mg.ymin);
                g2.DrawRectangle(p, mg.xmin, mg.ymin, mg.xmax - mg.xmin, mg.ymax - mg.ymin);
            //}

            pictureBox1.Image = map;
            pictureBox2.Image = shaded;
            //if (clearMap)
            //{
            //    pictureBox1.Invalidate();
            //    pictureBox2.Invalidate();
            //}
        }

        private void DoElevationCheck()
        {
            //allGroups.Sort(CompareGroups);
            //groupfactor = int.Parse(txtGroup.Text);

            //for (int group = 0; group < allGroups.Count; group++)
            //{
            //    allGroups[group].elevated = false;
            //}

            //minArea = int.Parse(txtArea.Text);
            //bool elevate = true;
            //for (int group = 0; group < allGroups.Count; group++)
            //{
            //    elevate = true;
            //    if (group != allGroups.Count - 1 )
            //    {
            //        for (int group2 = group + 1; group2 < allGroups.Count; group2++)
            //        {
            //            if ((allGroups[group].Overlaps(allGroups[group2], 0) && allGroups[group2].Area > minArea))
            //            {
            //                elevate = false;
            //            }                       
            //        }
            //    }
            //    if (elevate)
            //    {
            //        allGroups[group].elevated = elevate;
            //        //Since this group is elevated, all groups above it must be too
            //        for (int group2 = group + 1; group2 < allGroups.Count; group2++)
            //        {
            //            if (allGroups[group].Overlaps(allGroups[group2], 0))
            //            {
            //                allGroups[group2].elevated = true;
            //            }
            //        }
            //    }

            //}//end elevation check loop*/

            
        }

        private void ShadeAllGroups()
        {
            ShadeAllGroups(chkBounds.Checked);
        }

        private void ShadeAllGroups(bool drawBoundingBoxes)
        {
            DrawMaps(this.rect);
            this.Cursor = Cursors.WaitCursor;
            //DoElevationCheck();
            Bitmap shaded = new Bitmap(pictureBox2.Image);
            Bitmap map = new Bitmap(pictureBox1.Image);
            // Specify a pixel format.
            PixelFormat pxf = PixelFormat.Format24bppRgb;

            Rectangle bmprect = new Rectangle(0, 0, rect.Width, rect.Height);
            // Lock the bitmap's bits.
            BitmapData MapBmpData =
            map.LockBits(bmprect, ImageLockMode.ReadWrite,
                         pxf);
            BitmapData ShadedData = shaded.LockBits(bmprect, ImageLockMode.ReadWrite,
                         pxf);

            // Get the address of the first line.
            IntPtr ptr = MapBmpData.Scan0;
            IntPtr shadedPtr = ShadedData.Scan0;



            // Declare an array to hold the bytes of the bitmap.
            // int numBytes = bmp.Width * bmp.Height * 3;
            int numBytes = MapBmpData.Stride * bmprect.Height;
            byte[] MapRgbValues = new byte[numBytes];
            byte[] ShadedVals = new byte[numBytes];

            Marshal.Copy(ptr, MapRgbValues, 0, numBytes);
            Marshal.Copy(shadedPtr, ShadedVals, 0, numBytes);

            int nIndex = 0;
            float tmp;
            float ratio;
            Color c;
            foreach (MapGroup group in elevatedGroups)
            {
                foreach (MapLine line in group.lines)
                {
                    for (int x = line.xmin; x <= line.xmax; x++)
                    {
                        nIndex = ((line.yval - rect.Location.Y) * MapBmpData.Stride) + ((x - rect.Location.X) * 3);
                        int color = MapRgbValues[nIndex];
                        MapRgbValues[nIndex] = (byte)(color / 2);
                        MapRgbValues[nIndex + 1] = (byte)(color / 2);
                        MapRgbValues[nIndex + 2] = (byte)(128 + ((int)Math.Abs(color - 128f) / 2));
                        //shaded.SetPixel(x, line.yval, Color.FromArgb(128 + ((int)Math.Abs(color - 128f) / 2), (int)color / 2, (int)color / 2));


                        color = ShadedVals[nIndex];
                        tmp = heights[(line.yval * width) + x];
                        ratio = (tmp - min) / (max - min);
                        c = ScaleColor(ratio);
                        ShadedVals[nIndex] = c.B;//(byte)(color / 2);
                        ShadedVals[nIndex + 1] = c.G;//(byte)(color / 2);
                        ShadedVals[nIndex + 2] = c.R;// (byte)(128 + ((int)Math.Abs(color - 128f) / 2));
                        //color = map.GetPixel(x, line.yval).R;
                        //map.SetPixel(x, line.yval, Color.FromArgb(128 + ((int)Math.Abs(color - 128f) / 2), (int)color / 2, (int)color / 2));
                    }
                }
                if (drawBoundingBoxes)
                {
                    Graphics g = Graphics.FromImage(shaded);
                    Graphics g2 = Graphics.FromImage(map);
                    Pen p = new Pen(Color.Blue, 1);
                    MapGroup mg = group;
                    g.DrawRectangle(p, mg.xmin, mg.ymin, mg.xmax - mg.xmin, mg.ymax - mg.ymin);
                    g2.DrawRectangle(p, mg.xmin, mg.ymin, mg.xmax - mg.xmin, mg.ymax - mg.ymin);
                }

            }

            Marshal.Copy(MapRgbValues, 0, ptr, numBytes);
            Marshal.Copy(ShadedVals, 0, shadedPtr, numBytes);


            // Unlock the bits.
            map.UnlockBits(MapBmpData);
            shaded.UnlockBits(ShadedData);

            pictureBox1.Image = map;
            pictureBox2.Image = shaded;
            

            this.Cursor = Cursors.Default;
        }

        


        private float GetNormalizedHeight(int x, int y)
        {
            //float tmp, norm;
            //tmp = heights[(y * width) + x];
            //norm = tmp % scale;
            //if (norm == 0f)
            //    norm = (int)tmp;
            //else
            //    norm = (tmp - norm) + scale;
            //return norm;

            return histHeights[(y * width) + x];
        }

        private void DrawMaps(int xmin, int ymin, int xmax, int ymax)
        {
            this.Cursor = Cursors.WaitCursor;
            int bwidth = xmax - xmin;
            int bheight = ymax - ymin;
            Bitmap map = new Bitmap(bwidth, bheight);
            //pictureBox1.Width = bwidth;
            //pictureBox1.Height = bheight;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            //pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;

            pictureBox1.Image = (Image)map;

            Bitmap shaded = (Bitmap)map.Clone();
            //pictureBox2.Width = bwidth;
            //pictureBox2.Height = bheight;
            pictureBox2.Image = (Image)shaded;
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            //pictureBox2.SizeMode = PictureBoxSizeMode.CenterImage;
            pictureBox2.BorderStyle = BorderStyle.FixedSingle;

            Bitmap topo = (Bitmap)map.Clone();
            //pictureBox3.Width = bwidth;
            //pictureBox3.Height = bheight;
            pictureBox3.Image = (Image)topo;
            pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
            //pictureBox3.SizeMode = PictureBoxSizeMode.CenterImage;
            pictureBox3.BorderStyle = BorderStyle.FixedSingle;

            byte val = 0;
            float tmp;
            //Create standard and normalized map images

            // Specify a pixel format.
            PixelFormat pxf = PixelFormat.Format24bppRgb;

            // Lock the bitmap's bits.
            Rectangle bmprect = new Rectangle(0, 0, bwidth, bheight);
            BitmapData MapBmpData =
            map.LockBits(bmprect, ImageLockMode.ReadWrite,
                         pxf);
            BitmapData ShadedData = shaded.LockBits(bmprect, ImageLockMode.ReadWrite,
                         pxf);
            BitmapData TopoData = topo.LockBits(bmprect, ImageLockMode.ReadWrite,
                         pxf);

            // Get the address of the first line.
            IntPtr ptr = MapBmpData.Scan0;
            IntPtr shadedPtr = ShadedData.Scan0;
            IntPtr TopoPtr = TopoData.Scan0;


            
            // Declare an array to hold the bytes of the bitmap.
            // int numBytes = bmp.Width * bmp.Height * 3;
            int numBytes = MapBmpData.Stride * bheight;
            byte[] MapRgbValues = new byte[numBytes];
            byte[] ShadedVals = new byte[numBytes];
            byte[] TopoVals = new byte[numBytes];

            Marshal.Copy(ptr, MapRgbValues, 0, numBytes);
            Marshal.Copy(shadedPtr, ShadedVals, 0, numBytes);
            Marshal.Copy(TopoPtr, TopoVals, 0, numBytes);
            float norm;    
            int nIndex = 0;
            float ratio;
            for (int h = ymin; h < ymax; h++)
            {
                // Copy the RGB values into the array.
                //Marshal.Copy((IntPtr)(ptr.ToInt64() + (h * MapBmpData.Stride)), MapRgbValues, 0, numBytes);
                //Marshal.Copy((IntPtr)(shadedPtr.ToInt64() + (h * ShadedData.Stride)), ShadedVals, 0, numBytes);
                for (int w = xmin; w < xmax; w++)
                {
                    tmp = heights[(h * width) + w];
                    ratio = (tmp - min) / (max - min);
                    nIndex = ((h - ymin) * MapBmpData.Stride) + ((w - xmin) * 3);
                    if (tmp == -9999f) //No-data value
                    {
                        MapRgbValues[nIndex] = 255;
                        MapRgbValues[nIndex + 1] = 128;
                        MapRgbValues[nIndex + 2] = 128;
                        //map.SetPixel(w, h, Color.FromArgb(128, 128, 255));
                        //shaded.SetPixel(w, h, Color.FromArgb(128, 128, 255));
                    }
                    else
                    {
                        //Scale height to [0..255] range
                        norm = ratio * 255.0f;
                        if (double.IsNaN((double)norm)) { norm = 0; }
                        val = Convert.ToByte(norm);
                        //map.SetPixel(w, h, Color.FromArgb(val, val, val));
                        MapRgbValues[nIndex] = val;
                        MapRgbValues[nIndex + 1] = val;
                        MapRgbValues[nIndex + 2] = val;

                        Color c = ScaleColor(ratio);
                        

                        //Now get bucketized value
                        if (w > 0)
                        {
                            float prev = GetNormalizedHeight(w - 1, h);
                            tmp = GetNormalizedHeight(w, h);
                            if (prev != tmp)
                            {
                                ShadedVals[nIndex] = 0;
                                ShadedVals[nIndex + 1] = 0;
                                ShadedVals[nIndex + 2] = 255;

                                TopoVals[nIndex] = 0;
                                TopoVals[nIndex + 1] = 0;
                                TopoVals[nIndex + 2] = 0;
                            }
                            else
                            {
                                ShadedVals[nIndex] = val;
                                ShadedVals[nIndex + 1] = val;
                                ShadedVals[nIndex + 2] = val;

                                TopoVals[nIndex] = c.B;
                                TopoVals[nIndex + 1] = c.G;
                                TopoVals[nIndex + 2] = c.R;
                            }
                        }
                        else
                        {
                            //tmp = GetNormalizedHeight(w, h);
                            //if (tmp > max) tmp = max;
                            //val = Convert.ToByte(((tmp - min) / (max - min)) * 255.0f);
                            ShadedVals[nIndex] = val;
                            ShadedVals[nIndex + 1] = val;
                            ShadedVals[nIndex + 2] = val;

                            TopoVals[nIndex] = c.B;
                            TopoVals[nIndex + 1] = c.G;
                            TopoVals[nIndex + 2] = c.R;
                        }
                        
                        //shaded.SetPixel(w, h, Color.FromArgb(val, val, val));
                        //map.SetPixel(w, h, Color.FromArgb(val, val, val));
                    }
                    // Copy the RGB values back to the bitmap
                    //Marshal.Copy(MapRgbValues, 0, (IntPtr)(ptr.ToInt64() + (h * MapBmpData.Stride)), numBytes);
                    //Marshal.Copy(ShadedVals, 0, (IntPtr)(shadedPtr.ToInt64() + (h * ShadedData.Stride)), numBytes);
                }
            }
            Marshal.Copy(MapRgbValues, 0, ptr, numBytes);
            Marshal.Copy(ShadedVals, 0, shadedPtr, numBytes);
            Marshal.Copy(TopoVals, 0, TopoPtr, numBytes);

            // Unlock the bits.
            map.UnlockBits(MapBmpData);
            shaded.UnlockBits(ShadedData);
            topo.UnlockBits(TopoData);

            //if (this.rect.Width > 0)
            //{
            //    Graphics g = Graphics.FromImage(pictureBox1.Image);
            //    Pen p = new Pen(Color.FromArgb(128, Color.Azure), 10);
            //    p.Alignment = System.Drawing.Drawing2D.PenAlignment.Outset;
            //    g.DrawRectangle(p, this.rect.Location.X, this.rect.Location.Y, this.rect.Width, this.rect.Height);

            //    g = Graphics.FromImage(pictureBox2.Image);
            //    g.DrawRectangle(p, this.rect.Location.X, this.rect.Location.Y, this.rect.Width, this.rect.Height);
            //}
            this.Cursor = Cursors.Default;
            
        }
        struct HSV
        {
            public float h;
            public float s;
            public float v;
        }

        private HSV RGB2HSV(Color color)
        {
            float value = Math.Min(1, color.GetBrightness() * 2);
            HSV c = new HSV() { h = color.GetHue(), s = color.GetSaturation(), v = value };
            return c;
        }

        private float LERP(float value, float start, float end, float max)
        {
            return start + (end - start) * value / max;
        }

        private Color ScaleColor(float ratio)
        {
            HSV start = new HSV() { h = 140, s = 0.8f, v = 0.50f }; // green
            HSV end = new HSV() { h = 0, s = 0.8f, v = 0.5f };
            float h2 = LERP(ratio * 360, start.h, end.h, 360);
            float s2 = LERP(ratio, start.s, end.s, 1);
            float v2 = LERP(ratio, start.v, end.v, 1);

            HSV newHSV = new HSV() { h = h2, v = v2, s = s2 };

            Color c = HSV2RGB(newHSV);

            //Color start = Color.Green;
            //Color end = Color.Red;
            //int r2 = (int)LERP(ratio * 255, start.R, end.R, 255);
            //int g2 = (int)LERP(ratio*255, start.G, end.G, 255);
            //int b2 = (int)LERP(ratio*255, start.B, end.B, 255);
            //Color c = Color.FromArgb(r2, g2, b2);
            return c;
        }

        private Color HSV2RGB(HSV hsv)
        {
            float r = 0, g = 0, b = 0;


            if (hsv.s == 0.0)
            {
                r = g = b = hsv.v*255;
                return Color.FromArgb((int)r, (int)g, (int)b);
            }
            float c = hsv.s * hsv.v;
            float h0 = hsv.h / 60;
            float x = c * (1 - Math.Abs(h0 % 2 - 1));


            if (h0 >= 0 && h0 < 1)
            {
                r = c;
                g = x;
                b = 0;
            }
            if (h0 >= 1 && h0 < 2)
            {
                r = x;
                g = c;
                b = 0;
            }
            if (h0 >= 2 && h0 < 3)
            {
                r = 0;
                g = c;
                b = x;
            }
            if (h0 >= 3 && h0 < 4)
            {
                r = 0;
                g = x;
                b = c;
            }
            if (h0 >= 4 && h0 < 5)
            {
                r = x;
                g = 0;
                b = c;
            }
            if (h0 >= 5 && h0 < 6)
            {
                r = c;
                g = 0;
                b = x;
            }

            float m = hsv.v - c;
            r += m;
            g += m;
            b += m;
            r *= 255;
            g *= 255;
            b *= 255;
            Color col = Color.FromArgb((int)r, (int)g, (int)b);
            return col;
        }

        private void DrawMaps()
        {
            DrawMaps(0, 0, width, height);
        }

        int lastHlGroup = 0;

        private void button2_Click(object sender, EventArgs e)
        {
            ShadeAllGroups();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DrawMaps();
        }

       

        private int ClientToMapCoordX(int coord)
        {
            return (int)(((float)coord / (float)pictureBox1.Width) * width);
        }

        private int ClientToMapCoordY(int coord)
        {
            return (int)(((float)coord / (float)pictureBox1.Height) * height);
        }

        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                return;
            int mapX = ClientToMapCoordX(e.X);
            int mapY = ClientToMapCoordY(e.Y);

            MapGroup group = null;
            int mindist = int.MaxValue;
            int currdist = mindist;
            //DoElevationCheck();
            allGroups.Sort(ReverseCompareGroups);
            foreach (MapGroup grp in allGroups)
            {
                if (grp.elevated == false)
                    continue;

                int grpX = grp.xmin + ((grp.xmax - grp.xmin) / 2);
                int grpY = grp.ymin + ((grp.ymax - grp.ymin) / 2);

                currdist = (int)Math.Abs(Math.Sqrt(Math.Pow(grpX - mapX, 2) + Math.Pow(grpY - mapY, 2)));
                if (currdist < mindist)
                {
                    mindist = currdist;
                    group = grp;
                }
            }

            if (group != null)
            {
                label11.Text = mindist.ToString() + "m";
                lblArea.Text = group.Area.ToString() + "m";
                lblElevation.Text = group.zval.ToString() + "m";
                int grpX = group.xmin + ((group.xmax - group.xmin) / 2);
                int grpY = group.ymin + ((group.ymax - group.ymin) / 2);
                Graphics g = Graphics.FromImage(pictureBox1.Image);
                Pen p = new Pen(Color.FromArgb(128,Color.Azure), 2);
                g.DrawLine(p, mapX, mapY, grpX, grpY);
                g.DrawRectangle(p, group.xmin, group.ymin, group.xmax - group.xmin, group.ymax - group.ymin);
                this.Invalidate();
                pictureBox1.Invalidate();
            }
        }
        bool mousedown = false;
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mousedown = true;
                dragStart = e.Location;
            }
            
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mousedown && e.Button == MouseButtons.Left)
            {
                dragging = true;

            }
            else
            {
                return;
            }
            if (dragging)
            {
                rect = new Rectangle(ClientToMapCoordX(dragStart.X), ClientToMapCoordY(dragStart.Y),
                    ClientToMapCoordX(e.Location.X) - ClientToMapCoordX(dragStart.X),
                    ClientToMapCoordY(e.Location.Y) - ClientToMapCoordY(dragStart.Y));

                Graphics g = Graphics.FromHwnd(pictureBox1.Handle);
                Pen p = new Pen(Color.FromArgb(128, Color.Azure), 10);
                p.Alignment = System.Drawing.Drawing2D.PenAlignment.Outset;
                g.DrawRectangle(p, rect.Location.X, rect.Location.Y, rect.Width, rect.Height);

                this.Invalidate();
                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            
            if (!dragging || !mousedown || e.Button == MouseButtons.Right) return;
            //DrawMaps();
            dragging = false;
            mousedown = false;
            int xmin = ClientToMapCoordX(dragStart.X);
            int ymin = ClientToMapCoordY(dragStart.Y);
            int xmax = ClientToMapCoordX(e.Location.X);
            int ymax = ClientToMapCoordY(e.Location.Y);

            Graphics g = Graphics.FromImage(pictureBox1.Image);
            Pen p = new Pen(Color.FromArgb(128, Color.Azure), 10);
            p.Alignment = System.Drawing.Drawing2D.PenAlignment.Outset;
            g.DrawRectangle(p, xmin, ymin, xmax - xmin, ymax - ymin);

            AnalyzeMap(xmin, ymin, xmax, ymax);
            //DoElevationCheck();

            this.Invalidate();
            pictureBox1.Invalidate();

            lastHlGroup = allGroups.Count - 1;
            rect = new Rectangle(xmin, ymin, xmax - xmin, ymax - ymin);
            DrawMaps(rect);
            
        }

        private void DrawMaps(Rectangle area)
        {
            DrawMaps(area.Location.X, area.Location.Y, area.Location.X + area.Width, area.Location.Y + area.Height);
        }

        

        Rectangle rect = new Rectangle(0,0,0,0);
        

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);
            if (rect.Width > 0)
            {
                Graphics g = Graphics.FromHwnd(pictureBox1.Handle);
                Pen p = new Pen(Color.FromArgb(128, Color.Azure), 10);
                p.Alignment = System.Drawing.Drawing2D.PenAlignment.Outset;
                g.DrawRectangle(p, rect.Location.X, rect.Location.Y, rect.Width, rect.Height);
            }
           
            
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0)
                return;
            ShadeGroup(elevatedGroups[listBox1.SelectedIndex], chkBounds.Checked, chkClear.Checked);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //Remove rectangle outline
            rect = new Rectangle(0,0,width,height);
            AnalyzeMap(0, 0, width, height);
            DrawMaps(rect);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            int zoomWidth = (int)((double)rect.Width * ((double)numericUpDown1.Value / 100.0));
            int zoomHeight = (int)((double)rect.Height * ((double)numericUpDown1.Value / 100.0));

            pictureBox1.Height = zoomHeight;
            pictureBox1.Width = zoomWidth;
            pictureBox2.Width = zoomWidth;
            pictureBox2.Height = zoomHeight;
            pictureBox3.Width = zoomWidth;
            pictureBox3.Height = zoomHeight;
        }

        private void btnSaveImages_Click(object sender, EventArgs e)
        {
            pictureBox1.Image.Save("C:\\PTET\\normal"+DateTime.Now.Ticks.ToString()+".png",ImageFormat.Png);
            pictureBox2.Image.Save("C:\\PTET\\topo" + DateTime.Now.Ticks.ToString() + ".png", ImageFormat.Png);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem.ToString() == "Rural")
            {
                minArea = 250*250;
                maxArea = 2500*2500;
            }
            else if (comboBox1.SelectedItem.ToString() == "Suburban")
            {
                minArea = 100*100;
                maxArea = 1000*1000;
            }
            else if (comboBox1.SelectedItem.ToString() == "Urban")
            {
                minArea = 100;
                maxArea = 100*100;
            }
            else if (comboBox1.SelectedItem.ToString() == "All")
            {
                minArea = 1;
                maxArea = float.MaxValue;
            }
            else
            {
                minArea = 10;
                maxArea = 2500;
            }
            FilterGroups();
        }

       

        

    }

    public class MapLine
    {
        public int xmin;
        public int xmax;
        public float zval;
        public int yval;
        public bool elevated;

        public MapLine(int x1, int x2, float z, int y)
        {
            xmin = x1;
            xmax = x2;
            zval = z;
            yval = y;
            elevated = true;
        }

        public MapGroup group;
    }

    public class MapGroup
    {
        public int xmin;
        public int xmax;
        public int ymin;
        public int ymax;
        public float zval;
        public List<MapLine> lines;
        public bool elevated;

        private string GroupInfo()
        {
            return string.Format("Area: {0}, Pos: ({1},{2},{4}), {3}", Area, xmin, ymin, elevated.ToString(),zval);
        }

        public string Info
        {
            get { return GroupInfo(); }
        }

        public MapGroup(MapLine line)
        {
            elevated = true;
            xmin = line.xmin;
            xmax = line.xmax;
            ymin = line.yval;
            ymax = line.yval;
            zval = line.zval;
            lines = new List<MapLine>();
            lines.Add(line);
        }
        private float GetArea()
        {
            float area = 0;
            foreach (MapLine line in lines)
            {
                area += line.xmax - line.xmin;
            }
            return area;
        }

        public float Area
        {
            get
            {
                return GetArea();
            }
        }

        public bool Overlaps(MapGroup other, float scale)
        {
            if ((((this.xmax + scale) >= other.xmax && (this.xmin - scale) <= other.xmax) ||
                 ((this.xmin - scale) <= other.xmin && (this.xmax + scale) >= other.xmin) ||
                 ((this.xmax + scale) <= other.xmax && (this.xmin - scale) >= other.xmin)) &&
                (((this.ymax + scale) >= other.ymax && (this.ymin - scale) <= other.ymax) ||
                 ((this.ymin - scale) <= other.ymin && (this.ymax + scale) >= other.ymin) ||
                 ((this.ymax + scale) <= other.ymax && (this.ymin - scale) >= other.ymin)))
            {
                return ExactOverlap(other);
            }
            return false;
        }

        public bool ExactOverlap(MapGroup other)
        {
            bool boundedleft;
            bool boudedright;

            foreach (MapLine line in lines)
            {
                boundedleft = false;
                boudedright = false;
                foreach (MapLine otherline in other.lines)
                {
                    if (line.yval == otherline.yval)
                    {
                        if (line.xmax >= otherline.xmin - 1 && line.xmin < otherline.xmax)
                            boundedleft = true;
                        if (line.xmin <= otherline.xmax + 1 && line.xmax > otherline.xmax)
                            boudedright = true;
                    }
                    if (boundedleft && boudedright)
                    {
                        return true;
                    }
                }
                
            }

            return false;
        }
    }

    public class MedianList : List<int>
    {
        float min;
        float max;
        public int Median;

        public MedianList()
        {
            min = Int32.MaxValue;
            max = Int32.MinValue;
            Median = 0;
        }

        public void Add(float value, int point)
        {
            base.Add(point);
            if (value < min) min = value;
            if (value > max) max = value;

            Median = (int)(min + ((max - min) / 2));
        }

        public void AddRange(MedianList range)
        {
            base.AddRange(range);

            if (range.min < min) min = range.min;
            if (range.max > max) max = range.max;

            Median = (int)(min + ((max - min) / 2));
        }

    }
}
