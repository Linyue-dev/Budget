using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace Budget
{
    public class BudgetFiles
    {
        private static String DefaultSavePath = @"Budget\";
        private static String DefaultAppData = @"%USERPROFILE%\AppData\Local\";

        public static String VerifyReadFromFileName(String FilePath, String DefaultFileName)
        {


            if (FilePath == null)
            {
                FilePath = Environment.ExpandEnvironmentVariables(DefaultAppData + DefaultSavePath + DefaultFileName);
            }

            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException("ReadFromFileException: FilePath (" + FilePath + ") does not exist");
            }

            return FilePath;

        }

        public static String VerifyWriteToFileName(String FilePath, String DefaultFileName)
        {

            if (FilePath == null)
            {
                // create the default appdata directory if it does not already exist
                String tmp = Environment.ExpandEnvironmentVariables(DefaultAppData);
                if (!Directory.Exists(tmp))
                {
                    Directory.CreateDirectory(tmp);
                }

                // create the default Budget directory in the appdirectory if it does not already exist
                tmp = Environment.ExpandEnvironmentVariables(DefaultAppData + DefaultSavePath);
                if (!Directory.Exists(tmp))
                {
                    Directory.CreateDirectory(tmp);
                }

                FilePath = Environment.ExpandEnvironmentVariables(DefaultAppData + DefaultSavePath + DefaultFileName);
            }
            String folder = Path.GetDirectoryName(FilePath);
            String delme = Path.GetFullPath(FilePath);
            if (!Directory.Exists(folder))
            {
                throw new Exception("SaveToFileException: FilePath (" + FilePath + ") does not exist");
            }

            if (File.Exists(FilePath))
            {
                FileAttributes fileAttr = File.GetAttributes(FilePath);
                if ((fileAttr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    throw new Exception("SaveToFileException:  FilePath(" + FilePath + ") is read only");
                }
            }
            return FilePath;

        }
    }
}
