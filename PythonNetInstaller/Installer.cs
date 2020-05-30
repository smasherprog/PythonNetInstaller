using System;
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

        public static void DownloadAndInstallPython(string download_url, bool delete_exisiting_installation = false)
        {
            if (!download_url.EndsWith(".zip"))
            {
                throw new ArgumentException("Must end in .zip", "download_url");
            }

            var filepath = Path.GetFileName(download_url);
            RunCommand($"cd {INSTALL_PATH} && curl {download_url} -o {filepath}");
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
        }

        public static void InstallPythonFromAssembly(Assembly assembly, string resource_name, bool delete_exisiting_installation = false)
        {
            if (!resource_name.EndsWith(".zip"))
            {
                throw new ArgumentException("Must end in .zip", "resource_name");
            }

            var extractpath = Path.Combine(INSTALL_PATH, resource_name);
            CopyEmbeddedResourceToFile(assembly, resource_name, extractpath);
            extractpath = extractpath.Replace(".zip", "");
            if (Directory.Exists(extractpath) && delete_exisiting_installation)
            {
                Directory.Delete(extractpath, true);
            }

            ZipFile.ExtractToDirectory(extractpath + ".zip", extractpath);
            EMBEDDED_PYTHON = resource_name.Replace(".zip", "");
            Environment.SetEnvironmentVariable("PATH", $"{PythonHomeDirectory};" + Environment.GetEnvironmentVariable("PATH"));
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

        public static void RunCommand(string command, bool runInBackground = true /* this should only be true when debugging as it will open a window for each command and you can inspect the*/)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            if (runInBackground)
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            string commandMode = runInBackground ? "/C" : "/K";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                startInfo.FileName = "/bin/bash";
                commandMode = "-c";
            }
            startInfo.Arguments = $"{commandMode} {command}";
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }
    }
}
