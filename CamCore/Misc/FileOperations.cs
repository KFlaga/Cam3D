using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace CamCore
{
    public static class FileOperations
    {
        public delegate void FileDelegate(Stream file, string filePath);

        public static void LoadFromFile(FileDelegate onFileOpen, string filter)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = filter;
            bool? res = fileDialog.ShowDialog();
            if (res != null && res == true && File.Exists(fileDialog.FileName))
            {
                Stream fs = fileDialog.OpenFile();
//#if DEBUG
//                onFileOpen(fs, fileDialog.FileName);
//#else
                try
                {
                    onFileOpen(fs, fileDialog.FileName);
                }
                catch (Exception exc)
                {
                    MessageBox.Show("Failed to load data: " + exc.Message, "Error");
                }
//#endif
                fs.Close();
            }
        }

        public static void SaveToFile(FileDelegate onFileOpen, string filter)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = filter;
            bool? res = fileDialog.ShowDialog();
            if (res != null && res == true)
            {
                Stream fs = fileDialog.OpenFile();
                try
                {
                    onFileOpen(fs, fileDialog.FileName);
                }
                catch (Exception exc)
                {
                    MessageBox.Show("Failed to save data: " + exc.Message, "Error");
                }
                fs.Close();
            }
        }

        public static Stream OpenFile(string filter)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = filter;
            bool? res = fileDialog.ShowDialog();
            if (res != null && res == true)
            {
                Stream fs = fileDialog.OpenFile();
                return fs;
            }
            return null;
        }

        public static string GetFilePath(string filter)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = filter;
            bool? res = fileDialog.ShowDialog();
            if (res != null && res == true)
            {
                return fileDialog.FileName;
            }
            return null;
        }
    }
}
