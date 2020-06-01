using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PythonNetInstaller
{
    public static class Installer
    {
        private static string EMBEDDED_PYTHON = string.Empty;
        private static string INSTALL_PATH { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static string PythonHomeDirectory
        {
            get
            {
                var install_dir = Path.Combine(INSTALL_PATH, EMBEDDED_PYTHON);
                return install_dir;
            }
        }

        public static void InstallPythonFromUri(Uri download_url, bool delete_exisiting_installation = false)
        {
            var stringurl = download_url.ToString();
            if (!stringurl.EndsWith(".zip"))
            {
                throw new ArgumentException("Must end in .zip", "download_url");
            }

            var filepath = Path.GetFileName(stringurl);
            RunCommand($"cd {INSTALL_PATH} && curl {stringurl} -o {filepath}");
            var extractpath = Path.Combine(INSTALL_PATH, filepath);
            InstallPythonFromZip(extractpath, delete_exisiting_installation);
        }

        public static void InstallPythonFromZip(string file_path, bool delete_exisiting_installation = false)
        {
            if (!file_path.EndsWith(".zip"))
            {
                throw new ArgumentException("Must end in .zip", "file_path");
            }

            var resource_name = Path.GetFileName(file_path).Replace(".zip", "");
            var extractpath = Path.Combine(INSTALL_PATH, resource_name);
            if (Directory.Exists(extractpath) && delete_exisiting_installation)
            {
                Directory.Delete(extractpath, true);
            }

            ZipFile.ExtractToDirectory(file_path, extractpath);
            EMBEDDED_PYTHON = resource_name;
            Environment.SetEnvironmentVariable("PATH", $"{PythonHomeDirectory};" + Environment.GetEnvironmentVariable("PATH"));
            var scriptsdir = Path.Combine(PythonHomeDirectory, "Scripts");
            if (!Directory.Exists(scriptsdir))
            {
                Directory.CreateDirectory(scriptsdir);
            }
            Environment.SetEnvironmentVariable("PATH", $"{scriptsdir};" + Environment.GetEnvironmentVariable("PATH"));
            var file = Directory.GetFiles(PythonHomeDirectory, "*._pth", SearchOption.TopDirectoryOnly);
            var filecontents = File.ReadAllText(file[0]).Replace("#import site", "import site");
            File.WriteAllText(file[0], filecontents);
        }

        public static void InstallPythonFromAssembly(Assembly assembly, string resource_name, bool delete_exisiting_installation = false)
        {
            if (!resource_name.EndsWith(".zip"))
            {
                throw new ArgumentException("Must end in .zip", "resource_name");
            }

            var extractpath = Path.Combine(INSTALL_PATH, resource_name);
            CopyEmbeddedResourceToFile(assembly, resource_name, extractpath);
            InstallPythonFromZip(extractpath, delete_exisiting_installation);
        }

        public static void InstallPip()
        {
            string libDir = Path.Combine(PythonHomeDirectory, "Lib");
            if (!Directory.Exists(libDir))
            {
                Directory.CreateDirectory(libDir);
            }
            RunCommand($"cd {libDir} && curl https://bootstrap.pypa.io/get-pip.py -o get-pip.py");
            RunCommand($"cd {PythonHomeDirectory} && python.exe Lib\\get-pip.py");
        }

        private static void CopyEmbeddedResourceToFile(Assembly assembly, string resourceName, string filePath)
        {
            var key = assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(resourceName));
            using (Stream stream = assembly.GetManifestResourceStream(key))
            using (var file = new FileStream(filePath, FileMode.Create))
            {
                if (stream == null)
                    throw new ArgumentException($"Resource name '{resourceName}' not found!");
                stream.CopyTo(file);
            }
        }

        public static bool IsPythonInstalled()
        {
            return File.Exists(Path.Combine(PythonHomeDirectory, "python.exe"));
        }

        public static bool IsPipInstalled()
        {
            return File.Exists(Path.Combine(PythonHomeDirectory, "Scripts", "pip.exe"));
        }

        public static bool IsModuleInstalled(string module)
        {
            if (!IsPythonInstalled())
                return false;

            string moduleDir = Path.Combine(PythonHomeDirectory, "Lib", module);
            return Directory.Exists(moduleDir) && File.Exists(Path.Combine(moduleDir, "__init__.py"));
        }

        public static (string stderroutput, string stdoutput) RunCommand(string command)
        {
            if ((command.Contains("pip install") || command.Contains("pip.exe install")) && !command.Contains("--user"))
            {
                throw new ArgumentException("pip install must be used with the --user command otherwise installs will fail!", "command");
            }
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            string commandMode = "/C";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                process.StartInfo.FileName = "/bin/bash";
                commandMode = "-c";
            }
            process.StartInfo.Arguments = $"{commandMode} {command}";
            process.Start();
            string err = process.StandardError.ReadToEnd();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return (err, output);
        }
    }
}
