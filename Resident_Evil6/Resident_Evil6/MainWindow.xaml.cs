using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Resident_Evil6
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void UpdateDelegate(string output);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_DragEnter_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Grid_Drop_1(object sender, DragEventArgs e)
        {
            var pathArray = ((System.Array)e.Data.GetData(DataFormats.FileDrop));
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, a) =>
            {
                for (int i = 0; i < pathArray.Length; ++i)
                {
                    string path = pathArray.GetValue(i).ToString();
                    string filename = Path.GetFileName(path);
                    UpdateProgress(String.Format("{0}", filename));
                    try
                    {
                        Export(path);
                        UpdateProgress("...Done!");
                    }
                    catch (Exception ex)
                    {
                        UpdateProgress(String.Format(": {0}", ex.Message));
                    }
                    UpdateProgress(String.Format("\t[{0}/{1}]\n", i + 1, pathArray.Length));
                }
            };
            worker.RunWorkerAsync();
        }

        private void log_PreviewDragEnter_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        byte[] data;

        int count = 0;

        int length = 0;

        int offset = 0;

        List<int> pointers = new List<int>();
        List<string> text = new List<string>();

        private void Export(string path)
        {
            count = 0;
            length = 0;
            offset = 0;
            pointers.Clear();
            text.Clear();
            string dest = path + ".txt";
            FileHelper helper = new FileHelper(path);
            data = helper.GetBytes();
            GetPointers();
            GetText();
            string contents = helper.AgemoFormat(text.ToArray());
            helper.Write(dest, contents, Encoding.UTF8);
        }

        private void GetPointers()
        {
            using (BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                reader.BaseStream.Seek(0x14, SeekOrigin.Begin);
                count = FileHelper.SwapEndian(reader.ReadInt32());
                for (int i = 0; i < count; ++i)
                {
                    int pointer = FileHelper.SwapEndian(reader.ReadInt32());
                    if (-1 != pointer)
                    {
                        pointers.Add(pointer);
                    }
                }
                length = FileHelper.SwapEndian(reader.ReadInt32());
                offset = Convert.ToInt32(reader.BaseStream.Position);
            }
        }

        private void GetText()
        {
            using (BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                foreach (int pointer in pointers)
                {
                    int pos = pointer + offset;
                    reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                    List<byte> textBuff = new List<byte>();
                    while (true)
                    {
                        byte buff = reader.ReadByte();
                        if (buff == 0x0)
                        {
                            break;
                        }
                        textBuff.Add(buff);
                    }
                    string tx = Encoding.UTF8.GetString(textBuff.ToArray());
                    text.Add(tx);
                }
            }
        }

        private void UpdateProgress(string output)
        {
            Dispatcher.Invoke(new UpdateDelegate((o) =>
            {
                log.Text += o;
            }), output);
        }
        
    }
}
