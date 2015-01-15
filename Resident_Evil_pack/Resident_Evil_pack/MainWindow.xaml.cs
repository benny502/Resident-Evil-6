using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Resident_Evil_pack
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void UpdateDelegate(string tag, string filename, string message);
        private string ERROR = "ERROR";
        private string INFO = "INFO";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            switch (btn.Name)
            {
                case "Button1":
                    FolderBrowserDialog folder = new FolderBrowserDialog();
                    folder.Description = "选择原文件目录";
                    folder.RootFolder = Environment.SpecialFolder.Desktop;
                    folder.ShowNewFolderButton = true;
                    if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        App.folder = folder.SelectedPath;
                    }
                    break;
                case "Button2":
                    System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
                    dlg.Filter = "文本文件|*.txt";
                    dlg.Multiselect = true;
                    dlg.FileOk += dlg_FileOk;
                    dlg.ShowDialog();
                    break;
                case "tblBtn":
                    System.Windows.Forms.OpenFileDialog tbldlg = new System.Windows.Forms.OpenFileDialog();
                    tbldlg.Filter = "码表文件|*.txt;*.tbl";
                    tbldlg.FileOk += (s, a) =>
                    {
                        App.tblStr = tbldlg.FileName;
                    };
                    tbldlg.ShowDialog();
                    break;
            }

        }

        void dlg_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {

            System.Windows.Forms.OpenFileDialog dlg = sender as System.Windows.Forms.OpenFileDialog;
            string[] files = dlg.FileNames;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            worker.RunWorkerAsync(files);

        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] files = e.Argument as string[];
            FileHelper helper = new FileHelper();
            if (string.IsNullOrEmpty(App.tblStr)) throw new Exception("请先选择码表路径");
            Dictionary<string, string> tbl = helper.GetTbl(App.tblStr);
            foreach (string file in files)
            {
                string filename = Path.GetFileName(file);
                try
                {
                    string finder = filename.Substring(0, filename.Length - 4);
                    if (string.IsNullOrEmpty(App.folder)) throw new Exception("请先选择原文件路径");
                    string source = App.folder + "\\" + finder;
                    string dest = App.folder + "\\new\\" + finder;

                    string[] strs = helper.GetAgemoString(file, Encoding.UTF8);
                    /*for (int i = 0; i < strs.Length; ++i)
                    {
                        strs[i] = strs[i].Replace("\r\n", "\n");
                    }*/
                    getfileData(source);
                    getPointers();
                    Import(strs, tbl);
                    UpdatePointers();
                    WriteToBin(dest);
                    console(INFO, filename, "导入完成");
                }
                catch (Exception ex)
                {
                    console(ERROR, filename, ex.Message);
                }
            }
        }

        private byte[] data;
        private int count = 0;
        private int length = 0;
        private int offset = 0;
        private List<int> pointers = new List<int>();
        private List<int> text_addr = new List<int>();


        private void getfileData(string path)
        {
            if (!File.Exists(path)) throw new Exception("未检测到对应的原文件");
            FileHelper helper = new FileHelper(path);
            data = helper.GetBytes();
            count = 0;
            length = 0;
            offset = 0;
            pointers.Clear();
            text_addr.Clear();
        }

        private void getPointers()
        {
            using (BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                reader.BaseStream.Seek(0x14, SeekOrigin.Begin);
                count = FileHelper.SwapEndian(reader.ReadInt32());
                for (int i = 0; i < count; ++i)
                {
                    int addr = Convert.ToInt32(reader.BaseStream.Position);
                    int pointer = FileHelper.SwapEndian(reader.ReadInt32());
                    if (-1 != pointer)
                    {
                        pointers.Add(addr);
                    }
                }
                length = FileHelper.SwapEndian(reader.ReadInt32());
                offset = Convert.ToInt32(reader.BaseStream.Position);
            }
        }

        private void Import(string[] strs, Dictionary<string, string> tbl)
        {
            using (BinaryWriter writer = FileHelper.GetMemoryWriter(data))
            {
                byte[] zero = new byte[length];
                FileHelper helper = new FileHelper();
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                writer.Write(zero);
                writer.Flush();
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                foreach (string str in strs)
                {
                    int absolute_pos = Convert.ToInt32(writer.BaseStream.Position);
                    int pos = absolute_pos - offset;
                    text_addr.Add(pos);
                    writer.Write(helper.Trans(str, tbl));
                    byte ze = 0;
                    writer.Write(ze);
                }
            }
        }

        private void UpdatePointers()
        {
            using (BinaryWriter writer = FileHelper.GetMemoryWriter(data))
            {
                int i = 0;
                foreach (int pos in pointers)
                {
                    writer.BaseStream.Seek(pos, SeekOrigin.Begin);
                    writer.Write(FileHelper.SwapEndian(text_addr[i]));
                    i++;
                }
                writer.Flush();
            }
        }

        private void WriteToBin(string path)
        {
            FileHelper helper = new FileHelper();
            helper.Write(path, data);
        }

        private void console(string TAG, string filename, string msg)
        {
            Dispatcher.Invoke(new UpdateDelegate((t, f, m) =>
            {
                xbox.Text += String.Format("{0}: {1} - {2}\n", t, f, m);
            }), TAG, filename, msg);
        }
    }
}
