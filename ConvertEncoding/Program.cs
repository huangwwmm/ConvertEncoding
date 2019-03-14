using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace ConvertEncoding
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            if (args == null || args.Length < 1)
            {
                args = new string[] { "-i", ".\\Test" , "--extensionw" , ".h" , ".cpp" , "--outputlog" };
            }
#endif
            AppDomain.CurrentDomain.UnhandledException += OnException;

            ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);
            switch (result.Tag)
            {
                case ParserResultType.Parsed:
                    Parsed<Options> parsed = (Parsed<Options>)result;
                    Action action = new Action(parsed.Value);
                    break;
                case ParserResultType.NotParsed:
                default:
                    Options template = new Options();
                    template.OutputEncoding = "gbk";
                    template.ExtensionWhiteList = new string[] { ".c", ".h", ".cpp" };
                    template.ExtensionBlackList = new string[] { ".txt", ".png" };
                    template.InputDirectory = "D:\\";
                    template.OutputLog = true;
                    Console.WriteLine("Example:");
                    Console.WriteLine("\tbash " + Parser.Default.FormatCommandLine<Options>(template));
                    break;
            }
            Console.ReadKey();
        }

        private static void OnException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.ReadKey();
            Environment.Exit(1);
        }
    }

    public class Action
    {
        public Action(Options options)
        {
            OpenOrCreateDirectory(options.InputDirectory, false);
            if (string.IsNullOrEmpty(options.OutputDirectory))
            {
                options.OutputDirectory = options.InputDirectory;
            }
            OpenOrCreateDirectory(options.OutputDirectory, true);
            string inputDirectory = new DirectoryInfo(options.InputDirectory).FullName;
            string outputDirectory = new DirectoryInfo(options.OutputDirectory).FullName;

            Encoding outputEncoding = Encoding.GetEncoding(options.OutputEncoding);

            string[] allFiles = Directory.GetFiles(options.InputDirectory, "*", SearchOption.AllDirectories);
            HashSet<string> extensionWhiteList = options.ExtensionWhiteList == null
                ? new HashSet<string>()
                : new HashSet<string>(options.ExtensionWhiteList);
            bool allExtension = extensionWhiteList.Contains("*");
            HashSet<string> extensionBlackList = options.ExtensionBlackList == null
                ? new HashSet<string>()
                : new HashSet<string>(options.ExtensionBlackList);
            List<FileInfo> inputFiles = new List<FileInfo>();
            for (int iFile = 0; iFile < allFiles.Length; iFile++)
            {
                string iterFile = allFiles[iFile];
                FileInfo iterFileInfo = new FileInfo(iterFile);
                if ((allExtension || extensionWhiteList.Contains(iterFileInfo.Extension))
                    && !extensionBlackList.Contains(iterFileInfo.Extension))
                {
                    inputFiles.Add(iterFileInfo);
                }
                else if (options.OutputLog)
                {
                    Console.WriteLine("Ignore file: " + iterFileInfo.FullName);
                }
            }

            int convertedFileCount = 0;
            for (int iFile = 0; iFile < inputFiles.Count; iFile++)
            {
                FileInfo iterFile = inputFiles[iFile];
                Encoding inputEncoding = null;
                using (FileStream fs = File.OpenRead(iterFile.FullName))
                {
                    Ude.CharsetDetector cdet = new Ude.CharsetDetector();
                    cdet.Feed(fs);
                    cdet.DataEnd();
                    if (cdet.Charset != null)
                    {
                        inputEncoding = Encoding.GetEncoding(cdet.Charset);
                    }
                    else if (options.OutputLog)
                    {
                        Console.WriteLine(string.Format("{0} Cant detector file encoding", iterFile.FullName));
                    }
                }
                if (inputEncoding != null && inputEncoding != outputEncoding)
                {
                    File.WriteAllBytes(iterFile.FullName.Replace(inputDirectory, outputDirectory)
                        , Encoding.Convert(inputEncoding
                            , outputEncoding
                            , File.ReadAllBytes(iterFile.FullName)));
                    convertedFileCount++;
                    Console.WriteLine("Converted File: " + iterFile.FullName);
                }
                else if (options.OutputLog)
                {
                    Console.WriteLine(string.Format("File encoding already is ({0}): {1}", options.OutputEncoding, iterFile.FullName));
                }
            }
            Console.WriteLine(string.Format("Converted {0} files", convertedFileCount));
        }

        public void OpenOrCreateDirectory(string directory, bool createIfNotFound)
        {
            if (!Directory.Exists(directory))
            {
                if (createIfNotFound)
                {
                    Directory.CreateDirectory(directory);
                }
                if (!Directory.Exists(directory))
                {
                    throw new DirectoryNotFoundException(string.Format("Directory {0} not found and can't create", directory));
                }
            }
        }
    }

    public class Options
    {
        [Option('i'
            , "inputdir"
            , Required = true
            , HelpText = "Input Directory")]
        public string InputDirectory { get; set; }

        [Option('o'
            , "outputdir"
            , HelpText = "Output Directory")]
        public string OutputDirectory { get; set; }

        [Option("extensionw"
            , Default = new string[] { "*" }
            , HelpText = "Extension White List")]
        public IEnumerable<string> ExtensionWhiteList { get; set; }

        [Option("extensionb"
            , Default = null
            , HelpText = "Extension Black List")]
        public IEnumerable<string> ExtensionBlackList { get; set; }

        [Option('e'
            , "outencoding"
            , Default = "utf-8"
            , HelpText = "Output Encoding")]
        public string OutputEncoding { get; set; }

        [Option("outputlog"
           , Default = false
           , HelpText = "Output Log")]
        public bool OutputLog { get; set; }
    }
}
